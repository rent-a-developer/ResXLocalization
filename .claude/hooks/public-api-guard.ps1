# Claude Code PostToolUse hook: remind about the companion edits a public API change needs.
#
# The checklist itself lives in scripts/public-api-guard.ps1, so that Codex's hook and the pre-commit gate produce
# exactly the same text. This file is only the hook wiring: read the tool payload off stdin, pull the edited
# path out of it, and delegate.
#
# SCOPED TO THE TRIGGERING EDIT: it passes the one path the payload names. If the payload cannot be parsed it
# says so and checks nothing - it never falls back to every file git reports as changed, which would nag
# about work this edit did not touch.
#
# READ-ONLY. The shared script only ever prints; it stages nothing, rewrites no API snapshot, and installs
# nothing. Never fails the edit - a problem here is surfaced as text and the hook still exits 0.

$ErrorActionPreference = 'Stop'

try {
    # .claude/hooks/<this file> - two levels up.
    $repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))).Path

    $raw = [Console]::In.ReadToEnd()
    if ([String]::IsNullOrWhiteSpace($raw)) { exit 0 }

    try {
        $payload = $raw | ConvertFrom-Json
    }
    catch {
        Write-Output 'public-api-guard hook: could not parse the tool payload. Run: pwsh -File scripts/public-api-guard.ps1'
        exit 0
    }

    $filePath = $payload.tool_input.file_path
    if ([String]::IsNullOrWhiteSpace($filePath)) { exit 0 }

    # Only the two snapshot files matter, so the check costs nothing on every other edit.
    if ((Split-Path -Leaf $filePath) -notin @('PublicAPI.Shipped.txt', 'PublicAPI.Unshipped.txt')) { exit 0 }

    if (-not [System.IO.Path]::IsPathRooted($filePath)) { $filePath = Join-Path $repositoryRoot $filePath }
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) { exit 0 }

    $resolved = (Resolve-Path -LiteralPath $filePath).Path
    $prefix = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { exit 0 }

    $script = Join-Path $repositoryRoot 'scripts/public-api-guard.ps1'
    if (-not (Test-Path -LiteralPath $script -PathType Leaf)) {
        Write-Output "public-api-guard hook: scripts/public-api-guard.ps1 not found at $script"
        exit 0
    }

    # A CHILD pwsh: the shared script ends with `exit`, which in this process would end the hook there.
    & pwsh -NoProfile -NonInteractive -File $script -Path $resolved
}
catch {
    Write-Output "public-api-guard hook error: $($_.Exception.Message)"
}

exit 0
