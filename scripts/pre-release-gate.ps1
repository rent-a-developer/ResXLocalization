<#
.SYNOPSIS
    The pre-release gate: everything CI checks that can honestly be checked on this machine, in the order
    CI checks it.

.DESCRIPTION
    Run this before pushing a release - or any branch you want CI to go green on. It is the pre-commit gate
    plus the jobs that gate a publish: the documentation build with warnings as errors, the pack with
    package validation, the Native AOT gate against the packed packages, and the package consumers against
    those same packages - once on the SDK the repository pins and once on the .NET 8 SDK.

    It stops at the first failure. Later steps consume what earlier ones produce - the AOT gate and the
    consumers run against the packages the pack step wrote - so continuing past a failure would only
    produce a second, misleading one.

    WHAT IT DOES NOT COVER, and why. None of this is a judgement about importance; each one either needs
    infrastructure this machine does not have or is meaningless outside CI:

      CodeQL, dependency review    Need GitHub's analysis and advisory services.
      Codecov upload               Needs the OIDC token CI holds. The coverage numbers are produced by the
                                   test run here; it is only the upload that cannot happen.
      GitHub Pages deployment      Needs the Pages environment. The site itself IS built here.
      NuGet publication            Needs Trusted Publishing and a pushed version tag. Nothing here
                                   publishes anything.
      The GitHub release           Needs the tag and the GitHub API.
      The other operating system   CI runs the Avalonia verification, the AOT gate and the minimum-SDK
                                   consumers on Linux and on Windows. This runs only what the machine you
                                   are on can run, and says what it skipped.
      The WPF jobs, off Windows    The WPF projects do not load anywhere else, so on Linux or macOS the
                                   WPF build, the WPF tests and the WPF and combined consumers are not run.
      An 8.0 SDK on its own        CI's minimum-SDK job runs on a machine with only the .NET 8 SDK
                                   installed. This gate also runs the consumers on an 8.0 SDK, selected
                                   the same way, so that SDK's compiler and build targets are used. But
                                   the SDK the root global.json pins is installed here too, so a pass here
                                   does not prove the packages work on a machine that has only the .NET 8
                                   SDK.

    A dirty working tree is reported, not rejected: CI tests the commit you push, so anything uncommitted
    is untested by definition, but you may well still be iterating.

.PARAMETER Version
    The version you are about to release, for example 1.2.0. When given, two extra checks run - the same
    two CI runs immediately before it publishes: the <Version> in the repository-root Directory.Build.props
    must match, and CHANGELOG.md must hold exactly one dated, non-empty section for it. Omit it and both
    are skipped, because a branch push publishes nothing.

.PARAMETER SkipNativeAot
    Skip the Native AOT gate. It needs a C++ toolchain - MSVC and the Windows SDK on Windows, clang and
    zlib1g-dev on Linux.

.PARAMETER SkipConsumers
    Skip the package consumers. They restore from the packages the pack step wrote, which takes a minute.

.PARAMETER SkipDocumentation
    Skip the DocFX metadata and site build. It needs the docfx local tool.

.PARAMETER SkipMinimumSdk
    Skip running the package consumers a second time on the .NET 8 SDK. That step needs an 8.0 SDK
    installed next to the one the repository pins.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI uses and what gets published.

.EXAMPLE
    pwsh -File scripts/pre-release-gate.ps1

.EXAMPLE
    pwsh -File scripts/pre-release-gate.ps1 -Version 1.2.0
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [String] $Version,
    [Switch] $SkipNativeAot,
    [Switch] $SkipConsumers,
    [Switch] $SkipDocumentation,
    [Switch] $SkipMinimumSdk,
    [String] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up, whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

# The WPF projects only load on Windows. Off Windows the filter is what builds and tests, and the WPF and
# combined consumers do not run at all - both are named in the summary rather than silently missing.
$onWindows = $IsWindows
$solutionFileName = if ($onWindows) { 'ResXLocalization.slnx' } else { 'ResXLocalization.NonWindows.slnf' }

$consumerDirectory = Join-Path $repositoryRoot 'tests/package-consumption'
$consumerSdkPin = Join-Path $consumerDirectory 'global.json'
$packageOutput = Join-Path $repositoryRoot 'artifacts/packages'
$docfxConfiguration = 'build/docfx/docfx.json'

