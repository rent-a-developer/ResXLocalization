<#
.SYNOPSIS
    Prints the CONTRIBUTING.md companion-edit checklist when a project's public API files change.

.DESCRIPTION
    The public surface of the shipping projects is declared in their PublicAPI.Shipped.txt and
    PublicAPI.Unshipped.txt files and enforced by Microsoft.CodeAnalysis.PublicApiAnalyzers - the build
    fails on a public member that is not declared (RS0016) or declared but gone (RS0017), so the build
    already stops an *accidental* change.

    What the build cannot know is whether a *deliberate* one was accompanied by its companion edits.
    CONTRIBUTING.md requires an Unreleased changelog entry and a documentation update, and both are easy
    to forget, so this reminds you when one of those files moves. It never asks for a version bump: the
    version belongs to the maintainer and moves at release time.

    This is the shared implementation. AI agents call it from a PostToolUse hook - Claude Code through
    .claude/hooks/public-api-guard.ps1 and Codex through .codex/hooks/public-api-guard.ps1 - and
    scripts/pre-commit-gate.ps1 runs it over the whole working tree. It only ever reports; it never fails
    anything.

.PARAMETER Path
    One or more paths that were just edited. With no Path, every file git reports as changed (tracked
    modifications plus untracked files) is examined, which is what an agent whose tool payload does not
    carry a file path needs.

.EXAMPLE
    pwsh -File scripts/public-api-guard.ps1

.EXAMPLE
    pwsh -File scripts/public-api-guard.ps1 src/ResXLocalization.Core/PublicAPI.Unshipped.txt
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [String[]] $Path
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up, whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

function Get-ChangedFile
{
    param([String] $RepositoryRoot)

    # 2>$null: git warns about line-ending normalization per file, which is noise here.
    $tracked = & git -C $RepositoryRoot diff --name-only HEAD 2>$null
    $untracked = & git -C $RepositoryRoot ls-files --others --exclude-standard 2>$null

    return @($tracked) + @($untracked) | Where-Object { $_ }
}

if (-not $Path -or $Path.Count -eq 0)
{
    $Path = Get-ChangedFile -RepositoryRoot $repositoryRoot
}

$changedApiFiles = @($Path) |
    Where-Object { $_ } |
    Where-Object { (Split-Path -Leaf $_) -in @('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt') } |
    ForEach-Object { $_ -replace '\\', '/' } |
    Select-Object -Unique

if ($changedApiFiles.Count -eq 0) { exit 0 }

Write-Output @"
The declared public API changed:

$(($changedApiFiles | ForEach-Object { "  $_" }) -join "`n")

Per CONTRIBUTING.md a public-surface change also requires:

  1. CHANGELOG.md  - an entry under '## [Unreleased]', in Keep-a-Changelog format. Write a breaking
                     change as '- **BREAKING:** ...'.
  2. Documentation - the guide or reference page under docs/ that describes the API, and any example the
                     change makes wrong.

A change to one UI package usually needs the same change in the other: ResXLocalization.Avalonia and
ResXLocalization.WPF mirror each other's markup extensions and converters. If only one of them moved,
say why in the pull request.

You do NOT bump a version. <Version> in the repository-root Directory.Build.props, the release date in
the CHANGELOG, promoting PublicAPI.Unshipped.txt to Shipped, and the tag are all the maintainer's, at
release time. Describing the change accurately under Unreleased is what lets them choose the number.

Review the diff line by line first - it is the guard that this change is deliberate, not accidental. An
entry starting with *REMOVED* is a break: it means a member that shipped is gone.
"@

exit 0
