# Build + run Host from repo root.
#
# Tessera OS acrylic is controlled by persisted hub settings (UseOsAcrylic in Tessera.json).
# After Win11 sign-off, --tessera-os-acrylic is ignored; this script reads/writes the hub file.
#
# Usage:
#   .\.local\compile-local.ps1
#   .\.local\compile-local.ps1 -BuildOnly
#   .\.local\compile-local.ps1 -Configuration Release
#   .\.local\compile-local.ps1 -NoKill
#   .\.local\compile-local.ps1 -TesseraOsAcrylic On     # write UseOsAcrylic=true, then run
#   .\.local\compile-local.ps1 -TesseraOsAcrylic Off    # write UseOsAcrylic=false, then run
#   .\.local\compile-local.ps1 -ForceSoftwareRender     # H2 frost fallback eval row

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [switch] $BuildOnly,
    [switch] $NoKill,

    # Optional: write Tessera UseOsAcrylic to hub settings before launch (On/Off).
    [ValidateSet("On", "Off")]
    [string] $TesseraOsAcrylic,

    # H2 eval: force Software-only rendering (OsAcrylic ineligible; frost fallback).
    [switch] $ForceSoftwareRender
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$hostProj = Join-Path $repoRoot "host\MosaicShell.Host\MosaicShell.Host.csproj"
# version pinned to match the target framework of the Host project
$tfm = "net10.0-windows10.0.19041.0"
$tesseraSettingsPath = Join-Path $env:LOCALAPPDATA "MosaicShell\Config\modules\Tessera.json"

function Get-TesseraOsAcrylicEnabled {
    if (-not (Test-Path $tesseraSettingsPath)) {
        return $false
    }
    try {
        $json = Get-Content -LiteralPath $tesseraSettingsPath -Raw | ConvertFrom-Json
        return [bool]$json.UseOsAcrylic
    }
    catch {
        return $false
    }
}

function Set-TesseraOsAcrylicEnabled([bool] $enabled) {
    $dir = Split-Path -Parent $tesseraSettingsPath
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    if (Test-Path -LiteralPath $tesseraSettingsPath) {
        $settings = Get-Content -LiteralPath $tesseraSettingsPath -Raw | ConvertFrom-Json
    }
    else {
        $settings = [PSCustomObject]@{
            Style                 = "Fluent"
            Position              = "TL"
            MonitorIndex          = 1
            XPad                  = 20
            YPad                  = 20
            AutoDismissMs         = 2000
            Ani                   = 2
            AniDir                = "Left"
            UseLegacyVolumeHooks  = $true
            LegacyVolumeStep      = 0.02
            EnableMediaFlyouts    = $true
            EnableLockFlyouts     = $true
            EnableFlightFlyouts   = $true
            ShowMediaStripOnVolume = $true
            UseAcrylicBackdrop    = $true
            UseFocusDim           = $true
            FlyoutScalePercent    = 100
            UseBackdropBlur       = $true
            AccentColor           = ""
        }
    }

    $settings | Add-Member -NotePropertyName UseOsAcrylic -NotePropertyValue $enabled -Force
    ($settings | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $tesseraSettingsPath -Encoding utf8
}

if (-not (Test-Path $hostProj)) {
    Write-Host "error: Host project not found: $hostProj" -ForegroundColor Red
    exit 1
}

if ($PSBoundParameters.ContainsKey("TesseraOsAcrylic")) {
    $writeOn = $TesseraOsAcrylic -eq "On"
    Set-TesseraOsAcrylicEnabled -enabled $writeOn
    Write-Host "Tessera hub: UseOsAcrylic=$writeOn (written to Tessera.json)" -ForegroundColor DarkGray
}

$osAcrylicOn = Get-TesseraOsAcrylicEnabled

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
    if ($osAcrylicOn) {
        Write-Host "  Tessera OS acrylic: ON (hub UseOsAcrylic)" -ForegroundColor DarkGray
    }
    else {
        Write-Host "  Tessera OS acrylic: OFF (Skia frost; hub or -TesseraOsAcrylic On)" -ForegroundColor DarkGray
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