$failure = $null
$stepSucceeded = $true

function Write-Section
{
    param([Parameter(Mandatory)] [String] $Title)

    Write-Output ''
    Write-Output "=== $Title ==="
}

function Invoke-FromRepositoryRoot
{
    <#
        Runs a command with the repository root as the working directory, restoring the caller's location
        in a finally block. It returns NOTHING and the caller reads $LASTEXITCODE - returning the exit code
        would mix it into the pipeline with everything the command printed, and that output is exactly what
        you need when this step is the one that failed.

        The working directory is not cosmetic: `dotnet` resolves global.json from the CURRENT directory
        upward, and this repository's global.json is what pins the SDK.
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

function Get-DeclaredVersion
{
    # The single source of truth for the version of every package, and what `dotnet pack` produces.
    $sharedProperties = Join-Path $repositoryRoot 'Directory.Build.props'

    return ([Xml] (Get-Content -Raw -LiteralPath $sharedProperties)).Project.PropertyGroup.Version |
        Where-Object { $_ } |
        Select-Object -First 1
}

function Invoke-Step
{
    <#
        Runs one step unless an earlier one failed. $script:failure holds the name of the first failure and
        every later step becomes a no-op, so the output ends with the failure that matters rather than with
        whatever fell over as a consequence of it.

        A step reports its result through $script:stepSucceeded, and NOT by returning a value. In
        PowerShell a script block's output IS its return value, so `if (-not (& $Command))` would capture
        every line the build, the test run and the AOT gate print - the caller would see an array of build
        output rather than a result, nothing would reach the screen until the step ended, and a failing
        step that printed anything at all would evaluate as true and be recorded as a pass. Calling it bare
        lets its output stream straight to the console, which is where "See output" points.
    #>
    param(
        [Parameter(Mandatory)] [String] $Name,
        [Parameter(Mandatory)] [ScriptBlock] $Command
    )

    if ($script:failure)
    {
        return
    }

    Write-Section $Name

    # A step that returns without saying otherwise passed.
    $script:stepSucceeded = $true

    & $Command

    if (-not $script:stepSucceeded)
    {
        $script:failure = $Name
    }
}

# --- Working tree -------------------------------------------------------------------------------------

Write-Section 'Working tree'

$uncommitted = @(& git -C $repositoryRoot status --porcelain | Where-Object { $_ })

if ($uncommitted)
{
    Write-Output "$($uncommitted.Count) uncommitted change(s). CI tests the commit you push, so these are not covered:"
    $uncommitted | Select-Object -First 10 | ForEach-Object { Write-Output "  $_" }

    if ($uncommitted.Count -gt 10)
    {
        Write-Output "  ... and $($uncommitted.Count - 10) more"
    }
}
else
{
    Write-Output 'Clean.'
}

# --- Consumer SDK pin ---------------------------------------------------------------------------------
# The step that runs the consumers on the .NET 8 SDK writes a global.json into the consumer directory and
# removes it again. One left behind - by an interrupted run, or by hand - would quietly choose the SDK for
# everything that runs from there: the Native AOT gate's publish and every consumer. An 8.0 SDK cannot
# build for net10.0, so the net10.0 runs would fail with a message that does not mention the pin. Checked
# first, so the failure names the cause.

Invoke-Step 'Consumer SDK pin' {
    if (Test-Path -LiteralPath $consumerSdkPin)
    {
        Write-Output "FAILED - $consumerSdkPin exists and would choose the SDK for the AOT gate and the consumers. Delete it and re-run."
        $script:stepSucceeded = $false

        return
    }

    Write-Output 'None.'
}

# --- Release identity ---------------------------------------------------------------------------------
# CI verifies both of these immediately before it publishes. A tag that does not match the packed version
# publishes stale packages or, thanks to --skip-duplicate, nothing at all; a missing changelog section
# produces a release whose notes are empty. Neither runs on a branch push, so both are opt-in here.

Invoke-Step 'Release identity' {
    if (-not $Version)
    {
        Write-Output 'Skipped - pass -Version to check the declared version and the changelog section.'

        return
    }

    $declared = Get-DeclaredVersion

    if ($declared -ne $Version)
    {
        Write-Output "FAILED - Directory.Build.props declares $declared, not $Version."

        $script:stepSucceeded = $false

        return
    }

    Write-Output "Directory.Build.props declares $declared."

    Invoke-FromRepositoryRoot {
        & pwsh -NoProfile -NonInteractive -File 'scripts/extract-release-notes.ps1' -Version $Version
    }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)
}

