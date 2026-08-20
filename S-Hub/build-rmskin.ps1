<#
.SYNOPSIS
    Builds MosaicShell.rmskin locally.

.DESCRIPTION
    Packages the skin into a .rmskin file identical to what the GitHub Actions
    release workflow produces. Run from the repo root on Windows.

.PARAMETER Version
    Version string to embed in RMSKIN.ini (e.g. 6.0.0). Defaults to the
    current value already in RMSKIN.ini.

.PARAMETER Out
    Output directory. Defaults to the repo root.

.EXAMPLE
    .\S-Hub\build-rmskin.ps1
    .\S-Hub\build-rmskin.ps1 -Version 6.1.0 -Out C:\Releases
#>

param(
    [string]$Version,
    [string]$Out = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path

# --------------------------------- Version ---------------------------------- #
$rmskinPath = "$repo\RMSKIN.ini"
if ($Version) {
    (Get-Content $rmskinPath) -replace '^Version=.*', "Version=$Version" |
        Set-Content $rmskinPath -Encoding UTF8
    Write-Host "Version set to $Version"
}

# ---------------------------------- Stage ----------------------------------- #
$stage = "$env:TEMP\MosaicShell-rmskin-stage"
$skin  = "$stage\Skins\#MosaicShell"

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item $skin -ItemType Directory -Force > $null

foreach ($dir in @('@Resources','Accessories','Core','CoreShell','Ctx','Main','S-Hub')) {
    if (Test-Path "$repo\$dir") {
        Copy-Item "$repo\$dir" "$skin\" -Recurse -Force
    }
}
Copy-Item $rmskinPath "$stage\" -Force

# ---------------------------------- Pack ------------------------------------ #
$zip  = "$Out\MosaicShell.zip"
$dest = "$Out\MosaicShell.rmskin"

if (Test-Path $zip)  { Remove-Item $zip  -Force }
if (Test-Path $dest) { Remove-Item $dest -Force }

Compress-Archive -Path "$stage\*" -DestinationPath $zip -CompressionLevel Fastest
Rename-Item $zip $dest

Remove-Item $stage -Recurse -Force

Write-Host "Built: $dest" -ForegroundColor Green
