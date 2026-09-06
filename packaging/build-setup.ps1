#Requires -Version 5.1
<#
.SYNOPSIS
  Publish Host (self-contained) + Mosaicist, stage files, compile Inno Setup.

.PARAMETER Version
  Date-build tag stamped into assemblies and Setup filename (yyyy.M.d-bN), e.g. 2026.8.23-b1.
  Not semver.

.PARAMETER RepoRoot
  Repository root. Defaults to parent of this script's directory.

.PARAMETER SkipPublish
  Use existing packaging/stage contents (ISCC only).

.PARAMETER SkipInno
  Publish and stage only; do not run ISCC.
#>
param(
    [string]$Version = "0.0.0-dev",
    [string]$RepoRoot = "",
    [switch]$SkipPublish,
    [switch]$SkipInno
)

$ErrorActionPreference = "Stop"

function ConvertTo-VersionInfoVersion {
    param([Parameter(Mandatory)][string]$Tag)
    # Inno VersionInfoVersion must be n[.n[.n[.n]]]. Map 2026.8.23-b1 -> 2026.8.23.1
    if ($Tag -match '^v?(?<y>\d{4})\.(?<m>\d{1,2})\.(?<d>\d{1,2})-b(?<b>\d+)$') {
        return "{0}.{1}.{2}.{3}" -f [int]$Matches.y, [int]$Matches.m, [int]$Matches.d, [int]$Matches.b
    }
    return "0.0.0.0"
}

if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$hostProj = Join-Path $RepoRoot "host\MosaicShell.Host\MosaicShell.Host.csproj"
$mosaicistProj = Join-Path $RepoRoot "host\Mosaicist\Mosaicist.csproj"
$stage = Join-Path $PSScriptRoot "stage"
$output = Join-Path $PSScriptRoot "output"
$iss = Join-Path $PSScriptRoot "MosaicShell.iss"
$tilesSrc = Join-Path $RepoRoot "Tiles"

Write-Host "RepoRoot: $RepoRoot"
Write-Host "Version:  $Version"

if (-not $SkipPublish) {
    # Host/Mosaicist lock Core bin outputs (deps.json) while running; stop before publish.
    $lockers = Get-Process -Name "MosaicShell.Host", "Mosaicist" -ErrorAction SilentlyContinue
    if ($lockers) {
        Write-Host "Stopping locked processes: $($lockers.ProcessName -join ', ') (PID $($lockers.Id -join ', '))..." -ForegroundColor DarkYellow
        $lockers | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }

    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Join-Path $stage "Host") | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $stage "Mosaicist") | Out-Null

    Write-Host "Publishing Host (self-contained win-x64)..."
    dotnet publish $hostProj `
        -c Release -r win-x64 --self-contained true `
        -p:Version=$Version `
        -o (Join-Path $stage "Host")
    if ($LASTEXITCODE -ne 0) { throw "Host publish failed" }

    Write-Host "Publishing Mosaicist..."
    dotnet publish $mosaicistProj `
        -c Release -r win-x64 --self-contained false `
        -p:Version=$Version `
        -o (Join-Path $stage "Mosaicist")
    if ($LASTEXITCODE -ne 0) { throw "Mosaicist publish failed" }

    Write-Host "Copying Tiles..."
    Copy-Item -Recurse -Force $tilesSrc (Join-Path $stage "Tiles")
    Set-Content -Path (Join-Path $stage "VERSION.txt") -Value $Version -Encoding utf8
}

if ($SkipInno) {
    Write-Host "SkipInno set; stage ready at $stage"
    exit 0
}

$iscc = $null
# Inno Setup 7.1+ (pin to most recent at inception)
$candidates = @(
    "${env:ProgramFiles}\Inno Setup 7\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 7\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
)
foreach ($c in $candidates) {
    if (Test-Path $c) { $iscc = $c; break }
}
if (-not $iscc) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) {
    throw "ISCC.exe not found. Install Inno Setup 7.1+: winget install --id JRSoftware.InnoSetup.7 -e"
}

New-Item -ItemType Directory -Force -Path $output | Out-Null
$versionInfo = ConvertTo-VersionInfoVersion -Tag $Version
Write-Host "Compiling with $iscc (AppVersion=$Version, VersionInfoVersion=$versionInfo)..."
& $iscc "/DMyAppVersion=$Version" "/DMyVersionInfoVersion=$versionInfo" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed" }

$setup = Get-ChildItem $output -Filter "MosaicShell-Setup-*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $setup) { throw "Setup exe not produced in $output" }
Write-Host "Built $($setup.FullName)"