# --- Ignored revisions --------------------------------------------------------------------------------
# The lint job's first check, and the cheapest. A revision in .git-blame-ignore-revs that no longer
# resolves is skipped silently by git, and blame goes back to pointing at the tool that reformatted the
# line - which is exactly what squashing or rebasing a branch that added one does.

Invoke-Step 'Ignored revisions' {
    $revisions = @(Get-Content -LiteralPath (Join-Path $repositoryRoot '.git-blame-ignore-revs') |
        Where-Object { $_ -match '^[0-9a-f]{40}$' })

    if (-not $revisions)
    {
        Write-Output 'FAILED - .git-blame-ignore-revs lists no revisions.'

        $script:stepSucceeded = $false

        return
    }

    $unresolved = @($revisions | Where-Object {
            & git -C $repositoryRoot cat-file -e "$_^{commit}" 2>$null

            $LASTEXITCODE -ne 0
        })

    if ($unresolved)
    {
        Write-Output 'FAILED - these revisions are not commits in this repository:'
        $unresolved | ForEach-Object { Write-Output "  $_" }
        Write-Output 'A rebase or a squash merge rewrote them. Replace each with the SHA it became.'

        $script:stepSucceeded = $false

        return
    }

    Write-Output "$($revisions.Count) revision(s), all resolve."

    return
}

# --- Line endings -------------------------------------------------------------------------------------
# The lint job's second check, and the one whose absence is expensive: a tree whose files are CRLF fails
# the build two steps below with one "Was not formatted." error per file, every one of them naming the
# formatter rather than the line endings, and nothing before it - not the working-tree step above, not the
# tidiness check below - can see it. scripts/verify-line-endings.ps1 says why in full.

Invoke-Step 'Line endings' {
    Invoke-FromRepositoryRoot {
        & pwsh -NoProfile -NonInteractive -File 'scripts/verify-line-endings.ps1'
    }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)
}

# --- Style, formatting and ordering -------------------------------------------------------------------
# The lint job. All of it is also a build error, so this is only the faster way to find out - but it prints
# the diff that would fix things, where the build names the file and stops. It checks a disposable copy of
# the tree and never writes to yours.

Invoke-Step 'Style, formatting and ordering' {
    Invoke-FromRepositoryRoot {
        & pwsh -NoProfile -NonInteractive -File 'scripts/tidy-code.ps1' -Scope all -Check
    }

    if ($LASTEXITCODE -ne 0)
    {
        Write-Output ''
        Write-Output 'FAILED - the tree is not tidy. Apply it with: pwsh -File scripts/pre-commit-gate.ps1 -Fix'

        $script:stepSucceeded = $false

        return
    }

    # And then the same question of THIS tree, which is not the same question.
    #
    # The check above runs the tools for real on a copy placed outside the repository and asks git whether
    # anything changed there. That is the only honest way to check ordering - ReSharper has no check mode -
    # but it answers about the copy, and two things that decide how CSharpier formats are not copied with
    # the files: the line endings, which git normalizes away the moment the copy is committed, and anything
    # a directory ABOVE the repository contributes, which a copy under the temp directory does not have.
    Invoke-FromRepositoryRoot { & dotnet csharpier check . }

    if ($LASTEXITCODE -ne 0)
    {
        Write-Output ''
        Write-Output 'FAILED - CSharpier rejects the tree as it stands on disk, though the check above passed'
        Write-Output 'on a copy of it. The build would fail the same way. When the files themselves look'
        Write-Output 'right, the difference is around them: line endings (the step above), or a .csharpierrc'
        Write-Output 'or .editorconfig in a directory above this repository that the copy never saw.'

        $script:stepSucceeded = $false

        return
    }

    return
}

