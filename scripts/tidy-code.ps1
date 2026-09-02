<#
.SYNOPSIS
    Applies this repository's formatting: C# whitespace, XAML layout and line endings.

.DESCRIPTION
    Three concerns, three tools, no overlap between them:

      C# whitespace   indentation, blank lines, wrapping    dotnet format whitespace
      XAML layout     attribute placement and ordering      XamlStyler (xstyler)
      line endings    LF everywhere, per .gitattributes     this script

    Semantic style and the analyzer rules are not here: the build enforces them itself, through
    EnforceCodeStyleInBuild and TreatWarningsAsErrors.

    Line endings come last because XamlStyler cannot produce them. It always writes the host OS's
    newline and has no setting for it, so on Windows it turns every XAML file it touches into CRLF.
    Git stores LF regardless, but the working tree would keep reporting those files as modified with an
    empty `git diff`. Rewriting them to LF here removes that. On Linux and macOS there is nothing to do.

    Needs the local tools: run `dotnet tool restore` once per clone, or let this script do it.

.PARAMETER Check
    Report whether the tree is already formatted, and exit non-zero if it is not. This is what CI runs.

    It still WRITES. XamlStyler has no check mode that is meaningful here: on Windows its passive check
    fails on the LF files this repository stores, whatever their layout, because the tool compares
    against its own CRLF output. So the honest question is not "does each tool approve?" but "does
    formatting the tree change it?" - which is what this asks, and which gives the same answer on every
    OS. Do not point -Check at a working tree you are not ready to have formatted.

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1

.EXAMPLE
    pwsh -File scripts/tidy-code.ps1 -Check
#>

[CmdletBinding()]
param(
    [Switch] $Check
)

$ErrorActionPreference = 'Stop'

# scripts/<this file> - the repository root is one level up.
$repositoryRoot = Split-Path -Parent $PSScriptRoot

# Windows PowerShell 5.1 has no $IsWindows variable, and an undefined variable is $null - so testing
# $IsWindows alone reports "not Windows" on the one host that is always Windows. The version test comes
# first for that reason.
$onWindows = $PSVersionTable.PSVersion.Major -lt 6 -or $IsWindows

# The WPF projects only load on Windows; elsewhere the solution filter leaves them out.
$solution = if ($onWindows) { 'ResXLocalization.slnx' } else { 'ResXLocalization.NonWindows.slnf' }

$failures = New-Object System.Collections.Generic.List[String]

function Invoke-Tool {
    <#
        Runs `dotnet ...` and returns everything it printed. The caller decides success from $LASTEXITCODE.

        $ErrorActionPreference is deliberately relaxed for the call. With it at 'Stop', PowerShell turns
        anything a native program writes to stderr into a terminating error, so a harmless warning would
        abort the whole script with NativeCommandError. Exit codes decide success here, not stderr.
    #>
    param([Parameter(Mandatory)] [String[]] $Arguments)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { return (& dotnet @Arguments 2>&1 | Out-String) }
    finally { $ErrorActionPreference = $previous }
}

function Get-Fingerprint {
    # A hash over the content of every file the tools touch, used by -Check to tell whether formatting
    # changed anything.
    #
    # The list comes from git, not from Get-ChildItem: build output under obj/ and bin/ is on disk but
    # ignored, and hashing it would make the check fail on files nothing formatted.
    $patterns = @('*.cs', '*.xaml', '*.axaml', '*.resx')
    $relativePaths = & git -C $repositoryRoot ls-files --cached --others --exclude-standard -- $patterns 2>$null |
        Where-Object { $_ } |
        Sort-Object

    # Fail loudly rather than hashing nothing. git's stderr goes to $null above, so a git that fails
    # returns no paths instead of an error - and two hashes of an empty stream compare equal, which
    # would report a formatted tree without having looked at a single file. This is CI's only
    # formatting gate; it must not be able to pass by accident.
    if (-not $relativePaths) {
        throw 'tidy-code: git listed no files. Is this a git repository, and is git on PATH?'
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $accumulator = New-Object System.IO.MemoryStream
        foreach ($relativePath in $relativePaths) {
            $fullPath = Join-Path $repositoryRoot $relativePath
            if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { continue }

            # The path goes into the hash too, so that adding or removing a file is a change.
            $pathBytes = [System.Text.Encoding]::UTF8.GetBytes($relativePath)
            $accumulator.Write($pathBytes, 0, $pathBytes.Length)

            $bytes = [System.IO.File]::ReadAllBytes($fullPath)
            $accumulator.Write($bytes, 0, $bytes.Length)
        }

        return [System.BitConverter]::ToString($sha.ComputeHash($accumulator.ToArray()))
    }
    finally { $sha.Dispose() }
}

