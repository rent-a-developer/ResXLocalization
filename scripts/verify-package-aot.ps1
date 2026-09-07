<#
.SYNOPSIS
    Publishes the Avalonia package consumer with Native AOT, gates its IL diagnostics, and runs the native
    binary.

.DESCRIPTION
    This is the only check in the repository that can catch silent trimming damage. The library finds
    translations through ResourceManager and satellite assemblies, and the generated typed keys carry
    resource names as strings - none of which the JIT can lose, and all of which a trimmer can. Nothing is
    trimmed on the JIT, which is why the whole test suite passes while a trimmed application finds no
    translation at all.

    The consumer reaches the library through PackageReference, never through a project reference. That
    matters: the packed source generator, the buildTransitive resx wiring that feeds it, the satellite
    assemblies and the trimming metadata all have to survive packing, and a project-referenced test would
    pass even if packing dropped every one of them.

    Five things have to hold, and all five are gated here:

    1. The expected packages exist locally, at the expected version, by the id and version in their own
       nuspec - not by their file name.
    2. They restore into a consumer that has nothing but a PackageReference, from the local feed and never
       from nuget.org.
    3. The Native AOT publish succeeds and produces the native binary and the German satellite assembly.
    4. NO IL2xxx/IL3xxx diagnostic is reported, from anywhere - not from the library, not from a package in
       the closure, and not at the consumer's own call sites.
    5. The native binary RUNS and every assertion in it passes. A file that exists is not a check.

    ResXLocalization.WPF is out of scope: WPF does not support Native AOT.

.PARAMETER Framework
    The target framework to publish. net8.0 is the LTS floor the packages support; net10.0 is current and
    the default.

.PARAMETER Runtime
    The runtime identifier to publish for. Defaults to win-x64 on Windows and linux-x64 elsewhere.

.PARAMETER PackageVersion
    The version of the packages to consume. Defaults to the <Version> in the repository-root
    Directory.Build.props, which is what `dotnet pack` produces. CI passes the exact version of the
    artifacts the publish job produced.

.PARAMETER Configuration
    Build configuration. Release by default, because that is what CI uses.

.PARAMETER Pack
    Pack first, into artifacts/packages. On Windows that is every shipping project; elsewhere the WPF
    project cannot build, so Core and Avalonia are packed on their own - which is all this gate consumes.
    Full release packaging stays on Windows.

    CI does not use this: it downloads the exact packages the publish job produced.

.NOTES
    A native publish needs a C++ toolchain. On Windows that is MSVC, and vswhere.exe must be resolvable or
    the link step fails with a misleading MSB3073 - this script puts the Visual Studio Installer directory
    on PATH for that reason. On Linux it needs clang and zlib1g-dev.

.EXAMPLE
    pwsh -File scripts/verify-package-aot.ps1 -Pack

.EXAMPLE
    pwsh -File scripts/verify-package-aot.ps1 -Framework net8.0
#>
#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('net8.0', 'net10.0')]
    [String] $Framework = 'net10.0',

    [String] $Runtime,

    [String] $PackageVersion,

    [String] $Configuration = 'Release',

    [Switch] $Pack
)

$ErrorActionPreference = 'Stop'

if (-not $Runtime)
{
    $Runtime = $IsWindows ? 'win-x64' : 'linux-x64'
}

$supportedRuntimes = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')

if ($Runtime -notin $supportedRuntimes)
{
    Write-Host "FAILED. '$Runtime' is not one of the runtime identifiers this gate supports: $($supportedRuntimes -join ', ')." -ForegroundColor Red

    exit 1
}

