# Claude Code PostToolUse hook: format the file this edit touched - C# with CSharpier, XAML and AXAML with
# XamlStyler.
#
# The logic itself lives in scripts/tidy-code.ps1, so that Codex's hook and a human run exactly the same
# thing. This file is only the hook wiring: read the tool payload off stdin, pull the edited path out of it,
# check that the path is one we are allowed to touch, and delegate.
#
# SCOPED TO THE TRIGGERING EDIT. It formats the file named in the payload and nothing else. If the payload
# cannot be parsed, or names a path outside this repository, it says so and formats NOTHING - it never falls
# back to "every file git reports as changed", which would reformat work the user has in progress and did not
# ask this hook to touch.
#
# Formatting only, which is the default scope and takes under a second. Style and member ordering are not run
# here: `dotnet format style` needs MSBuild and ReSharper loads the whole solution, and neither belongs on the
# critical path of every single edit. All three are build errors, and scripts/pre-commit-gate.ps1 checks `-Scope
# all` before a commit, so nothing slips through.
#
# Never fails the edit - a formatter problem is surfaced as text and the hook still exits 0. A PostToolUse
# failure does not undo an edit that has already happened, so failing here would only be noise.

$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot
{
    # .claude/hooks/<this file> - two levels up. Resolved, so that the comparison below is against a real
    # path rather than against a string with ".." in it.
    return (Resolve-Path -LiteralPath (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))).Path
}

function Resolve-EligibleFile
{
    <#
        A path from the payload, turned into an absolute path this hook is allowed to format - or nothing.

        Rejected: anything that is not a .cs, .xaml or .axaml file, anything that no longer exists (a delete,
        or a move's old name), anything under bin/ or obj/, and anything that resolves outside the repository
        root. The last one is what stops a payload naming ../../etc/something, and it is checked AFTER
        resolution, so a symbolic link or junction that points out of the tree is rejected too - Resolve-Path
        follows it and the result no longer starts with the root.
    #>
    param([Parameter(Mandatory)] [String] $Root, [String] $Path)

    if ([String]::IsNullOrWhiteSpace($Path)) { return $null }
    if ([System.IO.Path]::GetExtension($Path) -notin @('.cs', '.xaml', '.axaml')) { return $null }

    if (-not [System.IO.Path]::IsPathRooted($Path))
    {
        $Path = Join-Path $Root $Path
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }

    $resolved = (Resolve-Path -LiteralPath $Path).Path

    $prefix = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { return $null }

    # Generated output is not ours to format, and reformatting it would fight the tool that wrote it.
    if ($resolved -match '[\\/](bin|obj)[\\/]') { return $null }

    return $resolved
}

try
{
    $repositoryRoot = Get-RepositoryRoot

    $raw = [Console]::In.ReadToEnd()
    if ([String]::IsNullOrWhiteSpace($raw)) { exit 0 }

    try
    {
        $payload = $raw | ConvertFrom-Json
    }
    catch
    {
        Write-Output 'tidy-code hook: could not parse the tool payload, so nothing was formatted. Run: pwsh -File scripts/tidy-code.ps1'
        exit 0
    }

    # Edit and Write both carry the path here. Nothing else is inferred: no path, no formatting.
    $file = Resolve-EligibleFile -Root $repositoryRoot -Path $payload.tool_input.file_path
    if (-not $file) { exit 0 }

    $script = Join-Path $repositoryRoot 'scripts/tidy-code.ps1'
    if (-not (Test-Path -LiteralPath $script -PathType Leaf))
    {
        Write-Output "tidy-code hook: scripts/tidy-code.ps1 not found at $script"
        exit 0
    }

    # A named mutex, so two edits landing at once cannot run two formatters over the same file. Global\ so it
    # is shared across sessions of both agents; the name is derived from the repository path, so two clones
    # do not block each other.
    $mutexName = 'Global\resxlocalization-tidy-' +
        [BitConverter]::ToString(
            [System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repositoryRoot.ToLowerInvariant()))
        ).Replace('-', '').Substring(0, 32)

    $mutex = [System.Threading.Mutex]::new($false, $mutexName)
    try
    {
        # Waiting, but not forever: a stuck hold must not wedge every later edit.
        [void] $mutex.WaitOne([TimeSpan]::FromSeconds(30))

        # A CHILD pwsh, not dot-sourcing: scripts/tidy-code.ps1 ends with `exit`, and running it in this
        # process would end the hook there - before it could report anything.
        $output = & pwsh -NoProfile -NonInteractive -File $script -Scope format -Path $file 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
    }
    finally
    {
        try { $mutex.ReleaseMutex() } catch { }
        $mutex.Dispose()
    }

    # Quiet on success. On failure say so explicitly and say what it means, because the build treats
    # formatting as an error - the same contract Codex's adapter reports through additionalContext.
    if ($exitCode -ne 0)
    {
        Write-Output "Formatting failed on $file. The build treats formatting as an error, so fix this before building:`n$output"
    }
}
catch
{
    Write-Output "tidy-code hook error: $($_.Exception.Message)"
}

exit 0
