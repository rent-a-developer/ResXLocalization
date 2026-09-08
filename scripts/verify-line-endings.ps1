<#
.SYNOPSIS
    Verifies that every file's line endings match what .gitattributes declares - both as git stored them
    and as they stand on disk.

.DESCRIPTION
    The check CI runs as "Verify line endings are normalized", plus the half CI cannot run.

    It matters here more than anywhere, because a tree with the wrong endings does not fail with a message
    about line endings. .editorconfig sets end_of_line = lf and CSharpier writes what it asks for, so a
    CRLF file is unformatted by definition and CSharpier.MsBuild fails the build with one
    "Was not formatted." error per file - naming the formatter, which is not the problem, in every one of
    them.

    Nothing else in the repository sees it first. Git normalizes on read, so `git status` reports a clean
    tree and the tidiness check reports a tidy one: scripts/tidy-code.ps1 -Check -Scope all commits the
    disposable copy before it runs the tools, which stores every file as LF whatever was on disk, and a
    formatter rewriting CRLF to LF is then a change `git diff` reports as nothing at all.

    `git ls-files --eol` is the one question that is not laundered, and it writes nothing: it reports, per
    file, what git STORED (i/), what is ON DISK (w/), and the attributes that decide both. Both columns are
    checked, and only one of them is CI's:

      i/  is CI's question, which it asks by renormalizing. It fails when a file was committed past
          .gitattributes - by a rule added after the file, or by a commit created server-side on GitHub,
          which bypasses the filter entirely.
      w/  only a working copy can answer, and it is what the compiler and the formatter actually read. A CI
          checkout is written from the index seconds earlier, so its working tree cannot disagree with it;
          a clone made with a filter that overrode the attribute, an unzipped archive, an editor that
          rewrote a file, or a copy through a tool that "helpfully" converts, all can.

    A drifted working tree is not repaired by checking it out again, which is the first thing anyone
    reaches for: git skips every file whose stat information matches the index BEFORE it considers --force,
    and a tree written wrong by whatever produced it matches perfectly. `git checkout-index --force --all`
    exits 0 without writing a byte. The files have to be deleted first, and the failure message says so.

    The expected ending is read from each file's own eol attribute rather than assumed, so this keeps
    checking the right thing if .gitattributes ever declares something else. A file that declares no eol
    attribute is not this script's business.

.EXAMPLE
    pwsh -File scripts/verify-line-endings.ps1
#>
#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up, whatever the current directory is.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

$violations = New-Object System.Collections.Generic.List[String]

foreach ($entry in @(& git -C $repositoryRoot ls-files --eol))
{
    # i/<stored> w/<on disk> attr/<attributes><TAB><path>. The path is tab-separated because it can
    # contain spaces; the three columns before it are space-padded to a fixed width.
    $pattern = '^i/(?<index>\S+)\s+w/(?<worktree>\S+)\s+attr/(?<attributes>.*?)\s*\t(?<path>.+)$'
    $parsed = [Regex]::Match($entry, $pattern)

    if (-not $parsed.Success)
    {
        continue
    }

    $declared = [Regex]::Match($parsed.Groups['attributes'].Value, '(?:^|\s)eol=(?<eol>lf|crlf)(?:\s|$)')

    if (-not $declared.Success)
    {
        continue
    }

    $expected = $declared.Groups['eol'].Value
    $stored = $parsed.Groups['index'].Value
    $onDisk = $parsed.Groups['worktree'].Value

    # 'none' is an empty file or one with no line breaks and '-text' is binary. Neither has a line ending
    # to be wrong about.
    $wrong = @($stored, $onDisk) | Where-Object { $_ -notin @('none', '-text', $expected) }

    if ($wrong)
    {
        $violations.Add("  $($parsed.Groups['path'].Value) - stored $stored, on disk $onDisk, expected $expected")
    }
}

if (-not $violations.Count)
{
    Write-Output 'Every file matches the ending .gitattributes declares.'

    exit 0
}

Write-Output "FAILED - $($violations.Count) file(s) do not match the ending .gitattributes declares:"
$violations | Select-Object -First 10 | ForEach-Object { Write-Output $_ }

if ($violations.Count -gt 10)
{
    Write-Output "  ... and $($violations.Count - 10) more"
}

Write-Output ''
Write-Output 'Wrong on disk (i/lf w/crlf): the working tree drifted from the index. Re-checking-out over it'
Write-Output 'does NOT fix it - git skips every file whose stat matches the index, which is exactly these'
Write-Output 'files, so `git checkout-index --force --all` and `git checkout -- .` both exit 0 having done'
Write-Output 'nothing. Deleting them first is what makes git write them again:'
Write-Output ''
Write-Output '  git ls-files | ForEach-Object { Remove-Item -LiteralPath $_ -Force }'
Write-Output '  git checkout -- .'
Write-Output ''
Write-Output 'That deletes tracked files before restoring them from the index, so commit or stash anything'
Write-Output 'uncommitted first. Re-run this check afterwards: if the endings come back, they are being'
Write-Output 'written by whatever produced this working tree rather than by git.'
Write-Output ''
Write-Output 'Wrong in the repository (i/crlf): git add --renormalize . and commit the result.'

exit 1
