<#
.SYNOPSIS
    Deletes generated build and documentation artifacts in the repository.

.DESCRIPTION
    MSBuild keeps stale assemblies, generated files and NuGet asset lists in bin/ and obj/. Documentation and
    packaging tooling also creates repository-level artifact folders. When a project is renamed, a package
    is downgraded or a source generator changes its output, those leftovers are what make a build fail or succeed
    for the wrong reason. Deleting them forces the next build to start from source.

    The scan is anchored to the repository root - the parent of this script's directory - not to the current
    working directory, so it deletes the same set no matter where you run it from.

    Generated XML documentation files are identified by matching each src project AssemblyName to an XML file
    beside its project file. Authored XML files with other names are left untouched.

.PARAMETER WhatIf
    List the folders and files that would be deleted without deleting anything.

.EXAMPLE
    pwsh -File scripts/clean-build-artifacts.ps1

.EXAMPLE
    pwsh -File scripts/clean-build-artifacts.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

Write-Output "Cleaning build artifacts under $repositoryRoot..."

$additionalArtifactDirectories =
    'artifacts',
    'docs/api',
    'docs/_site',
    'tests/package-consumption/.packages' |
    ForEach-Object { Get-Item -LiteralPath (Join-Path $repositoryRoot $_) -Force -ErrorAction SilentlyContinue } |
    Where-Object { $_ -is [System.IO.DirectoryInfo] }

# -Force so that hidden or system directories are enumerated too; .git is skipped because it never holds
# build output and walking it is pure cost.
$artifactDirectories =
    @($additionalArtifactDirectories) +
    @(Get-ChildItem -LiteralPath $repositoryRoot -Directory -Recurse -Force |
        Where-Object { $_.Name -in 'bin', 'obj' -and $_.FullName -notmatch '(^|\\|/)\.git(\\|/)' })

$documentationFiles =
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Filter '*.csproj' -File -Recurse |
    ForEach-Object {
        [xml] $project = Get-Content -LiteralPath $_.FullName
        $assemblyName = $project.Project.PropertyGroup.AssemblyName | Select-Object -First 1

        if ($assemblyName)
        {
            $documentationFilePath = Join-Path $_.DirectoryName "$assemblyName.xml"
            Get-Item -LiteralPath $documentationFilePath -Force -ErrorAction SilentlyContinue
        }
    } |
    Where-Object { $_ -is [System.IO.FileInfo] }

$deletedDirectories = 0
$deletedFiles = 0

foreach ($directory in $artifactDirectories)
{
    # Already removed as part of an ancestor that matched earlier in the enumeration.
    if (-not (Test-Path -LiteralPath $directory.FullName))
    {
        continue
    }

    if ($PSCmdlet.ShouldProcess($directory.FullName, 'Delete folder'))
    {
        Write-Output "Deleting folder: $($directory.FullName)"

        Remove-Item -LiteralPath $directory.FullName -Recurse -Force

        $deletedDirectories++
    }
}

foreach ($file in $documentationFiles)
{
    if ($PSCmdlet.ShouldProcess($file.FullName, 'Delete generated documentation file'))
    {
        Write-Output "Deleting generated documentation file: $($file.FullName)"

        Remove-Item -LiteralPath $file.FullName -Force

        $deletedFiles++
    }
}

Write-Output "Done. Deleted $deletedDirectories folder(s) and $deletedFiles generated documentation file(s)."

exit 0
