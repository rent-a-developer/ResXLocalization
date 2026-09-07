# Codex PostToolUse hook: format the files this edit touched - C# with CSharpier, XAML and AXAML with
# XamlStyler.
#
# The logic itself lives in scripts/tidy-code.ps1, which Claude Code's hook runs too. This file is only the
# hook wiring.
#
# SCOPED TO THE TRIGGERING EDIT, which for Codex means reading the patch. For a file edit Codex reports
# tool_name "apply_patch" and puts the patch TEXT in tool_input.command, not a file path - so the paths are
# parsed out of the patch headers:
#
#     *** Add File: src/Foo.cs        formatted
#     *** Update File: src/Foo.cs     formatted
#     *** Move to: src/Bar.cs         formatted (the new name; the old one no longer exists)
#     *** Delete File: src/Foo.cs     skipped
#
# If the patch cannot be parsed, this hook formats NOTHING and says so. It does not fall back to "every file
# git reports as changed": that would reformat work in progress that this edit did not touch, which is
# exactly the surprise a scoped hook exists to avoid.
#
# Formatting only, which is the default scope. Style and member ordering are build errors and
# scripts/pre-commit-gate.ps1 checks `-Scope all` before a commit; neither belongs on the critical path of every
# edit.
#
# Contract (https://learn.chatgpt.com/docs/hooks): exit 0 and write the response JSON to stdout. Exit code 2
# would block the operation - this hook never does that, because a formatter problem must not stop an edit.

$ErrorActionPreference = 'Stop'

function Write-HookResult
{
    param([String] $AdditionalContext)

    if ([String]::IsNullOrWhiteSpace($AdditionalContext))
    {
        $result = @{ continue = $true; suppressOutput = $true }
    }
    else
    {
        $result = @{
            continue = $true
            hookSpecificOutput = @{
                hookEventName = 'PostToolUse'
                additionalContext = $AdditionalContext
            }
        }
    }

    $result | ConvertTo-Json -Depth 5 -Compress | Write-Output
}

function Get-RepositoryRoot
{
    # .codex/hooks/<this file> - two levels up. Anchored to this file rather than asked of git, so that the
    # answer does not depend on the current directory or on git being on PATH.
    return (Resolve-Path -LiteralPath (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))).Path
}

function Get-PatchPath
{
    <#
        The paths an apply_patch command touches, from its headers. Deletions are skipped: there is nothing
        left to format. A rename is reported as an Update of the old name followed by a Move to the new one,
        so both are collected and the filter below drops the old name, which no longer exists.
    #>
    param([String] $Command)

    $paths = New-Object System.Collections.Generic.List[String]

    if ([String]::IsNullOrWhiteSpace($Command)) { return $paths }

    foreach ($line in ($Command -split "`r?`n"))
    {
        $match = [Regex]::Match($line, '^\s*\*\*\*\s+(Add File|Update File|Move to):\s*(.+?)\s*$')
        if ($match.Success)
        {
            $paths.Add($match.Groups[2].Value)
        }
    }

    return $paths
}

function Resolve-EligibleFile
{
    <#
        A path from the patch, turned into an absolute path this hook is allowed to format - or nothing.

        Rejected: anything that is not a .cs, .xaml or .axaml file, anything that no longer exists, anything
        under bin/ or obj/, and anything that resolves outside the repository root. The last check happens
        AFTER resolution, so a symbolic link or junction pointing out of the tree is rejected too.
    #>
    param([Parameter(Mandatory)] [String] $Root, [String] $Path)

    if ([String]::IsNullOrWhiteSpace($Path)) { return $null }

    $Path = $Path.Trim('"')
    if ([System.IO.Path]::GetExtension($Path) -notin @('.cs', '.xaml', '.axaml')) { return $null }

    if (-not [System.IO.Path]::IsPathRooted($Path))
    {
        $Path = Join-Path $Root $Path
    }

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }

    $resolved = (Resolve-Path -LiteralPath $Path).Path

    $prefix = $Root.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { return $null }

    if ($resolved -match '[\\/](bin|obj)[\\/]') { return $null }

    return $resolved
}

try
{
    $repositoryRoot = Get-RepositoryRoot

    $raw = [Console]::In.ReadToEnd()
    if ([String]::IsNullOrWhiteSpace($raw))
    {
        Write-HookResult
        exit 0
    }

    try
    {
        $payload = $raw | ConvertFrom-Json
    }
    catch
    {
        Write-HookResult -AdditionalContext 'tidy-code hook: the tool payload could not be parsed, so nothing was formatted. Run `pwsh -File scripts/tidy-code.ps1` yourself.'
        exit 0
    }

    $candidates = New-Object System.Collections.Generic.List[String]

    # apply_patch puts the patch in .command; Edit and Write carry a plain path. Both are read, and nothing
    # is inferred beyond them.
    # [string[]] @(...) on purpose: PowerShell unrolls a one-element list to a bare string on the way
    # out of a function, and AddRange cannot take one.
    $candidates.AddRange([string[]] @(Get-PatchPath -Command $payload.tool_input.command))
    if ($payload.tool_input.file_path) { $candidates.Add([String] $payload.tool_input.file_path) }

    $files = @(
        $candidates |
            ForEach-Object { Resolve-EligibleFile -Root $repositoryRoot -Path $_ } |
            Where-Object { $_ } |
            Select-Object -Unique
    )

    if ($files.Count -eq 0)
    {
        Write-HookResult
        exit 0
    }

    $script = Join-Path $repositoryRoot 'scripts/tidy-code.ps1'
    if (-not (Test-Path -LiteralPath $script -PathType Leaf))
    {
        Write-HookResult -AdditionalContext "tidy-code hook: scripts/tidy-code.ps1 not found at $script"
        exit 0
    }

    # A named mutex, so two edits landing at once cannot run two formatters over the same file. The name is
    # derived from the repository path, so two clones do not block each other, and it is the same name the
    # Claude adapter uses - the two agents serialize against each other as well.
    $mutexName = 'Global\resxlocalization-tidy-' +
        [BitConverter]::ToString(
            [System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repositoryRoot.ToLowerInvariant()))
        ).Replace('-', '').Substring(0, 32)

    $mutex = [System.Threading.Mutex]::new($false, $mutexName)
    try
    {
        [void] $mutex.WaitOne([TimeSpan]::FromSeconds(30))

        # A CHILD pwsh, not dot-sourcing: scripts/tidy-code.ps1 ends with `exit`, which in this process would
        # end the hook before it could write its protocol response - and a Codex hook that writes nothing is
        # a hook that failed.
        $output = & pwsh -NoProfile -NonInteractive -File $script -Scope format -Path @files 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
    }
    finally
    {
        try { $mutex.ReleaseMutex() } catch { }
        $mutex.Dispose()
    }

    # Quiet on success: the agent does not need to be told that nothing needed formatting. A failure is
    # reported, because it means the next build breaks on a formatting rule.
    if ($exitCode -ne 0)
    {
        Write-HookResult -AdditionalContext "Formatting failed. The build treats formatting as an error, so fix this before building:`n$output"
    }
    else
    {
        Write-HookResult
    }
}
catch
{
    Write-HookResult -AdditionalContext "tidy-code hook error: $($_.Exception.Message)"
}

exit 0
