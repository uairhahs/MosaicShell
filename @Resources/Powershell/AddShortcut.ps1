$DesktopPath = [Environment]::GetFolderPath("Desktop")
$Startpath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs"

# ---------------------------------------------------------------------------- #
#                                   Functions                                  #
# ---------------------------------------------------------------------------- #

function Get-IniContent ($filePath) {
    $ini = [ordered]@{}
    if (![System.IO.File]::Exists($filePath)) {
        throw "$filePath invalid"
    }
    # $section = ';ItIsNotAFuckingSection;'
    # $ini.Add($section, [ordered]@{})

    foreach ($line in [System.IO.File]::ReadLines($filePath)) {
        if ($line -match "^\s*\[(.+?)\]\s*$") {
            $section = $matches[1]
            $secDup = 1
            while ($ini.Keys -contains $section) {
                $section = $section + '||ps' + $secDup
            }
            $ini.Add($section, [ordered]@{})
        }
        elseif ($line -match "^\s*;.*$") {
            $notSectionCount = 0
            while ($ini[$section].Keys -contains ';NotSection' + $notSectionCount) {
                $notSectionCount++
            }
            $ini[$section][';NotSection' + $notSectionCount] = $matches[1]
        }
        elseif ($line -match "^\s*(.+?)\s*=\s*(.+?)$") {
            $key, $value = $matches[1..2]
            $ini[$section][$key] = $value
        }
        else {
            $notSectionCount = 0
            while ($ini[$section].Keys -contains ';NotSection' + $notSectionCount) {
                $notSectionCount++
            }
            $ini[$section][';NotSection' + $notSectionCount] = $line
        }
    }

    return $ini
}

function Set-IniContent($ini, $filePath) {
    $str = @()

    foreach ($section in $ini.GetEnumerator()) {
        if ($section -ne ';ItIsNotAFuckingSection;') {
            $str += "[" + ($section.Key -replace '\|\|ps\d+$', '') + "]"
        }
        foreach ($keyvaluepair in $section.Value.GetEnumerator()) {
            if ($keyvaluepair.Key -match "^;NotSection\d+$") {
                $str += $keyvaluepair.Value
            }
            else {
                $str += $keyvaluepair.Key + "=" + $keyvaluepair.Value
            }
        }
    }

    $finalStr = $str -join [System.Environment]::NewLine

    $finalStr | Out-File -filePath $filePath -Force -Encoding unicode
}

# ---------------------------------------------------------------------------- #
#                                    Actions                                   #
# ---------------------------------------------------------------------------- #

function Check {
    If (Test-Path -Path "$Startpath\MosaicShell.lnk") {
        $RmAPI.Bang('[!SetOption Section4.Button2.Shape MeterStyle "SectionButton:S | Section4.ButtonProg.True"]')
        $RmAPI.Bang('[!UpdateMeter Section4.Button2.Shape]')
        $RmAPI.Bang('[!Redraw]')
        $RmAPI.Log("Found: CoreHome in programs")
    }
    else {
        $RmAPI.Log("Failed to find corehome in programs")
    }
    If (Test-Path -Path "$DesktopPath\MosaicShell.lnk") {
        $RmAPI.Bang('[!SetOption Section4.Button1.Shape MeterStyle "SectionButton:S | Section4.ButtonDesk.True"]')
        $RmAPI.Bang('[!UpdateMeter Section4.Button1.Shape]')
        $RmAPI.Bang('[!Redraw]')
        $RmAPI.Log("Found: CoreHome on desktop")
    }
    else {
        $RmAPI.Log("Failed to find corehome on desktop")
    }
    $Ini = Get-IniContent "$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini"
    If ($Ini['Rainmeter']['HardwareAcceleration'] -contains '1') {
        $RmAPI.Bang('[!SetVariable HardwareAcceleration 1]')
        $RmAPI.Bang('[!UpdateMeter HardwareAcceleration]')
        $RmAPI.Bang('[!Redraw]')
    }
}

Check

function ToggleHA {
    If ($RmAPI.VariableStr('HardwareAcceleration') -contains '1') {
        $RmAPI.Bang("[!WriteKeyValue Rainmeter HardwareAcceleration 0 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"]")
    } else {
        $RmAPI.Bang("[!WriteKeyValue Rainmeter HardwareAcceleration 1 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"]")
    }
    $RmAPI.Bang("[!WriteKeyValue `"$($RmAPI.VariableStr('CURRENTCONFIG'))`" Active 1 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"][`"$($RmAPI.VariableStr('@'))Addons\RestartRainmeter.exe`"]")
}

function Set-DPICompatability {
    REG ADD "HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" /V "$($RmAPI.VariableStr('PROGRAMPATH'))Rainmeter.exe" /T REG_SZ /D '~HIGHDPIAWARE' /F
    $RmAPI.Bang("[!WriteKeyValue `"$($RmAPI.VariableStr('CURRENTCONFIG'))`" Active 1 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"][`"$($RmAPI.VariableStr('@'))Addons\RestartRainmeter.exe`"]")
}

function Reset-DPICompatability {
    REG ADD "HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" /V "$($RmAPI.VariableStr('PROGRAMPATH'))Rainmeter.exe" /T REG_SZ /D '' /F
    $RmAPI.Bang("[!WriteKeyValue `"$($RmAPI.VariableStr('CURRENTCONFIG'))`" Active 1 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"][`"$($RmAPI.VariableStr('@'))Addons\RestartRainmeter.exe`"]")
}

function AutoStartup {
    $RainmeterExe = $RmAPI.VariableStr('PROGRAMPATH')
    $ResourceFolder = $RmAPI.VariableStr('@')
    If (!(Test-Path "$Startpath\Startup\Rainmeter.lnk")) {
        & "$($ResourceFolder)Actions\AHKv1.exe" "$($ResourceFolder)Addons\CoreAHKFunctions.ahk" "Shortcut_NoParam" "$Startpath\Startup\Rainmeter.lnk" "$($RainmeterExe)Rainmeter.exe"
    }
}

function Desktop {
    $RainmeterExe = $RmAPI.VariableStr('PROGRAMPATH')
    $ResourceFolder = $RmAPI.VariableStr('@')
    & "$($ResourceFolder)Actions\AHKv1.exe" "$($ResourceFolder)Addons\CoreAHKFunctions.ahk" "Shortcut_Regular" "$DesktopPath\MosaicShell.lnk" "$($RainmeterExe)Rainmeter.exe" '[!DeactivateConfig #MosaicShell\Main][!ActivateConfig #MosaicShell\Main Home.ini]' "$($ResourceFolder)Images\Logo.ico"
}

function StartFolder {
    $RainmeterExe = $RmAPI.VariableStr('PROGRAMPATH')
    $ResourceFolder = $RmAPI.VariableStr('@')
    & "$($ResourceFolder)Actions\AHKv1.exe" "$($ResourceFolder)Addons\CoreAHKFunctions.ahk" "Shortcut_Regular" "$Startpath\MosaicShell.lnk" "$($RainmeterExe)Rainmeter.exe" '[!DeactivateConfig #MosaicShell\Main][!ActivateConfig #MosaicShell\Main Home.ini]' "$($ResourceFolder)Images\Logo.ico"
}

function RemoveDeskop {
    Remove-Item "$DesktopPath\MosaicShell.lnk" -Recurse
}

function RemoveStartFolder {
    Remove-Item "$Startpath\Microsoft\Windows\Start Menu\Programs\MosaicShell.lnk" -Recurse
}

