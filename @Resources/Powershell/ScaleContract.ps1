# MosaicShell scale contract - DPI probe + writers
# UiScale = DpiScale * UserScale

function Get-DpiScale {
    # Prefer user32 GetDpiForSystem (Win10+); fall back to GDI physical/logical ratio.
    try {
        if (-not ('MosaicShellDpiSystem' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class MosaicShellDpiSystem {
  [DllImport("user32.dll")]
  public static extern uint GetDpiForSystem();
}
'@
        }
        $dpi = [MosaicShellDpiSystem]::GetDpiForSystem()
        if ($dpi -gt 0) {
            return [Math]::Round($dpi / 96.0, 4)
        }
    } catch {
        # continue to GDI fallback
    }

    try {
        if (-not ('MosaicShellDPI' -as [type])) {
            Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Drawing;

public class MosaicShellDPI {
  [DllImport("gdi32.dll")]
  static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

  public enum DeviceCap {
    VERTRES = 10,
    DESKTOPVERTRES = 117
  }

  public static float Scaling() {
    Graphics g = Graphics.FromHwnd(IntPtr.Zero);
    IntPtr desktop = g.GetHdc();
    try {
      int logical = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
      int physical = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);
      if (logical <= 0) { return 1f; }
      return (float)physical / (float)logical;
    } finally {
      g.ReleaseHdc(desktop);
      g.Dispose();
    }
  }
}
'@ -ReferencedAssemblies 'System.Drawing.dll'
        }
        $scale = [Math]::Round([MosaicShellDPI]::Scaling(), 4)
        if ($scale -gt 0) { return $scale }
    } catch {
        # last resort
    }

    return 1
}

function Get-ScaleVarsPath {
    if ($RmAPI) {
        return (Join-Path $RmAPI.VariableStr('@') 'ScaleVars.inc')
    }
    throw 'ScaleVars path requires RmAPI or explicit path'
}

function Read-UserScale([string]$ScaleVarsPath) {
    $userScale = 1
    if (Test-Path -LiteralPath $ScaleVarsPath) {
        $raw = [System.IO.File]::ReadAllText($ScaleVarsPath)
        $m = [regex]::Match($raw, '(?m)^\s*UserScale\s*=\s*(.+?)\s*$')
        if ($m.Success) {
            $parsed = 0.0
            if ([double]::TryParse($m.Groups[1].Value, [ref]$parsed) -and $parsed -gt 0) {
                $userScale = $parsed
            }
        }
    }
    return $userScale
}

function Read-LastUiScale([string]$ScaleVarsPath) {
    # Missing LastUiScale means persisted Set.W/H are still in design space (UiScale=1).
    $last = 1.0
    if (Test-Path -LiteralPath $ScaleVarsPath) {
        $raw = [System.IO.File]::ReadAllText($ScaleVarsPath)
        $m = [regex]::Match($raw, '(?m)^\s*LastUiScale\s*=\s*(.+?)\s*$')
        if ($m.Success) {
            $parsed = 0.0
            if ([double]::TryParse($m.Groups[1].Value, [ref]$parsed) -and $parsed -gt 0) {
                $last = $parsed
            }
        }
    }
    return $last
}

