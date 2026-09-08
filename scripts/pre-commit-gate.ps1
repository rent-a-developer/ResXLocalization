<#
.SYNOPSIS
    The pre-commit gate: repository hygiene, line endings, style/formatting/ordering, a Release build, and
    the test suites this machine can run.

.DESCRIPTION
    CONTRIBUTING.md requires that all tests pass and the build succeeds with no warnings. Because
    TreatWarningsAsErrors=true, the build is also the style, trim-analyzer and public-API gate: IL2xxx and
    IL3xxx diagnostics fail it, and so does an undeclared or vanished public member (RS0016 / RS0017).

    AI agents have hooks that nag about the public API files as you edit, but a hook only sees edits made
    through a tool - and Codex only runs its hooks once they are trusted. This script repeats that check
    over the whole working tree. Run it before every commit, whichever agent you are.

    WHAT THIS SCRIPT WRITES. By default: build output and package caches, and nothing else. It does not
    edit your source files and it does not touch the git index - the tidiness step runs as a CHECK, on a
    disposable copy of the tree. Pass -Fix to have it tidy the working tree first.

    TWO GATES ARE DELIBERATELY NOT RUN HERE, because each takes minutes and neither applies to every
    change. Run them when their trigger applies:

      Native AOT gate     Trigger: any change to resource lookup, culture fallback, satellite discovery,
                          the generated typed keys, or the packaging that carries them. Needs a C++
                          toolchain. It is the ONLY check in the repository that can see silent trimming
                          damage, because nothing is trimmed on the JIT.
                          Run: pwsh -File scripts/verify-package-aot.ps1 -Pack

      Package consumers   Trigger: any change to the package contents, the buildTransitive wiring, the
                          generator's compiler floor, or a dependency version. They install the packages
                          the way a stranger would, which a project-referenced test cannot.
                          Run: pwsh -File scripts/pre-release-gate.ps1 -SkipNativeAot -SkipDocumentation
                               which packs and then runs all of them, on both of AvaloniaConsumer's
                               target frameworks.

.PARAMETER Fix
    Apply style, formatting and member ordering to the working tree before the checks, instead of only
    reporting what would change. This rewrites your files; review the diff and include it in your commit.

.PARAMETER SkipTidy
    Skip the style, formatting and ordering step entirely. The build still fails on any of them.

.PARAMETER SkipBuild
    Skip the Release build (implies -SkipTests).

.PARAMETER SkipTests
    Skip the test run.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI and CONTRIBUTING.md use.

.EXAMPLE
    pwsh -File scripts/pre-commit-gate.ps1

.EXAMPLE
    pwsh -File scripts/pre-commit-gate.ps1 -Fix
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Switch] $Fix,
    [Switch] $SkipBuild,
    [Switch] $SkipTests,
    [Switch] $SkipTidy,
    [String] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up. Everything below is anchored to it, so the
# script behaves the same whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

# The WPF projects only load on Windows. Elsewhere the solution filter is what the build and the test run
# use, and the WPF suite is reported as not run rather than counted as passed.
$onWindows = $IsWindows
$solutionFileName = if ($onWindows) { 'ResXLocalization.slnx' } else { 'ResXLocalization.NonWindows.slnf' }

$publicApiGuard = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'
$lineEndings = Join-Path $repositoryRoot 'scripts/verify-line-endings.ps1'
$tidy = Join-Path $repositoryRoot 'scripts/tidy-code.ps1'

$failures = New-Object System.Collections.Generic.List[String]

function Write-Section
{
    param([Parameter(Mandatory)] [String] $Title)

    Write-Output ''
    Write-Output "=== $Title ==="
}

function Invoke-FromRepositoryRoot
{
    <#
        Runs a native command with the repository root as the working directory. The location is restored
        in a finally block, so an interrupted run does not leave the caller's shell somewhere else.

        It deliberately returns NOTHING and the caller reads $LASTEXITCODE afterwards. Returning the exit
        code would put it on the pipeline together with everything the command printed, so the caller would
        receive an array of build output with a number on the end - and the build log would vanish into a
        variable instead of reaching the screen, which is exactly where it is needed when the build is what
        failed.

        The working directory is not cosmetic here. `dotnet` resolves global.json from the CURRENT
        directory upward, and this repository's global.json is what pins the SDK.
    #>
    param([Parameter(Mandatory)] [ScriptBlock] $Command)

    Push-Location -LiteralPath $repositoryRoot
    try
    {
        & $Command
    }
    finally
    {
        Pop-Location
    }
}