# --- Build --------------------------------------------------------------------------------------------
# TreatWarningsAsErrors is on for every project, so this is also the style, member-ordering, trim-analyzer
# and public-API gate: an IL2xxx/IL3xxx diagnostic fails here, and so does an undeclared or vanished public
# member.

Invoke-Step "Build ($Configuration)" {
    Invoke-FromRepositoryRoot { & dotnet build $solutionFileName -c $Configuration }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)

    if (-not $onWindows)
    {
        Write-Output ''
        Write-Output 'NOT BUILT: the WPF projects. They need Windows.'
    }
}

# --- Tests --------------------------------------------------------------------------------------------
# One command, because the solution is the source of truth for the project set: `dotnet test` discovers its
# test projects and runs every target framework, so the Core suite runs on net8.0 and net10.0 here.
# --no-build and the configuration are both required - without them `dotnet test` silently rebuilds in
# Debug and tests that instead of the Release build above.

Invoke-Step 'Tests' {
    Invoke-FromRepositoryRoot { & dotnet test $solutionFileName -c $Configuration --no-build }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)

    if (-not $onWindows)
    {
        Write-Output ''
        Write-Output 'NOT RUN: the WPF sample tests, including the WPF threading tests. They need Windows.'
    }
}

# --- Documentation ------------------------------------------------------------------------------------
# --warningsAsErrors, because a docfx warning is a broken cross-reference or a file the configuration does
# not reach, and both of those reach the published site as a hole. `docfx metadata` takes no such flag.

Invoke-Step 'Documentation' {
    if ($SkipDocumentation)
    {
        Write-Output 'Skipped (-SkipDocumentation).'

        return
    }

    Invoke-FromRepositoryRoot { & dotnet tool run docfx metadata $docfxConfiguration }

    if ($LASTEXITCODE -ne 0)
    {
        Write-Output 'FAILED - docfx metadata. Run `dotnet tool restore --configfile NuGet.config` if docfx is missing.'

        $script:stepSucceeded = $false

        return
    }

    Invoke-FromRepositoryRoot { & dotnet tool run docfx build $docfxConfiguration --warningsAsErrors }

    $script:stepSucceeded = ($LASTEXITCODE -eq 0)

    if (-not $onWindows)
    {
        Write-Output ''
        Write-Output 'PARTIAL: the API metadata excludes ResXLocalization.WPF, which needs Windows to build.'
    }
}

# --- Pack ---------------------------------------------------------------------------------------------
# Package validation runs here, against the last published version: removing or changing a public
# signature fails the pack rather than reaching nuget.org. Both steps below consume what this writes.

Invoke-Step 'Pack' {
    Invoke-FromRepositoryRoot {
        & dotnet pack $solutionFileName -c $Configuration --no-build -o $packageOutput
    }

    if ($LASTEXITCODE -ne 0)
    {
        $script:stepSucceeded = $false

        return
    }

    # `dotnet pack --no-build` says nothing at all when it succeeds, which in a gate reads like a step that
    # did not run. Name what it produced instead.
    Get-ChildItem -LiteralPath $packageOutput -Filter '*.nupkg' |
        Sort-Object Name |
        ForEach-Object { Write-Output "  $($_.Name)" }

    if (-not $onWindows)
    {
        Write-Output ''
        Write-Output 'NOT PACKED: ResXLocalization.WPF. Release packaging happens on Windows.'
    }

    # The consumers below build against the packages just written, under the same version as the last
    # pack. A reused MSBuild node keeps evaluating the previous package's targets, so stop it here.
    & dotnet build-server shutdown 2>&1 | Out-Null
}

# --- Native AOT gate ----------------------------------------------------------------------------------
# The only check in the repository that can see silent trimming damage: nothing is trimmed on the JIT, so
# the whole test suite passes while a trimmed application finds no translation at all. Both frameworks,
# because net8.0 is the LTS floor the packages promise and net10.0 is what most consumers use.