# scripts/<this file> - the repository root is one level up, whatever the current directory is. Every path
# below is anchored to it.
$repositoryRoot = (Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$consumerDirectory = Join-Path $repositoryRoot 'tests/package-consumption/AvaloniaConsumer'
$packageDirectory = Join-Path $repositoryRoot 'artifacts/packages'
$packageCache = Join-Path $repositoryRoot 'tests/package-consumption/.packages'
$consumerNuGetConfig = Join-Path $repositoryRoot 'tests/package-consumption/nuget.config'
$publishDirectory = Join-Path $repositoryRoot "artifacts/package-aot/$Framework-$Runtime"
$logDirectory = Join-Path $repositoryRoot 'artifacts/package-aot/logs'

# The Avalonia gate consumes these two. WPF is not among them and never will be.
$requiredPackageIds = @('ResXLocalization.Core', 'ResXLocalization.Avalonia')

if (-not $PackageVersion)
{
    # The single source of truth for the version of every package. Reading it here keeps this script
    # correct across a release bump without a second place to edit.
    $sharedProperties = Join-Path $repositoryRoot 'Directory.Build.props'
    $PackageVersion = ([Xml] (Get-Content -Raw -LiteralPath $sharedProperties)).Project.PropertyGroup.Version |
        Where-Object { $_ } |
        Select-Object -First 1

    if (-not $PackageVersion)
    {
        Write-Host "FAILED. No <Version> found in $sharedProperties." -ForegroundColor Red

        exit 1
    }
}

# Everything below runs inside a try/finally that restores the caller's PATH, environment and working
# location, so an interrupted run leaves the shell as it found it. `exit` inside a try block still runs the
# finally.
#
# The working directory is not cosmetic: `dotnet` resolves global.json from the CURRENT directory upward,
# and this repository's global.json is what pins the SDK the packages are built with.
$originalPath = $env:PATH
$originalNuGetPackages = $env:NUGET_PACKAGES

Push-Location -LiteralPath $repositoryRoot
try
{
    if ($IsWindows)
    {
        # Without vswhere.exe on PATH the native link step fails with MSB3073 and a message about a command
        # that is not recognized, which names neither the toolchain nor this script's own prerequisite.
        $visualStudioInstaller = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'

        if ((Test-Path -LiteralPath $visualStudioInstaller) -and ($env:PATH -notlike "*$visualStudioInstaller*"))
        {
            $env:PATH = "$visualStudioInstaller;$env:PATH"
        }
    }

    # --- The packages under test ---------------------------------------------------------------------

    if ($Pack)
    {
        if ($IsWindows)
        {
            Write-Host "Packing the shipping projects ($PackageVersion)..." -ForegroundColor Cyan

            & dotnet pack (Join-Path $repositoryRoot 'ResXLocalization.slnx') --configuration $Configuration --output $packageDirectory
        }
        else
        {
            # ResXLocalization.WPF targets net*-windows and cannot build here. Packing the two this gate
            # needs is the honest thing to do; a full release pack stays on Windows.
            Write-Host "Packing ResXLocalization.Core and ResXLocalization.Avalonia ($PackageVersion) - WPF needs Windows..." -ForegroundColor Cyan

            foreach ($packageId in $requiredPackageIds)
            {
                & dotnet pack (Join-Path $repositoryRoot "src/$packageId/$packageId.csproj") `
                    --configuration $Configuration --output $packageDirectory

                if ($LASTEXITCODE -ne 0) { break }
            }
        }

        if ($LASTEXITCODE -ne 0)
        {
            Write-Host 'FAILED. dotnet pack did not succeed.' -ForegroundColor Red

            exit 1
        }
    }

    # The consumer's package cache is emptied on EVERY run, not only after a pack. NuGet resolves by
    # version and not by content, so a cache entry for 1.1.0 satisfies a reference to 1.1.0 whatever bytes
    # produced it - and this script exists to test THESE bytes. A stale entry would turn the gate into a
    # re-run of whatever passed last time.
    #
    # Which is why a cache that CANNOT be deleted is a hard failure rather than a warning. NuGet extracts
    # packages read-only, and a build server started by an earlier run can still hold a handle to one of
    # them for a few seconds, so the delete is retried - but if it never succeeds, the gate stops instead
    # of running against whatever is left behind.
    if (Test-Path -LiteralPath $packageCache)
    {
        Get-ChildItem -LiteralPath $packageCache -Recurse -Force -File -ErrorAction SilentlyContinue |
            Where-Object { $_.IsReadOnly } |
            ForEach-Object { $_.IsReadOnly = $false }

        # The build server is asked to stop before EVERY attempt, not once. It is the usual holder - an
        # MSBuild or ILC node from the previous run keeps package assemblies loaded - and when this gate
        # runs both frameworks back to back, the first leg's nodes are still exiting while the second
        # leg starts. One shutdown up front is not enough for that; measured, it needs a few seconds
        # and another ask.
        $removed = $false

        for ($attempt = 1; $attempt -le 8; $attempt++)
        {
            & dotnet build-server shutdown 2>&1 | Out-Null

            try
            {
                Remove-Item -Recurse -Force -LiteralPath $packageCache -ErrorAction Stop
                $removed = $true

                break
            }
            catch
            {
                # A recursive delete stops at the first file it cannot open, so retry it file by file:
                # everything that is not held goes, and the next attempt has less left to do.
                Get-ChildItem -LiteralPath $packageCache -Recurse -Force -File -ErrorAction SilentlyContinue |
                    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue }

                Start-Sleep -Seconds 3
            }
        }

        if (-not $removed -or (Test-Path -LiteralPath $packageCache))
        {
            Write-Host "FAILED. The consumer package cache at $packageCache could not be emptied." -ForegroundColor Red
            Write-Host 'Running against it would test whatever was cached last, not the packages under test.' -ForegroundColor Red
            Write-Host 'Close anything holding a file there and re-run.' -ForegroundColor Red

            exit 1
        }
    }

    # --- The artifact set, validated from the nuspec --------------------------------------------------
    #
    # From the metadata inside each package, never from its file name. A file name is a claim: renaming
    # ResXLocalization.Core.1.0.0.nupkg to ResXLocalization.Core.1.1.0.nupkg would satisfy a name check and
    # then test a package whose nuspec still says 1.0.0. The id and the version that matter are the ones
    # NuGet reads.

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    function Get-NuspecMetadata
    {
        param([Parameter(Mandatory)] [String] $PackagePath)

        $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
        try
        {
            $entry = $archive.Entries |
                Where-Object { $_.FullName -like '*.nuspec' -and $_.FullName -notlike '*/*' } |
                Select-Object -First 1

            if ($null -eq $entry) { return $null }

            $reader = [System.IO.StreamReader]::new($entry.Open())
            try { $nuspec = [Xml] $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally
        {
            $archive.Dispose()
        }

        return [PSCustomObject] @{
            Id = $nuspec.package.metadata.id
            Version = $nuspec.package.metadata.version
        }
    }

    if (-not (Test-Path -LiteralPath $packageDirectory))
    {
        Write-Host "FAILED. $packageDirectory does not exist." -ForegroundColor Red
        Write-Host ''
        Write-Host 'This script consumes the packed packages, not the projects. Produce them first:' -ForegroundColor Red
        Write-Host '  pwsh -File scripts/verify-package-aot.ps1 -Pack' -ForegroundColor Red

        exit 1
    }

    $available = @{}
    foreach ($package in (Get-ChildItem -LiteralPath $packageDirectory -Filter '*.nupkg'))
    {
        $metadata = Get-NuspecMetadata -PackagePath $package.FullName
        if ($null -eq $metadata)
        {
            Write-Host "FAILED. $($package.Name) contains no nuspec." -ForegroundColor Red

            exit 1
        }

        $available["$($metadata.Id)/$($metadata.Version)"] = $package.Name
    }

    $missing = @($requiredPackageIds | Where-Object { -not $available.ContainsKey("$_/$PackageVersion") })

    if ($missing.Count -gt 0)
    {
        Write-Host "FAILED. $packageDirectory does not contain version $PackageVersion of:" -ForegroundColor Red
        $missing | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        Write-Host ''
        Write-Host 'Present, by the id and version in each nuspec:' -ForegroundColor Red
        if ($available.Count -eq 0)
        {
            Write-Host '  (nothing)' -ForegroundColor Red
        }
        else
        {
            $available.GetEnumerator() | Sort-Object Key | ForEach-Object {
                Write-Host "  $($_.Key)   ($($_.Value))" -ForegroundColor Red
            }
        }
        Write-Host ''
        Write-Host 'Produce them with: pwsh -File scripts/verify-package-aot.ps1 -Pack' -ForegroundColor Red

        exit 1
    }

    Write-Host "Every package this gate needs is present at $PackageVersion, by their nuspec metadata." -ForegroundColor Cyan

    Write-Host "Publishing the Avalonia package consumer ($Framework, $Runtime, packages $PackageVersion)..." `
        -ForegroundColor Cyan

    # NUGET_PACKAGES is set explicitly, and to the CONSUMER's isolated cache. The environment variable takes
    # precedence over the globalPackagesFolder setting in tests/package-consumption/nuget.config, so an
    # inherited one - CI sets NUGET_PACKAGES to a workspace-wide cache - would silently defeat the isolation
    # that config file exists to provide, and the consumer could restore a ResXLocalization assembly that
    # never came out of these packages. It is restored in the finally block at the end of the script.
    $env:NUGET_PACKAGES = $packageCache

    # PublishAot is passed here rather than set in the project file, so that an ordinary `dotnet run` of the
    # consumer stays a genuine JIT baseline.
    #
    # RestoreConfigFile names the CONSUMER's NuGet configuration explicitly rather than relying on NuGet's
    # upward search finding it. That file is what maps ResXLocalization.* to the local artifact feed and
    # <clear />s the sources first - so a missing local package fails instead of resolving from nuget.org,
    # where a published package of the same version exists and would look like a pass.
    #
    # TrimmerSingleWarn=false is what makes the gate meaningful: left at its default, ILC collapses every
    # diagnostic from one assembly into a single IL2104 "assembly produced trim warnings" line, and the
    # individual IL2xxx codes this script counts never appear.
    #
    # Run from the consumer directory, which is where a consumer would run it.
    Push-Location -LiteralPath $consumerDirectory
    try
    {
        $publishOutput = & dotnet publish 'AvaloniaConsumer.csproj' `
            --configuration $Configuration `
            --framework $Framework `
            --runtime $Runtime `
            --self-contained true `
            -p:RestoreConfigFile=$consumerNuGetConfig `
            -p:PublishAot=true `
            -p:TrimmerSingleWarn=false `
            -p:SuppressTrimAnalysisWarnings=false `
            -p:ResXLocalizationVersion=$PackageVersion `
            --output $publishDirectory 2>&1
    }
    finally
    {
        Pop-Location
    }

    $publishExitCode = $LASTEXITCODE

    $publishOutput | ForEach-Object { Write-Host $_ }

    # The log is kept whatever happens, because a failure here is the one worth reading twice and the
    # console scrolls.
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    $publishLog = Join-Path $logDirectory "publish-$Framework-$Runtime.log"
    $publishOutput | Out-File -LiteralPath $publishLog -Encoding utf8

    if ($publishExitCode -ne 0)
    {
        Write-Host ''
        Write-Host "FAILED. The Native AOT publish exited with code $publishExitCode. Log: $publishLog" -ForegroundColor Red

        exit 1
    }

    # --- The warning gate -----------------------------------------------------------------------------

    $diagnostics = $publishOutput |
        Select-String -Pattern 'IL[23]\d{3}' |
        ForEach-Object {
            # Every diagnostic line ends with the MSBuild project suffix "[...csproj::TargetFramework=...]",
            # which names this project no matter which assembly the diagnostic came from. Strip it before
            # deciding where the diagnostic originated, or everything would look like it came from the
            # consumer.
            $origin = $_.Line.Trim() -replace '\s*\[[^\[\]]*\]\s*$', ''

            [PSCustomObject] @{
                Origin = $origin
                Code = [Regex]::Match($_.Line, 'IL[23]\d{3}').Value
                Text = $_.Line.Trim()
            }
        }

    # The gate is ZERO diagnostics, from anywhere. The packages advertise Native AOT support without
    # [RequiresUnreferencedCode] or [RequiresDynamicCode] on any public member, so a consumer publishing
    # this way sees nothing - and a warning appearing anywhere is a regression against that promise.
    #
    # The split below only shapes the failure message, because "the library started warning" and "our own
    # call site started warning" have different causes. The same site is reported twice, once by the Roslyn
    # analyzer and once by ILC, so the list is de-duplicated.
    $fromConsumer = @($diagnostics | Where-Object { $_.Origin.StartsWith($consumerDirectory, [StringComparison]::OrdinalIgnoreCase) })
    $fromElsewhere = @($diagnostics | Where-Object { -not $_.Origin.StartsWith($consumerDirectory, [StringComparison]::OrdinalIgnoreCase) })

    Write-Host ''

    if ($diagnostics.Count -eq 0)
    {
        Write-Host 'IL diagnostics: none, from anywhere. A consumer publishing this way sees no warnings.' -ForegroundColor Cyan
    }
    else
    {
        Write-Host 'FAILED. The publish reported IL diagnostics, and the gate is zero:' -ForegroundColor Red

        if ($fromElsewhere.Count -gt 0)
        {
            Write-Host ''
            Write-Host "  From the library or a package ($($fromElsewhere.Count) before de-duplication):" -ForegroundColor Red
            $fromElsewhere | Sort-Object Text -Unique | ForEach-Object { Write-Host "    $($_.Text)" -ForegroundColor Red }
        }

        if ($fromConsumer.Count -gt 0)
        {
            Write-Host ''
            Write-Host "  At the consumer's own call sites ($($fromConsumer.Count) before de-duplication):" -ForegroundColor Red
            $fromConsumer | Sort-Object Origin -Unique | Group-Object Code | Sort-Object Name | ForEach-Object {
                Write-Host ("    {0,-8} {1} call site(s)" -f $_.Name, $_.Count) -ForegroundColor Red
            }
            Write-Host ''
            Write-Host '  A diagnostic here means a public API started carrying [RequiresUnreferencedCode] or' -ForegroundColor Red
            Write-Host '  [RequiresDynamicCode], which the packages promise their consumers they do not.' -ForegroundColor Red
        }

        Write-Host ''
        Write-Host "Log: $publishLog" -ForegroundColor Red
        Write-Host 'Do not silence these at the call site or with NoWarn. An IL2xxx warning is the only' -ForegroundColor Red
        Write-Host 'build-time evidence that a reflection path survives trimming - restructure the code, or' -ForegroundColor Red
        Write-Host 'answer the diagnostic where it occurs with a justified, tested suppression.' -ForegroundColor Red

        exit 1
    }

    # --- The published output -------------------------------------------------------------------------

    $executableName = $IsWindows ? 'AvaloniaConsumer.exe' : 'AvaloniaConsumer'
    $executable = Join-Path $publishDirectory $executableName
    $germanSatellite = Join-Path $publishDirectory 'de/AvaloniaConsumer.resources.dll'

    foreach ($expected in @($executable, $germanSatellite))
    {
        if (-not (Test-Path -LiteralPath $expected))
        {
            Write-Host ''
            Write-Host "FAILED. The publish did not produce $expected." -ForegroundColor Red
            Write-Host "Log: $publishLog" -ForegroundColor Red

            exit 1
        }
    }

    # --- Running the native binary --------------------------------------------------------------------
    # The file existing is not the check. Everything that can go wrong under trimming - a resource that is
    # no longer found, a satellite that is no longer loaded, a typed key whose ResourceManager was trimmed
    # away - produces a binary that exists, starts, and answers wrongly.

    Write-Host ''
    Write-Host 'Running the native binary...' -ForegroundColor Cyan
    Write-Host ''

    $runOutput = & $executable 2>&1
    $runExitCode = $LASTEXITCODE

    $runOutput | ForEach-Object { Write-Host $_ }

    $runLog = Join-Path $logDirectory "run-$Framework-$Runtime.log"
    $runOutput | Out-File -LiteralPath $runLog -Encoding utf8

    Write-Host ''

    if ($runExitCode -ne 0)
    {
        Write-Host "FAILED. The native binary exited with code $runExitCode. Log: $runLog" -ForegroundColor Red

        exit 1
    }

    Write-Host "PASSED. $Framework/$Runtime published from the packages with no IL diagnostics at all, and every assertion passed." `
        -ForegroundColor Green

    exit 0
}
finally
{
    $env:PATH = $originalPath
    $env:NUGET_PACKAGES = $originalNuGetPackages
    Pop-Location
}