foreach ($required in @($publicApiGuard, $lineEndings, $tidy))
{
    if (-not (Test-Path -LiteralPath $required -PathType Leaf))
    {
        Write-Output "pre-commit-gate: FAILED - $required does not exist."

        exit 1
    }
}

# --- 1. Public API ------------------------------------------------------------------------------------
# Not a failure - a reminder. An accidental public-surface change is already a build error (RS0016 /
# RS0017 from Microsoft.CodeAnalysis.PublicApiAnalyzers, below); what this catches is a deliberate one that
# arrived without the companion edits CONTRIBUTING.md requires. It reads; it never writes.

Write-Section 'Hygiene: public API'

$guardOutput = & pwsh -NoProfile -NonInteractive -File $publicApiGuard
$guardExitCode = $LASTEXITCODE

if ($guardExitCode -ne 0)
{
    $failures.Add('public-api-guard')
    Write-Output "FAIL - public-api-guard.ps1 exited with code $guardExitCode."
}
elseif ($guardOutput)
{
    $guardOutput | ForEach-Object { Write-Output $_ }
}
else
{
    Write-Output 'Unchanged.'
}

# --- 2. Line endings ----------------------------------------------------------------------------------
# Cheap, and it has to come before the tidiness check rather than after it, because the tidiness check
# cannot see this: it commits a disposable copy of the tree before running the tools, and committing is
# what normalizes line endings away. A CRLF tree passes step 3 and fails step 4 with one
# "Was not formatted." error per file, none of which mentions a line ending.
#
# It reads; it never writes, and it never touches the git index. scripts/verify-line-endings.ps1 says why
# both columns of `git ls-files --eol` are checked and what fixes each.

Write-Section 'Hygiene: line endings'

& pwsh -NoProfile -NonInteractive -File $lineEndings

if ($LASTEXITCODE -ne 0)
{
    $failures.Add('line endings')
}

# --- 3. Style, formatting and member ordering ---------------------------------------------------------
# All of it is a build error too, so leaving it to step 4 only means a slower way of finding out - and the
# check prints the exact diff that would fix things, where the build only names the file.
#
# By default this REPORTS. -Fix applies. The editor hooks format on every edit, but they deliberately skip
# the two slow tools - the code-style fixers and the member reordering - and this is where those run.

Write-Section 'Style, formatting and ordering'