Invoke-Step 'Native AOT gate' {
    if ($SkipNativeAot)
    {
        Write-Output 'Skipped (-SkipNativeAot).'

        return
    }

    foreach ($framework in @('net8.0', 'net10.0'))
    {
        Invoke-FromRepositoryRoot {
            & pwsh -NoProfile -NonInteractive -File 'scripts/verify-package-aot.ps1' `
                -Framework $framework -Configuration $Configuration
        }

        if ($LASTEXITCODE -ne 0)
        {
            Write-Output "FAILED - the Native AOT gate failed on $framework."

            $script:stepSucceeded = $false

            return
        }
    }

    Write-Output ''
    Write-Output "Ran on this machine's runtime only. CI also runs net8.0 and net10.0 on linux-x64."

    return
}

# --- Package consumers --------------------------------------------------------------------------------
# Installs the packages the way a stranger would, and asserts that the engine, the packed source generator
# and the buildTransitive resx wiring all arrive through one PackageReference - none of which a
# project-referenced test can see. NUGET_PACKAGES and RestoreConfigFile are overridden so a run cannot
# quietly resolve a ResXLocalization assembly that did not come out of the packages just packed.

function Invoke-PackageConsumers
{
    <#
        Runs each consumer from the consumer directory - so a global.json there decides the SDK - and sets
        $script:stepSucceeded to $false on the first one that fails. Each entry is @{ Name; Arguments }.
    #>
    param([Parameter(Mandatory)] [Object[]] $Consumers)

    $packageVersion = Get-DeclaredVersion
    $originalNuGetPackages = $env:NUGET_PACKAGES
    $consumerNuGetConfig = Join-Path $consumerDirectory 'nuget.config'

    # NuGet resolves by version and not by content, so a ResXLocalization package an earlier run left in the
    # consumers' cache would satisfy the reference instead of the one just packed - the Native AOT gate empties
    # that cache, but it can be skipped. Remove our packages from it here, whatever ran before; the build
    # server goes first because one of its nodes can still hold a package assembly open. NuGet extracts
    # read-only, hence the attribute reset.
    & dotnet build-server shutdown 2>&1 | Out-Null

    try
    {
        Get-ChildItem -LiteralPath (Join-Path $consumerDirectory '.packages') -Directory -Filter 'resxlocalization.*' -ErrorAction SilentlyContinue |
            ForEach-Object {
                Get-ChildItem -LiteralPath $_.FullName -Recurse -Force -File | ForEach-Object { $_.IsReadOnly = $false }
                Remove-Item -LiteralPath $_.FullName -Recurse -Force
            }
    }
    catch
    {
        Write-Output "FAILED - the cached ResXLocalization packages could not be removed: $($_.Exception.Message)"
        Write-Output 'Running against them would test whatever was cached last. Close anything holding a file there and re-run.'
        $script:stepSucceeded = $false

        return
    }

    Push-Location -LiteralPath $consumerDirectory
    try
    {
        $env:NUGET_PACKAGES = Join-Path $consumerDirectory '.packages'

        foreach ($consumer in $Consumers)
        {
            $name = $consumer.Name
            $arguments = @($consumer.Arguments)

            Write-Output ''
            Write-Output "--- $((@($name) + $arguments) -join ' ') ---"

            # A build left by the previous run - possibly by the other SDK - could let MSBuild and WPF's
            # markup compiler decide that nothing changed and skip exactly the passes under test.
            Remove-Item -LiteralPath (Join-Path $name 'bin'), (Join-Path $name 'obj') -Recurse -Force -ErrorAction SilentlyContinue

            & dotnet run --project "$name/$name.csproj" -c $Configuration @arguments `
                -p:RestoreConfigFile=$consumerNuGetConfig -p:ResXLocalizationVersion=$packageVersion

            if ($LASTEXITCODE -ne 0)
            {
                $script:stepSucceeded = $false

                return
            }
        }

        if (-not $onWindows)
        {
            Write-Output ''
            Write-Output 'NOT RUN: WpfConsumer, WpfDerivedNamespaceConsumer and CombinedConsumer. They need Windows.'
        }
    }
    finally
    {
        $env:NUGET_PACKAGES = $originalNuGetPackages
        Pop-Location
    }
}

# The WPF consumers target one framework each. WpfDerivedNamespaceConsumer derives its RootNamespace from
# its project name; see the comment "WPF's temporary markup-compile pass" in
# build/ResXLocalization.Resx.targets.
$wpfConsumers = @(
    @{ Name = 'WpfConsumer'; Arguments = @() }
    @{ Name = 'WpfDerivedNamespaceConsumer'; Arguments = @() }
    @{ Name = 'CombinedConsumer'; Arguments = @() }
)

