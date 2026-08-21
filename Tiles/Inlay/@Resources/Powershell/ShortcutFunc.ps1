$file = "$($RmAPI.VariableStr('SKINSPATH'))..\CoreData\Inlay\Shortcuts.inc"
$imageDirectory = "$($RmAPI.VariableStr('SKINSPATH'))..\CoreData\Inlay\IconCache\"
$mainConfig = "Inlay\Main\Accessories\ShortcutsEditor"

function debug {
    param(
        [Parameter()]
        [string]
        $message
    )

    $RmAPI.Bang("[!Log `"`"`"Inlay: " + $message + "`"`"`" Debug]")
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

function ChangeTargetTo($index) {
    $RmAPI.Bang("[!Delay 200][!CommandMeasure s.ShortcutFunc `"ChangeTargetTo($index)`" $mainConfig]")
}

function ShortcutChangeTo {
    param (
    [int]$index,
    $selectedFile,
    $selectedFileName,
    $returnedImageName
    )
    debug "Changing $index shortcut to $selectedFile which is $selectedFileName with image $returnedImageName"

    $Ini = Get-IniContent $file
    # ---------------------------- change cache values --------------------------- #
    $RmAPI.Bang("[!SetOption Shortcut$index.Image ImageName `"`"`"$imageDirectory$returnedImageName`"`"`" $mainConfig]")
    $RmAPI.Bang("[!SetOption Shortcut$index.String Text `"`"`"$selectedFileName`"`"`" $mainConfig]")
    $RmAPI.Bang("[!UpdateMeter * $mainConfig][!HideMeterGroup Overlay $mainConfig][!Redraw $mainConfig]")
    # ------------------------------- write values ------------------------------- #
    $Ini["Shortcut$index.Shape"]['LeftMouseUpAction'] = "[`"$selectedFile`"]"
    $Ini["Shortcut$index.Image"]['ImageName'] = "`"$imageDirectory$returnedImageName.png`""
    $Ini["Shortcut$index.String"]['Text'] = $selectedFileName
    Set-IniContent $Ini $file
}

function ShortcutImageChangeTo {
    param (
    [int]$index,
    $selectedFile
    )
    debug "Changing $index image to $selectedFile"

    $Ini = Get-IniContent $file
    # ---------------------------- change cache values --------------------------- #
    $RmAPI.Bang("[!SetOption Shortcut$index.Image ImageName `"`"`"$selectedFile`"`"`" $mainConfig]")
    $RmAPI.Bang("[!UpdateMeter * $mainConfig][!HideMeterGroup Overlay $mainConfig][!Redraw $mainConfig]")
    # ------------------------------- write values ------------------------------- #
    $Ini["Shortcut$index.Image"]['ImageName'] = "`"$selectedFile`""
    Set-IniContent $Ini $file
}

function ShortcutNameChangeTo {
    param (
    [int]$index,
    $name
    )
    debug "Changing $index name to $name"

    $Ini = Get-IniContent $file
    # ---------------------------- change cache values --------------------------- #
    $RmAPI.Bang("[!SetOption Shortcut$index.String Text `"`"`"$name`"`"`" $mainConfig]")
    $RmAPI.Bang("[!UpdateMeter * $mainConfig][!HideMeterGroup Overlay $mainConfig][!Redraw $mainConfig]")
    # ------------------------------- write values ------------------------------- #
    $Ini["Shortcut$index.String"]['Text'] = $name
    Set-IniContent $Ini $file
}

function ShortcutSwapBetween {
    param (
        [int]$i1,
        [int]$i2
    )
    debug "$i1 $i2"

    $Ini = Get-IniContent $file
    # --------------------------- swap values in cache --------------------------- #
    $RmAPI.Bang("[!SetOption Shortcut$i1.Image ImageName $($Ini["Shortcut$i2.Image"]['ImageName']) $mainConfig]")
    $RmAPI.Bang("[!SetOption Shortcut$i2.Image ImageName $($Ini["Shortcut$i1.Image"]['ImageName']) $mainConfig]")
    $RmAPI.Bang("[!SetOption Shortcut$i1.String Text `"`"`"$($Ini["Shortcut$i2.String"]['Text'])`"`"`" $mainConfig]")
    $RmAPI.Bang("[!SetOption Shortcut$i2.String Text `"`"`"$($Ini["Shortcut$i1.String"]['Text'])`"`"`" $mainConfig]")
    $RmAPI.Bang("[!UpdateMeter * $mainConfig][!Redraw $mainConfig]")
    # --------------------------- swap and write values -------------------------- #
    $Ini["Shortcut$i2.Shape"]['LeftMouseUpAction'], $Ini["Shortcut$i1.Shape"]['LeftMouseUpAction'] = $Ini["Shortcut$i1.Shape"]['LeftMouseUpAction'], $Ini["Shortcut$i2.Shape"]['LeftMouseUpAction']
    $Ini["Shortcut$i2.Image"]['ImageName'], $Ini["Shortcut$i1.Image"]['ImageName'] = $Ini["Shortcut$i1.Image"]['ImageName'], $Ini["Shortcut$i2.Image"]['ImageName']
    $Ini["Shortcut$i2.String"]['Text'], $Ini["Shortcut$i1.String"]['Text'] = $Ini["Shortcut$i1.String"]['Text'], $Ini["Shortcut$i2.String"]['Text']
    Set-IniContent $Ini $file
}

function ShortcutNew {
    param (
    [int]$index,
    $selectedFile,
    $selectedFileName,
    $returnedImageName,
    [int]$ti,
    [int]$ri
    )
    debug "Adding $index shortcut to $selectedFile which is $selectedFileName with image $returnedImageName"

    $r = $ti % $ri

    If ($r -eq 0) {
        $shapestyle = 'Shortcut.Shape:S | Shortcut.Shape.NewLine:S'
    } else {
        $shapestyle = 'Shortcut.Shape:S'
    }

    Add-Content $file @"

[Shortcut$index.Shape]
Meter=Shape
MeterStyle=$shapestyle
LeftMouseUpAction=[`"$selectedFile`"]
[Shortcut$index.Image]
Meter=Image
MeterStyle=Shortcut.Image:S
ImageName=`"$imageDirectory$returnedImageName.png`"
[Shortcut$index.String]
Meter=String
MeterStyle=RegularText | Shortcut.String:S
Text=$selectedFileName
"@
    $Ini = Get-IniContent $file
    $Ini["Variables"]['module_shortcuts.totalitems_count'] = $index
    Set-IniContent $Ini $file
    $RmAPI.Bang("[!Refresh $mainConfig]")
    ChangeTargetTo $index
}

function ShortcutRemove {
    param (
    [int]$index,
    [int]$ti,
    [int]$ri
    )
    debug "Removing shortut $index where current total is $ti with $ri shortcuts in each row"
    $Ini = Get-IniContent $file
    $StartingIndex = $index
    $NewTotal = $ti - 1
    debug "$StartingIndex $NewTotal"
    # ------------------------------ Collpase values ----------------------------- #
    for ($i = $StartingIndex; $i -le $NewTotal; $i++) {
        $Ini["Shortcut$i.Shape"]['LeftMouseUpAction']   = $Ini["Shortcut$($i + 1).Shape"]['LeftMouseUpAction']
        $Ini["Shortcut$i.Image"]['ImageName']           = $Ini["Shortcut$($i + 1).Image"]['ImageName']
        $Ini["Shortcut$i.String"]['Text']               = $Ini["Shortcut$($i + 1).String"]['Text']
    }
    # ----------------------------- remove last value ---------------------------- #
    $Ini.Remove("Shortcut$ti.Shape")
    $Ini.Remove("Shortcut$ti.Image")
    $Ini.Remove("Shortcut$ti.String")
    $Ini["Variables"]['module_shortcuts.totalitems_count'] = $ti - 1
    # ------------------------------ Set and refresh ----------------------------- #
    Set-IniContent $Ini $file
    $RmAPI.Bang("[!Refresh $mainConfig]")

    if ($($ti - 1) -ge $index) {
        ChangeTargetTo $index
    }
}

function ShortcutFixNewLine {
    param (
    [int]$ti,
    [int]$ri
    )

    $Ini = Get-IniContent $file
    for ($i = 1; $i -le $ti; $i++) {
        if ($i % $ri -eq 1) {
            $shapestyle = 'Shortcut.Shape:S | Shortcut.Shape.NewLine:S'
        } else {
            $shapestyle = 'Shortcut.Shape:S'
        }
        $Ini["Shortcut$i.Shape"]['MeterStyle'] = $shapestyle
    }
    Set-IniContent $Ini $file
    $RmAPI.Bang("[!Refresh Inlay\Main][!DeactivateConfig]")

}
