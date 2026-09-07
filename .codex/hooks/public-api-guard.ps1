# Codex PostToolUse hook: remind about the companion edits a public API change needs.
#
# The checklist itself lives in scripts/public-api-guard.ps1, which Claude Code's hook runs too. This file is
# only the hook wiring.
#
# SCOPED TO THE TRIGGERING EDIT. For a file edit Codex reports tool_name "apply_patch" and puts the patch TEXT
# in tool_input.command, so the paths come from the patch headers - Add File, Update File and Move to; a
# Delete File is skipped, because a deleted snapshot has nothing to check. If the patch cannot be parsed this
# hook checks nothing and says so. It never falls back to every file git reports as changed, which would nag
# about work this edit did not touch.
#
# READ-ONLY. The shared script only ever prints; it stages nothing, rewrites no API snapshot, and installs
# nothing.
#
# Contract (https://learn.chatgpt.com/docs/hooks): exit 0 and write the response JSON to stdout. The checklist
# is returned as additionalContext, so it reaches the model rather than only the user.

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

function Get-PatchPath
{
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

try
{
    # .codex/hooks/<this file> - two levels up. Anchored to this file rather than asked of git, so the answer
    # does not depend on the current directory or on git being on PATH.
    $repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))).Path

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
        Write-HookResult -AdditionalContext 'public-api-guard hook: the tool payload could not be parsed. Run `pwsh -File scripts/public-api-guard.ps1` yourself.'
        exit 0
    }

    $candidates = New-Object System.Collections.Generic.List[String]
    # [string[]] @(...) on purpose: PowerShell unrolls a one-element list to a bare string on the way
    # out of a function, and AddRange cannot take one.
    $candidates.AddRange([string[]] @(Get-PatchPath -Command $payload.tool_input.command))
    if ($payload.tool_input.file_path) { $candidates.Add([String] $payload.tool_input.file_path) }

    $prefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    $files = New-Object System.Collections.Generic.List[String]
    foreach ($candidate in $candidates)
    {
        $path = $candidate.Trim('"')
        if ((Split-Path -Leaf $path) -notin @('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt')) { continue }

        if (-not [System.IO.Path]::IsPathRooted($path)) { $path = Join-Path $repositoryRoot $path }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }

        $resolved = (Resolve-Path -LiteralPath $path).Path
        if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { continue }

        if (-not $files.Contains($resolved)) { $files.Add($resolved) }
    }

    if ($files.Count -eq 0)
    {
        Write-HookResult
        exit 0
    }

    $script = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'
    if (-not (Test-Path -LiteralPath $script -PathType Leaf))
    {
        Write-HookResult -AdditionalContext "public-api-guard hook: scripts/public-api-guard.ps1 not found at $script"
        exit 0
    }

    # A CHILD pwsh: the shared script ends with `exit`, which in this process would end the hook before it
    # could write its protocol response - and a Codex hook that writes nothing is a hook that failed.
    $output = & pwsh -NoProfile -NonInteractive -File $script -Path @files 2>&1 | Out-String

    Write-HookResult -AdditionalContext $output.Trim()
}
catch
{
    Write-HookResult -AdditionalContext "public-api-guard hook error: $($_.Exception.Message)"
}

exit 0