function Write-ScaleVars {
    param(
        [string]$Path,
        [double]$DpiScale,
        [double]$UserScale,
        [double]$LastUiScale
    )

    $dir = Split-Path -Parent $Path
    if (!(Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $content = @"
[Variables]
; Host-agnostic scale contract persistence.
; UiScale = DpiScale * UserScale (see Includes\ScaleContract.inc)
; LastUiScale tracks which UiScale Set.W/Set.H were last sized for.
DpiScale=$DpiScale
UserScale=$UserScale
LastUiScale=$LastUiScale
"@
    $content | Out-File -FilePath $Path -Encoding unicode -Force
}

function Get-UiScale([double]$DpiScale, [double]$UserScale) {
    return [Math]::Round($DpiScale * $UserScale, 4)
}

function Read-NumericVar([string]$Path, [string]$Name) {
    if (!(Test-Path -LiteralPath $Path)) { return $null }
    $raw = [System.IO.File]::ReadAllText($Path)
    $m = [regex]::Match($raw, "(?m)^\s*$([regex]::Escape($Name))\s*=\s*([0-9]+(?:\.[0-9]+)?)\s*$")
    if ($m.Success) {
        $parsed = 0.0
        if ([double]::TryParse($m.Groups[1].Value, [ref]$parsed)) { return $parsed }
    }
    return $null
}

function Write-NumericVars([string]$Path, [hashtable]$Values) {
    if (!(Test-Path -LiteralPath $Path)) { return }
    if ($Values.Count -eq 0) { return }
    $raw = [System.IO.File]::ReadAllText($Path)
    foreach ($key in $Values.Keys) {
        $replacement = [string]([Math]::Round([double]$Values[$key]))
        if ($raw -match "(?m)^\s*$([regex]::Escape($key))\s*=") {
            $raw = [regex]::Replace($raw, "(?m)^(\s*$([regex]::Escape($key))\s*=\s*).*$", "`${1}$replacement", 1)
        }
    }
    [System.IO.File]::WriteAllText($Path, $raw)
}

function Sync-CoreWindowSize {
    param(
        [string]$CoreVarsPath,
        [double]$PreviousUiScale,
        [double]$NewUiScale
    )

    if ($PreviousUiScale -le 0 -or $NewUiScale -le 0) { return $null }
    if ([Math]::Abs($NewUiScale - $PreviousUiScale) -lt 0.0001) { return $null }

    $w = Read-NumericVar $CoreVarsPath 'Set.W'
    $h = Read-NumericVar $CoreVarsPath 'Set.H'
    if ($null -eq $w -or $null -eq $h) { return $null }

    $ratio = $NewUiScale / $PreviousUiScale
    $newW = [Math]::Round($w * $ratio)
    $newH = [Math]::Round($h * $ratio)

    # Keep within unscaled max bounds grown by NewUiScale (Window.inc maxes are design-ish).
    $maxW = [Math]::Round(1920 * $NewUiScale)
    $maxH = [Math]::Round(1080 * $NewUiScale)
    $minW = [Math]::Round(500 * $NewUiScale)
    $minH = [Math]::Round(375 * $NewUiScale)
    if ($newW -lt $minW) { $newW = $minW }
    if ($newH -lt $minH) { $newH = $minH }
    if ($newW -gt $maxW) { $newW = $maxW }
    if ($newH -gt $maxH) { $newH = $maxH }

    Write-NumericVars $CoreVarsPath @{ 'Set.W' = $newW; 'Set.H' = $newH }

    return @{ W = $newW; H = $newH }
}

function Sync-TileScaleAliases {
    param(
        [string]$SkinsPath,
        [string]$CoreFolder,
        [double]$UiScale
    )

    # Keep a live formula so DpiScale/UserScale edits stay coherent without re-sync.
    $scaleFormula = '(#DpiScale#*#UserScale#)'

    $skinNames = @(
        'Tessera', 'Mixdeck', 'Inlay', 'Slate', 'Chord', 'Substrate',
        'Chrono', 'Phono', 'Pulse', 'Canvas'
    )

    foreach ($name in $skinNames) {
        $candidates = @(
            (Join-Path $SkinsPath "$name\@Resources\Vars.inc"),
            (Join-Path $SkinsPath "Tiles\$name\@Resources\Vars.inc")
        )
        foreach ($varsFile in $candidates) {
            if (!(Test-Path -LiteralPath $varsFile)) { continue }
            $raw = [System.IO.File]::ReadAllText($varsFile)
            if ($raw -match '(?m)^\s*Scale\s*=') {
                $raw = [regex]::Replace($raw, '(?m)^(\s*Scale\s*=\s*).*$', "`${1}$scaleFormula")
                [System.IO.File]::WriteAllText($varsFile, $raw)
            }
            $mainVarsDir = Join-Path (Split-Path (Split-Path $varsFile)) 'Main\Vars'
            if (Test-Path -LiteralPath $mainVarsDir) {
                Get-ChildItem -LiteralPath $mainVarsDir -File -ErrorAction SilentlyContinue | ForEach-Object {
                    $v = [System.IO.File]::ReadAllText($_.FullName)
                    if ($v -match '(?m)^\s*Scale\s*=') {
                        $v = [regex]::Replace($v, '(?m)^(\s*Scale\s*=\s*).*$', "`${1}$scaleFormula")
                        [System.IO.File]::WriteAllText($_.FullName, $v)
                    }
                }
            }
        }
    }

    # Chord launch Style.Scale aliases
    $chordStyleDirs = @(
        (Join-Path $SkinsPath 'Chord\Launch\Vars'),
        (Join-Path $SkinsPath 'Tiles\Chord\Launch\Vars')
    )
    foreach ($styleDir in $chordStyleDirs) {
        if (!(Test-Path -LiteralPath $styleDir)) { continue }
        Get-ChildItem -LiteralPath $styleDir -Filter '*.inc' -File | ForEach-Object {
            $v = [System.IO.File]::ReadAllText($_.FullName)
            if ($v -match '(?m)^\s*Style\.Scale\s*=') {
                $v = [regex]::Replace($v, '(?m)^(\s*Style\.Scale\s*=\s*).*$', "`${1}$scaleFormula")
                [System.IO.File]::WriteAllText($_.FullName, $v)
            }
        }
    }
}

function Update-SetSAlias {
    param(
        [string]$CoreVarsPath,
        [double]$DpiScale,
        [double]$UserScale
    )
    if (!(Test-Path -LiteralPath $CoreVarsPath)) { return }
    $raw = [System.IO.File]::ReadAllText($CoreVarsPath)
    # Keep Set.S as live formula so runtime UserScale edits recompute
    $formula = "(#DpiScale#*#UserScale#)"
    if ($raw -match '(?ms)\[Set\.S\].*?Formula\s*=') {
        $raw = [regex]::Replace($raw, '(?m)^(\s*Formula\s*=\s*).*$', "`${1}$formula", 1)
        [System.IO.File]::WriteAllText($CoreVarsPath, $raw)
    }
}

function Apply-ScaleContract {
    param(
        [switch]$ResetUserScale,
        [Nullable[double]]$UserScaleOverride,
        [switch]$SkipRefresh
    )

    $resourcePath = $RmAPI.VariableStr('@')
    $skinsPath = $RmAPI.VariableStr('SKINSPATH')
    $scaleVarsPath = Join-Path $resourcePath 'ScaleVars.inc'
    $coreVarsPath = Join-Path $resourcePath 'Vars.inc'

    $dpi = Get-DpiScale
    $user = Read-UserScale $scaleVarsPath
    if ($ResetUserScale) { $user = 1 }
    if ($null -ne $UserScaleOverride -and $UserScaleOverride -gt 0) {
        $user = [Math]::Round([double]$UserScaleOverride, 4)
    }

    $prevUi = Read-LastUiScale $scaleVarsPath
    $ui = Get-UiScale $dpi $user
    $resized = Sync-CoreWindowSize -CoreVarsPath $coreVarsPath -PreviousUiScale $prevUi -NewUiScale $ui

    Write-ScaleVars -Path $scaleVarsPath -DpiScale $dpi -UserScale $user -LastUiScale $ui
    Update-SetSAlias -CoreVarsPath $coreVarsPath -DpiScale $dpi -UserScale $user
    Sync-TileScaleAliases -SkinsPath $skinsPath -CoreFolder 'MosaicShell' -UiScale $ui

    $RmAPI.Bang("[!SetVariable DpiScale $dpi]")
    $RmAPI.Bang("[!SetVariable UserScale $user]")
    $RmAPI.Bang("[!SetVariable UiScale $ui]")
    $RmAPI.Bang('[!SetOption Set.S Formula "(#DpiScale#*#UserScale#)"][!UpdateMeasure Set.S]')
    if ($null -ne $resized) {
        $RmAPI.Bang("[!SetVariable Set.W $($resized.W)][!SetVariable Set.H $($resized.H)]")
    }

    if (-not $SkipRefresh) {
        $RmAPI.Bang('[!Refresh "#MosaicShell\Main"]')
    }

    $sizeNote = if ($null -ne $resized) { " Set.W=$($resized.W) Set.H=$($resized.H)" } else { '' }
    $RmAPI.Log("ScaleContract: DpiScale=$dpi UserScale=$user UiScale=$ui (from $prevUi)$sizeNote")
}

function Ensure-HighDpiAware {
    $rmExe = Join-Path $RmAPI.VariableStr('PROGRAMPATH') 'Rainmeter.exe'
    REG ADD "HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" /V "$rmExe" /T REG_SZ /D '~HIGHDPIAWARE' /F | Out-Null
}

function Repair-HighDpiAware {
    Ensure-HighDpiAware
    $RmAPI.Bang("[!WriteKeyValue `"$($RmAPI.VariableStr('CURRENTCONFIG'))`" Active 1 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"][`"$($RmAPI.VariableStr('@'))Addons\RestartRainmeter.exe`"]")
}

function Reset-UserScale {
    Apply-ScaleContract -ResetUserScale
}

function Redetect-Dpi {
    Apply-ScaleContract
}

# Rainmeter entry: probe on load without forcing full refresh loop
function Initialize-ScaleContract {
    Ensure-HighDpiAware
    Apply-ScaleContract -SkipRefresh
}