Invoke-Step 'Package consumers' {
    if ($SkipConsumers)
    {
        Write-Output 'Skipped (-SkipConsumers).'

        return
    }

    # AvaloniaConsumer targets net8.0 and net10.0, and `dotnet run` runs a project with several target
    # frameworks only when one is named, so the consumer runs once per framework with `--framework`. That
    # way both of the packages' target frameworks are exercised.
    $consumers = @(
        @{ Name = 'AvaloniaConsumer'; Arguments = @('--framework', 'net8.0') }
        @{ Name = 'AvaloniaConsumer'; Arguments = @('--framework', 'net10.0') }
    )

    if ($onWindows)
    {
        $consumers += $wpfConsumers
    }

    Invoke-PackageConsumers -Consumers $consumers
}

# --- Package consumers on the .NET 8 SDK ---------------------------------------------------------------
# The same consumers, built by the oldest SDK the packages support - what CI's "Consume packages on .NET 8
# SDK" job does, with the same temporary pin. The SDKs differ in more than the compiler - for example in
# WPF's temporary markup-compile pass, which only WpfConsumer built by the .NET 8 SDK gets wrong without
# the fix; see the comment "WPF's temporary markup-compile pass" in build/ResXLocalization.Resx.targets.

Invoke-Step 'Package consumers (.NET 8 SDK)' {
    if ($SkipConsumers -or $SkipMinimumSdk)
    {
        Write-Output "Skipped ($(if ($SkipConsumers) { '-SkipConsumers' } else { '-SkipMinimumSdk' }))."

        return
    }

    if (-not (& dotnet --list-sdks | Where-Object { $_ -like '8.*' }))
    {
        Write-Output 'FAILED - no .NET 8 SDK is installed. Install one (see CONTRIBUTING.md), or pass -SkipMinimumSdk.'
        $script:stepSucceeded = $false

        return
    }

    if (Test-Path -LiteralPath $consumerSdkPin)
    {
        Write-Output "FAILED - $consumerSdkPin already exists; this step writes and removes its own. Delete it and re-run."
        $script:stepSucceeded = $false

        return
    }

    # The pin covers the consumer directory only: `dotnet` resolves global.json from the current directory
    # upward, and Invoke-PackageConsumers runs from there. It is gitignored, and removed again below.
    Set-Content -LiteralPath $consumerSdkPin -Value '{ "sdk": { "version": "8.0.100", "rollForward": "latestFeature" } }'

    try
    {
        Push-Location -LiteralPath $consumerDirectory
        try
        {
            $activeSdk = & dotnet --version
        }
        finally
        {
            Pop-Location
        }

        Write-Output "Active SDK: $activeSdk"

        if ($activeSdk -notlike '8.*')
        {
            Write-Output 'FAILED - the pin did not select an 8.0 SDK.'
            $script:stepSucceeded = $false

            return
        }

        # -p:TargetFrameworks as well as --framework: restore evaluates every entry in TargetFrameworks, and
        # an 8.0 SDK cannot evaluate net10.0 (NETSDK1045).
        $consumers = @(@{ Name = 'AvaloniaConsumer'; Arguments = @('--framework', 'net8.0', '-p:TargetFrameworks=net8.0') })

        if ($onWindows)
        {
            $consumers += $wpfConsumers
        }

        Invoke-PackageConsumers -Consumers $consumers
    }
    finally
    {
        # Never let a failed delete escape: the summary line must still be written, and the leftover is
        # named so the next run's first step does not have to explain it.
        Remove-Item -LiteralPath $consumerSdkPin -Force -ErrorAction SilentlyContinue

        if (Test-Path -LiteralPath $consumerSdkPin)
        {
            Write-Output "FAILED - the temporary pin $consumerSdkPin could not be removed. Delete it by hand."
            $script:stepSucceeded = $false
        }
    }
}

# --- Summary ------------------------------------------------------------------------------------------

Write-Section 'Pre-release gate summary'

if ($failure)
{
    Write-Output "FAILED: Check $failure failed. See output."

    exit 1
}

Write-Output 'PASSED: All checks passed.'

exit 0
