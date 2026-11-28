# Build + run Host from repo root.
#
# Tessera Native Translucency is controlled by persisted hub settings (UseNativeTranslucency in Tessera.json).
# After Win11 sign-off, --tessera-native-translucency is ignored; this script reads/writes the hub file.
#
# Usage:
#   .\compile-local.ps1
#   .\compile-local.ps1 -BuildOnly
#   .\compile-local.ps1 -Configuration Release
#   .\compile-local.ps1 -NoKill
#   .\compile-local.ps1 -TesseraNativeTranslucency On     # write UseNativeTranslucency=true, then run
#   .\compile-local.ps1 -TesseraNativeTranslucency Off    # write UseNativeTranslucency=false, then run
#   .\compile-local.ps1 -ForceSoftwareRender     # H2 frost fallback eval row

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [switch] $BuildOnly,
    [switch] $NoKill,

    # Optional: write Tessera UseNativeTranslucency to hub settings before launch (On/Off).
    [ValidateSet("On", "Off")]
    [string] $TesseraNativeTranslucency,

    # H2 eval: force Software-only rendering (Native Translucency ineligible; frost fallback).
    [switch] $ForceSoftwareRender
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot ".")
$hostProj = Join-Path $repoRoot "host\MosaicShell.Host\MosaicShell.Host.csproj"
# version pinned to match the target framework of the Host project
$tfm = "net11.0-windows10.0.19041.0"
$tesseraSettingsPath = Join-Path $env:LOCALAPPDATA "MosaicShell\Config\modules\Tessera.json"

function Get-TesseraNativeTranslucencyEnabled {
    if (-not (Test-Path $tesseraSettingsPath)) {
        return $false
    }
    try {
        $json = Get-Content -LiteralPath $tesseraSettingsPath -Raw | ConvertFrom-Json
        # Files saved before the rename carry only UseOsAcrylic.
        if ($null -ne $json.UseNativeTranslucency) { return [bool]$json.UseNativeTranslucency }
        return [bool]$json.UseOsAcrylic
    }
    catch {
        return $false
    }
}

function Set-TesseraNativeTranslucencyEnabled([bool] $enabled) {
    $dir = Split-Path -Parent $tesseraSettingsPath
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    if (Test-Path -LiteralPath $tesseraSettingsPath) {
        $settings = Get-Content -LiteralPath $tesseraSettingsPath -Raw | ConvertFrom-Json
    }
    else {
        $settings = [PSCustomObject]@{
            Style                  = "Fluent"
            Position               = "TL"
            MonitorIndex           = 1
            XPad                   = 20
            YPad                   = 20
            AutoDismissMs          = 2000
            Ani                    = 2
            AniDir                 = "Left"
            UseLegacyVolumeHooks   = $true
            LegacyVolumeStep       = 0.02
            EnableMediaFlyouts     = $true
            EnableLockFlyouts      = $true
            EnableFlightFlyouts    = $true
            ShowMediaStripOnVolume = $true
            UseAcrylicBackdrop     = $true
            UseFocusDim            = $true
            FlyoutScalePercent     = 100
            UseBackdropBlur        = $true
            AccentColor            = ""
        }
    }

    $settings | Add-Member -NotePropertyName UseNativeTranslucency -NotePropertyValue $enabled -Force
    # Drop the pre-rename key so the file cannot hold two values that disagree.
    $settings.PSObject.Properties.Remove("UseOsAcrylic")
    ($settings | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $tesseraSettingsPath -Encoding utf8
}

if (-not (Test-Path $hostProj)) {
    Write-Host "error: Host project not found: $hostProj" -ForegroundColor Red
    exit 1
}

if ($PSBoundParameters.ContainsKey("TesseraNativeTranslucency")) {
    $writeOn = $TesseraNativeTranslucency -eq "On"
    Set-TesseraNativeTranslucencyEnabled -enabled $writeOn
    Write-Host "Tessera hub: UseNativeTranslucency=$writeOn (written to Tessera.json)" -ForegroundColor DarkGray
}

$nativeTranslucencyOn = Get-TesseraNativeTranslucencyEnabled

Push-Location $repoRoot
try {
    if (-not $NoKill) {
        $running = Get-Process -Name "MosaicShell.Host" -ErrorAction SilentlyContinue
        if ($running) {
            Write-Host "Stopping running MosaicShell.Host (PID $($running.Id -join ', '))…" -ForegroundColor DarkYellow
            $running | Stop-Process -Force
            Start-Sleep -Milliseconds 400
        }
    }

    Write-Host "Building Host ($Configuration)…" -ForegroundColor Cyan
    & dotnet build $hostProj -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "error: build failed ($LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Build OK." -ForegroundColor Green
    if ($BuildOnly) { exit 0 }

    Write-Host "Starting Host…" -ForegroundColor Cyan
    if ($nativeTranslucencyOn) {
        Write-Host "  Tessera Native Translucency: ON (hub UseNativeTranslucency)" -ForegroundColor DarkGray
    }
    else {
        Write-Host "  Tessera Native Translucency: OFF (Skia frost; hub or -TesseraNativeTranslucency On)" -ForegroundColor DarkGray
    }
    if ($ForceSoftwareRender) {
        Write-Host "  eval: --tessera-software-render (H2 Software fallback row)" -ForegroundColor DarkGray
    }

    # Explicit --no-build: we already built; avoids a second restore/compile race.
    $runArgs = @(
        'run', '--project', $hostProj,
        '-c', $Configuration,
        '-f', $tfm,
        '--no-build',
        '--no-launch-profile'
    )
    $hostFlags = @()
    if ($ForceSoftwareRender) {
        $hostFlags += '--tessera-software-render'
    }
    if ($hostFlags.Count -gt 0) {
        $runArgs += '--'
        $runArgs += $hostFlags
    }
    & dotnet @runArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
