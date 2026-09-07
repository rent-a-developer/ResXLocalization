<#
.SYNOPSIS
    Records the current public surface of the shipping projects in their PublicAPI.Unshipped.txt files.

.DESCRIPTION
    The shipping projects are guarded by Microsoft.CodeAnalysis.PublicApiAnalyzers: a public member that
    is not listed in the project's PublicAPI.Shipped.txt or PublicAPI.Unshipped.txt is RS0016, and a
    listed member that no longer exists is RS0017. Both are build errors here, because
    TreatWarningsAsErrors is on - so an unintended change to the public surface breaks the build rather
    than slipping through review.

    This script applies the RS0016 code fix, which writes the missing entries into
    PublicAPI.Unshipped.txt. It creates the two files first where they are missing: dotnet format applies
    the fix but will not create the files, and without them the analyzer reports nothing at all.

    Only the three runtime projects are covered. ResXLocalization.SourceGenerators ships inside the UI
    packages as an analyzer, has no package identity of its own and no tracked public API; its equivalent
    is analyzer release tracking in AnalyzerReleases.*.md, which is a separate file and a separate step.

    Review the diff it produces. That diff IS the public-API change, and per CONTRIBUTING.md a real one
    also needs a CHANGELOG entry under `## [Unreleased]` and a documentation update. It does NOT need a
    version bump from you: the version, the release date and the tag are the maintainer's, at release
    time.

    At release time the accumulated entries move from PublicAPI.Unshipped.txt to PublicAPI.Shipped.txt,
    and a removal is recorded in PublicAPI.Unshipped.txt as `*REMOVED*<signature>`. That promotion is a
    maintainer step - see -MarkShipped - and is never part of an ordinary contribution.

.PARAMETER Project
    One or more project files to update. Defaults to the three runtime projects under src/.

.PARAMETER MarkShipped
    The MAINTAINER's release step, not the edit step: fold PublicAPI.Unshipped.txt into
    PublicAPI.Shipped.txt and leave Unshipped empty. `*REMOVED*` entries delete the matching Shipped line
    rather than being carried over. Run this when a version is released, so that the next release's
    Unshipped.txt again means "new since the last release".

.EXAMPLE
    pwsh -File scripts/update-public-api.ps1

.EXAMPLE
    pwsh -File scripts/update-public-api.ps1 src/ResXLocalization.Core/ResXLocalization.Core.csproj

.EXAMPLE
    pwsh -File scripts/update-public-api.ps1 -MarkShipped
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Project,

    [Switch] $MarkShipped
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up, whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

# The runtime projects, named rather than globbed: a glob over src/ would also pick up the source
# generator, which has no public API to track.
$runtimeProjectNames = @('ResXLocalization.Core', 'ResXLocalization.Avalonia', 'ResXLocalization.WPF')

if (-not $Project -or $Project.Count -eq 0)
{
    $Project = $runtimeProjectNames | ForEach-Object { Join-Path $repositoryRoot "src/$_/$_.csproj" }
}

$failed = $false

foreach ($projectFile in $Project)
{
    if (-not (Test-Path -LiteralPath $projectFile))
    {
        Write-Output "update-public-api: $projectFile not found - skipped."

        continue
    }

    $projectFile = (Resolve-Path -LiteralPath $projectFile).Path
    $projectDirectory = Split-Path -Parent $projectFile
    $projectName = Split-Path -Leaf $projectFile

    # dotnet format applies the RS0016 fix but never creates these files, and the analyzer stays silent
    # while they are absent - so creating them is what switches the guard on for a project.
    #
    # PublicAPI.Shipped.txt starts with '#nullable enable': the shipping projects compile with
    # Nullable=enable, and without that header the analyzer records no nullability at all and reports
    # RS0037 for every annotated member. With it, `string` and `string?` are different API entries, so a
    # nullability change to a public signature shows up as the source-breaking change it is.
    $headerPerFileName = @{
        'PublicAPI.Shipped.txt' = '#nullable enable'
        'PublicAPI.Unshipped.txt' = ''
    }

    foreach ($fileName in $headerPerFileName.Keys)
    {
        $filePath = Join-Path $projectDirectory $fileName

        if (-not (Test-Path -LiteralPath $filePath))
        {
            Set-Content -LiteralPath $filePath -Value $headerPerFileName[$fileName] -NoNewline:($headerPerFileName[$fileName] -eq '')
            Write-Output "update-public-api: created $fileName for $projectName."
        }
    }

    if ($MarkShipped)
    {
        $shippedPath = Join-Path $projectDirectory 'PublicAPI.Shipped.txt'
        $unshippedPath = Join-Path $projectDirectory 'PublicAPI.Unshipped.txt'

        $unshipped = @(Get-Content -LiteralPath $unshippedPath | Where-Object { $_.Trim() })

        if ($unshipped.Count -eq 0)
        {
            Write-Output "update-public-api: nothing unshipped in $projectName."

            continue
        }

        $shipped = @(Get-Content -LiteralPath $shippedPath | Where-Object { $_.Trim() -and $_ -ne '#nullable enable' })

        $removed = @($unshipped | Where-Object { $_.StartsWith('*REMOVED*', [StringComparison]::Ordinal) }) |
            ForEach-Object { $_.Substring('*REMOVED*'.Length) }
        $added = @($unshipped | Where-Object { -not $_.StartsWith('*REMOVED*', [StringComparison]::Ordinal) })

        $shipped = @($shipped | Where-Object { $removed -notcontains $_ }) + $added |
            Sort-Object -Unique

        Set-Content -LiteralPath $shippedPath -Value (@('#nullable enable') + $shipped)
        Set-Content -LiteralPath $unshippedPath -Value '' -NoNewline

        Write-Output "update-public-api: marked $($added.Count) added and $($removed.Count) removed API(s) as shipped in $projectName."

        continue
    }

    # From the repository root: `dotnet` resolves global.json from the CURRENT directory upward, and this
    # repository's global.json is what pins the SDK. The location is restored in the finally block.
    Push-Location -LiteralPath $repositoryRoot
    try
    {
        $output = & dotnet format analyzers $projectFile --diagnostics RS0016 --severity info -v q 2>&1
    }
    finally
    {
        Pop-Location
    }

    if ($LASTEXITCODE -ne 0)
    {
        $failed = $true
        Write-Output "update-public-api: dotnet format failed for ${projectName}:`n$output"
    }
    else
    {
        Write-Output "update-public-api: updated $projectName."
    }
}

if ($failed) { exit 1 }

Write-Output ''

if ($MarkShipped)
{
    Write-Output 'PublicAPI.Unshipped.txt is empty again. The next entry that appears there is new since this release.'
}
else
{
    Write-Output 'Review the PublicAPI.*.txt diff - it is the public-API change, and a real one also needs an'
    Write-Output 'entry under ## [Unreleased] in CHANGELOG.md and a documentation update (see CONTRIBUTING.md).'
    Write-Output 'Do not bump a version: that is the maintainer''s, at release time.'
}

exit 0
