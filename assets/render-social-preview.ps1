<#
.SYNOPSIS
    Renders social-preview.html into social-preview.png (1280 x 640) with headless Edge or Chrome.

.PARAMETER Scale
    Device scale factor. 1 (default) gives 1280 x 640, GitHub's recommended size; 2 gives 2560 x 1280.
#>
param(
    [int] $Scale = 1
)

$ErrorActionPreference = 'Stop'

$html = Join-Path $PSScriptRoot 'social-preview.html'
$png = Join-Path $PSScriptRoot $(if ($Scale -eq 1) { 'social-preview.png' } else { "social-preview@${Scale}x.png" })

$browser = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $browser) {
    throw 'Neither Microsoft Edge nor Google Chrome was found.'
}

# A throwaway profile keeps the headless run away from the user's own browser profile.
$profileDir = Join-Path ([IO.Path]::GetTempPath()) "social-preview-render-$PID"

# Start-Process -Wait: invoked with &, Edge hands off to a broker and returns before the screenshot exists.
Start-Process -FilePath $browser -Wait -WindowStyle Hidden -ArgumentList @(
    '--headless=new',
    '--disable-gpu',
    '--hide-scrollbars',
    '--no-first-run',
    "--user-data-dir=`"$profileDir`"",
    "--force-device-scale-factor=$Scale",
    '--window-size=1280,640',
    '--virtual-time-budget=8000',
    "--screenshot=`"$png`"",
    ([Uri] $html).AbsoluteUri
)

if (-not (Test-Path $png)) {
    throw "The browser did not write $png."
}

Remove-Item $profileDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Output $png
