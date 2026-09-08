<#
.SYNOPSIS
    Validates the CHANGELOG entry for a release and writes its section to artifacts/release/release-notes.md.

.DESCRIPTION
    Shared by the CI publish and GitHub-release jobs, so the notes attached to a release and the checks that
    gate the publication come from one place.

    The CHANGELOG must contain exactly one dated `## [x.y.z] - YYYY-MM-DD` heading for the version, with no
    TBD placeholder and a non-empty body. Those failures all mean the same thing - a release was tagged
    before its changelog entry was finished - and it is much cheaper to fail here than to publish immutable
    packages pointing at an empty section.

    The output goes under artifacts/, which is generated output and is ignored by git, so nothing about it
    looks authored and nothing about it can be committed by accident.

    Keep-a-Changelog link reference definitions - the `[1.1.0]: https://...` lines that a changelog collects
    at the end of the file, and that a section can also carry - are dropped from the extracted notes. They
    resolve against the changelog, not against a GitHub release page, where they render as nothing or as a
    broken link.

    On failure the script emits a GitHub Actions ::error annotation and exits non-zero.

.PARAMETER Version
    The version to extract, without the leading "v" - for example 1.0.0.

.PARAMETER OutputFile
    Where to write the extracted section. Defaults to artifacts/release/release-notes.md in the repository,
    which is what the workflow attaches to the GitHub release.

.EXAMPLE
    pwsh -File scripts/extract-release-notes.ps1 -Version 1.0.0
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [String] $Version,

    [String] $OutputFile
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up, whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$changelog = Join-Path $repositoryRoot 'CHANGELOG.md'

if (-not $OutputFile)
{
    $OutputFile = Join-Path $repositoryRoot 'artifacts/release/release-notes.md'
}

if (-not (Test-Path -LiteralPath $changelog))
{
    Write-Host "::error title=Missing changelog::$changelog does not exist."

    exit 1
}

$lines = Get-Content -LiteralPath $changelog -Encoding utf8

# --- 1. Exactly one dated heading, and no TBD ---------------------------------------------------------

$escapedVersion = [Regex]::Escape($Version)
$datedHeading = "^## \[$escapedVersion\] - \d{4}-\d{2}-\d{2}$"
$anyHeadingForVersion = "^## \[$escapedVersion\]"

$datedHeadingCount = @($lines | Where-Object { $_ -match $datedHeading }).Count
$hasTbd = @($lines | Where-Object { $_ -match "^## \[$escapedVersion\] - TBD" }).Count -gt 0

if ($datedHeadingCount -ne 1 -or $hasTbd)
{
    Write-Host "::error title=Invalid changelog::Expected exactly one dated heading for $Version in CHANGELOG.md, with no TBD. Found $datedHeadingCount dated heading(s)$(if ($hasTbd) { ' and a TBD placeholder' })."

    exit 1
}

# --- 2. The section body ------------------------------------------------------------------------------

# A link reference definition: `[1.1.0]: https://github.com/...`, at the start of a line.
$linkDefinition = '^\[[^\]]+\]:\s'

$section = New-Object System.Collections.Generic.List[String]
$inSection = $false

foreach ($line in $lines)
{
    if ($line -match $anyHeadingForVersion)
    {
        $inSection = $true

        continue
    }

    # The next release heading of any version ends this section.
    if ($inSection -and ($line -match '^## \['))
    {
        break
    }

    if ($inSection -and ($line -notmatch $linkDefinition))
    {
        $section.Add($line)
    }
}

if (-not ($section | Where-Object { $_.Trim() }))
{
    Write-Host "::error title=Empty changelog section::Release notes for $Version are empty."

    exit 1
}

$outputDirectory = Split-Path -Parent $OutputFile
if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory))
{
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

Set-Content -LiteralPath $OutputFile -Value $section -Encoding utf8

Write-Host "Wrote the CHANGELOG section for $Version to $OutputFile ($($section.Count) line(s))."

exit 0
