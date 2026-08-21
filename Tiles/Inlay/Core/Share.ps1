function Update {
    $editingModule = $RmAPI.VariableStr('Skin.Name')
    $skinsPath = $RmAPI.VariableStr('SKINSPATH')
    $global:Import = "$skinsPath"+"$editingModule"+"\@Resources\Vars.inc"
    $global:Export = "$skinsPath"+"$editingModule"+"\@Resources\Presets\LY-Export.inc"
}

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

function StartExport {
    $Ini = Get-IniContent -filePath $global:Import
    $Ini.Remove("BoldText")
    $Ini.Remove("SemiBoldText")
    $Ini.Remove("RegularText")

    # Copy keys to an array to avoid enumerating them directly on the hashtable
    $keys = @($Ini["Variables"].Keys)
    # Remove elements not matching the expected pattern
    $ToMatch = @('ReplaceWin','DroptopIntegration','TypeToSearch', 'Scale', 'ImageUnderlayPath', 'MonitorIndex', 'PreserveTaskbarSpace', 'ModuleAllNone', 'AvatarPath', 'ProgramListDirectory', 'HotApp1', 'HotApp2', 'HotApp3', 'HotApp4', 'HotApp5', 'HotApp6')
    $keys | ForEach-Object {
        if ($ToMatch -contains $_) {
            $Ini["Variables"].Remove($_)
        }
    }
    $Ini["@Resources\Vars.inc"] = $Ini["Variables"]
    $Ini.Remove("Variables")
    # $ExportIni = $Ini | Where-Object { $_ -ne "ReplaceWin" }
    Set-IniContent -ini $Ini -filePath $global:Export

}