# --- C# whitespace --------------------------------------------------------------------------------

function Invoke-CSharpFormat {
    # No --no-restore: this has to work on a fresh clone. When the packages are already restored -
    # locally after a build, and in CI after the restore step - it costs little.
    $output = Invoke-Tool -Arguments @('format', 'whitespace', $solution)
    if ($LASTEXITCODE -ne 0) { $failures.Add("dotnet format whitespace:`n$output") }
}

# --- XAML layout ----------------------------------------------------------------------------------

function Invoke-XamlFormat {
    foreach ($directory in @('src', 'samples')) {
        $target = Join-Path $repositoryRoot $directory
        if (-not (Test-Path -LiteralPath $target)) { continue }

        $output = Invoke-Tool -Arguments @('xstyler', '--recursive', '--directory', $target)
        if ($LASTEXITCODE -ne 0) { $failures.Add("dotnet xstyler ($directory):`n$output") }
    }
}

# --- Line endings ---------------------------------------------------------------------------------
# Always last: XamlStyler above writes the host newline and cannot be told otherwise.

function Invoke-LineEndingFix {
    # Byte-level, so the encoding and any byte order mark survive. A file that needs no change is not
    # written at all, so unchanged files keep their timestamp.
    $patterns = @('*.xaml', '*.axaml', '*.resx')
    $relativePaths = @(& git -C $repositoryRoot ls-files -- $patterns 2>$null)

    $rewritten = New-Object System.Collections.Generic.List[String]

    foreach ($relativePath in $relativePaths) {
        if ([String]::IsNullOrWhiteSpace($relativePath)) { continue }

        $fullPath = Join-Path $repositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { continue }

        $bytes = [System.IO.File]::ReadAllBytes($fullPath)
        $output = New-Object System.Collections.Generic.List[Byte]

        # Drop every CR that is directly followed by LF. A lone CR is left alone, and a UTF-16 file
        # never matches this pattern - its CR is followed by a NUL - so neither can be corrupted here.
        for ($index = 0; $index -lt $bytes.Length; $index++) {
            if ($bytes[$index] -eq 13 -and ($index + 1) -lt $bytes.Length -and $bytes[$index + 1] -eq 10) {
                continue
            }

            $output.Add($bytes[$index])
        }

        if ($output.Count -ne $bytes.Length) {
            [System.IO.File]::WriteAllBytes($fullPath, $output.ToArray())
            $rewritten.Add($relativePath)
        }
    }

    # Rewriting a file leaves git's cached stat information stale, which on its own keeps the file
    # listed as modified by `git status`. Clear that, but only where doing so cannot stage anything:
    # the file's blob must already equal what the index holds. A file with real changes is left alone,
    # so this never stages work on the developer's behalf.
    foreach ($relativePath in $rewritten) {
        $indexBlob = (& git -C $repositoryRoot ls-files -s -- $relativePath) -split '\s+' | Select-Object -Index 1
        $fileBlob = & git -C $repositoryRoot hash-object --path $relativePath -- (Join-Path $repositoryRoot $relativePath)

        if ($indexBlob -and $fileBlob -and $indexBlob -eq $fileBlob) {
            & git -C $repositoryRoot add -- $relativePath
        }
    }
}

# --- Run ------------------------------------------------------------------------------------------

$output = Invoke-Tool -Arguments @('tool', 'restore', '--configfile', (Join-Path $repositoryRoot 'NuGet.config'))
if ($LASTEXITCODE -ne 0) {
    Write-Output "tidy-code: dotnet tool restore:`n$output"
    exit 1
}

$before = if ($Check) { Get-Fingerprint } else { $null }

Invoke-CSharpFormat
Invoke-XamlFormat
Invoke-LineEndingFix

if ($Check -and -not $failures.Count -and (Get-Fingerprint) -ne $before) {
    $failures.Add('the tree is not formatted. Run: pwsh -File scripts/tidy-code.ps1')
}

if (-not $failures.Count) {
    Write-Output "tidy-code: repository $(if ($Check) { 'checked' } else { 'formatted' })."
}
else {
    $failures | ForEach-Object { Write-Output "tidy-code: $_" }
    exit 1
}
