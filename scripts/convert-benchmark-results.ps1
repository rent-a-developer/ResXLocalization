<#
.SYNOPSIS
    Merges the BenchmarkDotNet reports of the benchmark projects into the two files the CI benchmark comparison
    reads: times.json (mean time) and allocations.json (allocated bytes).

.DESCRIPTION
    benchmark-action/github-action-benchmark reads one BenchmarkDotNet JSON file and compares only the mean
    time. Every benchmark class writes its own full JSON report, so this script

      - merges every *-report-full.json below -ReportDirectory into times.json: their Benchmarks arrays one
        after the other, and the host information of the first report, for the "Time" series (tool:
        benchmarkdotnet);
      - writes allocations.json in the action's customSmallerIsBetter format, one entry per benchmark with its
        allocated bytes per operation, for the "Allocations" series.

    An allocation is written as its bytes plus 1. The action computes a ratio to the value on main, and a
    benchmark that allocated nothing there would otherwise be a division by zero instead of an alert when it
    starts to allocate.

    A benchmark name is BenchmarkDotNet's FullName, which includes its class and the namespace of its project,
    so the names of the classes and projects cannot collide; the script fails if two benchmarks share one anyway, or if a benchmark has
    no result, because a missing row would silently drop out of the comparison.

.PARAMETER ReportDirectory
    The directory searched, recursively, for the BenchmarkDotNet *-report-full.json files.

.PARAMETER OutputDirectory
    The directory times.json and allocations.json are written to. It is created if it does not exist.

.EXAMPLE
    pwsh -File scripts/convert-benchmark-results.ps1 -ReportDirectory artifacts/benchmarks -OutputDirectory artifacts/benchmarks/series
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ReportDirectory,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

$reportFiles = @(
    Get-ChildItem -LiteralPath $ReportDirectory -Recurse -File -Filter '*-report-full.json' | Sort-Object FullName
)

if ($reportFiles.Count -eq 0)
{
    throw "No BenchmarkDotNet *-report-full.json file was found below $ReportDirectory."
}

$reports = @($reportFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
$benchmarks = @($reports | ForEach-Object { $_.Benchmarks })
$allocations = New-Object System.Collections.Generic.List[Object]
$names = New-Object System.Collections.Generic.HashSet[String]

foreach ($benchmark in $benchmarks)
{
    if (-not $names.Add($benchmark.FullName))
    {
        throw "Two benchmarks are named $($benchmark.FullName)."
    }

    if ($null -eq $benchmark.Statistics -or $null -eq $benchmark.Memory)
    {
        throw "$($benchmark.FullName) has no result. Its run failed; see the BenchmarkDotNet log."
    }

    $allocations.Add(
        [ordered]@{
            name = $benchmark.FullName
            unit = 'bytes'
            value = [long] $benchmark.Memory.BytesAllocatedPerOperation + 1
        }
    )
}

$times = $reports[0] | Select-Object -Property * -ExcludeProperty Benchmarks
$times | Add-Member -NotePropertyName Benchmarks -NotePropertyValue $benchmarks

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$times | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'times.json') -Encoding utf8NoBOM
ConvertTo-Json -InputObject $allocations -Depth 4 |
    Set-Content -LiteralPath (Join-Path $OutputDirectory 'allocations.json') -Encoding utf8NoBOM

Write-Output "Merged $($benchmarks.Count) benchmark(s) from $($reportFiles.Count) report(s) into $OutputDirectory."
