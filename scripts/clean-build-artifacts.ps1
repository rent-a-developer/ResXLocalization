<#
.SYNOPSIS
    Deletes generated build and documentation artifacts in the repository.

.DESCRIPTION
    MSBuild keeps stale assemblies, generated files and NuGet asset lists in bin/ and obj/. The packaging,
    Native AOT and documentation tooling also creates repository-level artifact folders. When a project is renamed, a package
    is downgraded or a source generator changes its output, those leftovers are what make a build fail or succeed
    for the wrong reason. Deleting them forces the next build to start from source.

    The scan is anchored to the repository root - the parent of this script's directory - not to the current
    working directory, so it deletes the same set no matter where you run it from.

    WHAT IT DELETES, and nothing else:

      - a fixed list of known generated directories, named below;
      - every bin/ and obj/ directory found INSIDE the resolved repository root.

    It does not guess. In particular it never deletes an XML file because its name matches a project's
    AssemblyName: an authored XML file is allowed to have that name, and a delete based on a name pattern
    cannot tell a generated documentation file from a hand-written one. Generated files that this script does
    not know about are covered by .gitignore, so `git clean -X` removes them with git's own knowledge of what
    is generated.

    Directories that are reparse points - symbolic links, junctions, mount points - are skipped rather than
    followed: deleting "recursively" through one deletes the target, which may be anywhere on the machine. The
    .git directory is skipped too; it holds no build output and walking it is pure cost.

.PARAMETER WhatIf
    List the folders that would be deleted without deleting anything.

.EXAMPLE
    pwsh -File scripts/clean-build-artifacts.ps1

.EXAMPLE
    pwsh -File scripts/clean-build-artifacts.ps1 -WhatIf
#>
#requires -Version 7.0
[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path

Write-Output "Cleaning build artifacts under $repositoryRoot..."

# The generated directories this repository creates by name. Each one is written by a tool and holds nothing
# authored. Keep this list in step with .gitignore.
$knownGeneratedDirectories = @(
    # Everything this repository generates on purpose lives under artifacts/: the packages, the Native AOT
    # publish, the generated API metadata, the documentation site, the test results and the extracted
    # release notes.
    'artifacts'
    'tests/package-consumption/.packages'
)

function Test-IsReparsePoint
{
    param([Parameter(Mandatory)] [System.IO.DirectoryInfo] $Directory)

    return $Directory.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)
}

function Test-IsInsideRepository
{
    <#
        A last check before a recursive delete: the resolved full path has to sit under the resolved
        repository root. A reparse point that was somehow followed, or a path assembled from a variable that
        turned out to be empty, cannot get past this.
    #>
    param([Parameter(Mandatory)] [String] $FullPath)

    $normalizedRoot = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar

    return $FullPath.StartsWith($normalizedRoot, [StringComparison]::OrdinalIgnoreCase)
}

$candidates = New-Object System.Collections.Generic.List[System.IO.DirectoryInfo]

foreach ($relativePath in $knownGeneratedDirectories)
{
    $directory = Get-Item -LiteralPath (Join-Path $repositoryRoot $relativePath) -Force -ErrorAction SilentlyContinue

    if ($directory -is [System.IO.DirectoryInfo] -and -not (Test-IsReparsePoint -Directory $directory))
    {
        $candidates.Add($directory)
    }
}

# -Force so that hidden or system directories are enumerated too. -Attributes !ReparsePoint stops the walk
# from descending through a link, which is what keeps a recursive delete inside this repository.
$discovered = Get-ChildItem -LiteralPath $repositoryRoot -Directory -Recurse -Force -Attributes !ReparsePoint |
    Where-Object { $_.Name -in 'bin', 'obj' } |
    Where-Object { $_.FullName -notmatch '(^|\\|/)\.git(\\|/)' }

foreach ($directory in $discovered)
{
    $candidates.Add($directory)
}

$deletedDirectories = 0

foreach ($directory in $candidates)
{
    # Already removed as part of an ancestor that matched earlier in the enumeration.
    if (-not (Test-Path -LiteralPath $directory.FullName))
    {
        continue
    }

    if (-not (Test-IsInsideRepository -FullPath $directory.FullName))
    {
        Write-Output "Skipping (outside the repository): $($directory.FullName)"

        continue
    }

    if ($PSCmdlet.ShouldProcess($directory.FullName, 'Delete folder'))
    {
        Write-Output "Deleting folder: $($directory.FullName)"

        Remove-Item -LiteralPath $directory.FullName -Recurse -Force

        $deletedDirectories++
    }
}

Write-Output "Done. Deleted $deletedDirectories folder(s)."

exit 0