if ($SkipTidy)
{
    Write-Output 'Skipped (-SkipTidy).'
}
elseif ($Fix)
{
    # Snapshot the dirty source files BEFORE tidying. `git diff` afterwards lists your own edits too, so
    # reporting its count would say "tidied 40 files" when the tools touched one of them. What the run
    # actually changed is the difference between the two lists.
    $dirtyBefore = @(& git -C $repositoryRoot diff --name-only -- '*.cs' '*.xaml' '*.axaml' 2>$null | Where-Object { $_ })

    & pwsh -NoProfile -NonInteractive -File $tidy -Scope all
    $tidyExitCode = $LASTEXITCODE

    if ($tidyExitCode -ne 0)
    {
        $failures.Add('tidy')
        Write-Output 'FAIL - tidy-code could not finish. Run `dotnet tool restore --configfile NuGet.config` if the tools are missing.'
    }
    else
    {
        $dirtyAfter = @(& git -C $repositoryRoot diff --name-only -- '*.cs' '*.xaml' '*.axaml' 2>$null | Where-Object { $_ })
        $tidied = @($dirtyAfter | Where-Object { $_ -notin $dirtyBefore })

        Write-Output ''
        if ($tidied)
        {
            Write-Output "Tidied $($tidied.Count) file(s) that you had not already changed:"
            $tidied | ForEach-Object { Write-Output "  $_" }
            Write-Output 'Review the diff and include it in your commit.'
        }
        elseif ($dirtyBefore)
        {
            # Everything the tools touched was already in your diff, so there is nothing new to point at -
            # but the tools may still have rewritten those files, and that is worth one line.
            Write-Output "Tidy ran clean. Your $($dirtyBefore.Count) changed file(s) may have been rewritten - review the diff."
        }
        else
        {
            Write-Output 'Already tidy.'
        }
    }
}
else
{
    & pwsh -NoProfile -NonInteractive -File $tidy -Scope all -Check
    $tidyExitCode = $LASTEXITCODE

    if ($tidyExitCode -ne 0)
    {
        $failures.Add('tidy')
        Write-Output ''
        Write-Output 'FAIL - the tree is not tidy, or tidy-code could not finish. The diff above is what'
        Write-Output 'would fix it. Apply it with: pwsh -File scripts/pre-commit-gate.ps1 -Fix'
    }
    else
    {
        # The check above asks whether the tools would change a COPY of the tree. CSharpier.MsBuild asks
        # about the tree itself, during the build, and anything that lives around the files rather than in
        # them can make the two disagree - the line endings step 2 covers, or a .csharpierrc or
        # .editorconfig in a directory ABOVE this repository, which a copy under the temp directory never
        # sees. Asking CSharpier here costs about a second and names the file and the reason; the build
        # names every file in the solution and calls all of them unformatted.
        Invoke-FromRepositoryRoot { & dotnet csharpier check . }

        if ($LASTEXITCODE -ne 0)
        {
            $failures.Add('formatting')
            Write-Output ''
            Write-Output 'FAIL - CSharpier rejects the tree as it stands on disk, though the check above'
            Write-Output 'passed on a copy of it. The build fails the same way, less legibly.'
        }
    }
}

# --- 4. Build -----------------------------------------------------------------------------------------

if ($SkipBuild)
{
    Write-Section 'Build'
    Write-Output 'Skipped (-SkipBuild).'
}
else
{
    Write-Section "Build ($Configuration)"

    Invoke-FromRepositoryRoot { & dotnet build $solutionFileName -c $Configuration }
    $buildExitCode = $LASTEXITCODE

    if ($buildExitCode -ne 0)
    {
        $failures.Add('build')
        Write-Output 'FAIL - build did not succeed. TreatWarningsAsErrors=true, so a style slip, a misplaced'
        Write-Output 'member or an IL2xxx/IL3xxx trim diagnostic fails here too. Never suppress an IL warning'
        Write-Output 'to get green.'
    }

    if (-not $onWindows)
    {
        Write-Output ''
        Write-Output 'NOT BUILT: the WPF projects. They need Windows and are outside the solution filter.'
    }
}

# --- 5. Tests -----------------------------------------------------------------------------------------
# The solution (or the filter) is the source of truth for the project set, and `dotnet test` discovers its
# test projects and runs every target framework automatically - so the Core suite runs on net8.0 and
# net10.0 from one command, and the summary names each one.

if ($SkipBuild -or $SkipTests -or $failures.Contains('build'))
{
    Write-Section 'Tests'
    Write-Output 'Skipped.'
}
else
{
    Write-Section 'Tests'

    Invoke-FromRepositoryRoot { & dotnet test $solutionFileName -c $Configuration --no-build }
    $testExitCode = $LASTEXITCODE

    if ($testExitCode -ne 0) { $failures.Add('tests') }

    if (-not $onWindows)
    {
        Write-Output ''
        Write-Output 'NOT RUN: the WPF sample tests, including the WPF threading tests. They need Windows.'
    }
}

# --- Summary ------------------------------------------------------------------------------------------

Write-Section 'Pre-commit gate summary'

if ($failures.Count -gt 0)
{
    Write-Output "FAILED: $($failures -join ', ')"
    Write-Output 'Do not commit over this - fix it and re-run.'

    exit 1
}

Write-Output 'PASSED. Not covered here, and not needed for every change:'
Write-Output '  - the Native AOT gate, after a change to resource lookup, the generated keys or the packaging'
Write-Output '    (pwsh -File scripts/verify-package-aot.ps1 -Pack)'
Write-Output '  - the package consumers, after a change to what the packages contain'
Write-Output '    (pwsh -File scripts/pre-release-gate.ps1 -SkipNativeAot -SkipDocumentation)'

exit 0
