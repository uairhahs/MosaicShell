﻿function Create-Shortcut {
    $RainmeterExe = $RmAPI.VariableStr('PROGRAMPATH')
    $CoreResourceFolder = $RmAPI.VariableStr('@')
    $ResourceFolder = "$($RmAPI.VariableStr('SKINSPATH'))Inlay\@Resources\"
    $OrbImageName = $RmAPI.VariableStr('OrbImageName')
    $Dir = "$($RmAPI.VariableStr('SKINSPATH'))\..\CoreData\Inlay\Inlay.lnk"

    & "$($CoreResourceFolder)Actions\AHKv1.exe" "$($CoreResourceFolder)Addons\CoreAHKFunctions.ahk" "Shortcut_Regular" "$Dir" "$($RainmeterExe)Rainmeter.exe" '[!UpdateMeasure valliActionReceiver Inlay\Winblock]' "$($ResourceFolder)Images\StartOrbs\$OrbImageName.ico"

    $Path = Split-Path $Dir | Resolve-Path

    $RmAPI.Bang("[`"$Path`"]")
}

