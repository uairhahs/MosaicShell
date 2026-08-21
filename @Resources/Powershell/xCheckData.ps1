$SkinsPath = $RmAPI.VariableStr('SKINSPATH')

function Update {
    Check-Data
}

function Create-Slate {
    New-Item -Path "$SkinsPath..\CoreData" -Name "Slate" -ItemType "directory"
    New-Item -Path "$SkinsPath..\CoreData\Slate" -Name "Include.inc" -ItemType "file"
    $RmAPI.Log("Created: Slate")
}

function Create-Chord {
    New-Item -Path "$SkinsPath..\CoreData" -Name "Chord" -ItemType "directory"
    New-Item -Path "$SkinsPath..\CoreData\Chord" -Name "Chord.ahk" -ItemType "file"
    New-Item -Path "$SkinsPath..\CoreData\Chord" -Name "Include.inc" -ItemType "file"
    New-Item -Path "$SkinsPath..\CoreData\Chord" -Name "IconCache" -ItemType "directory"
    New-Item -Path "$SkinsPath..\CoreData\Chord\IconCache" -Name "folder.png" -ItemType "file"
    $RmAPI.Log("Created: Chord")
}

function Create-Updater {
    New-Item -Path "$SkinsPath..\CoreData" -Name "Updater" -ItemType "directory"
    Copy-Item -Path "$SkinsPath\#MosaicShell\@Resources\Actions\*" -Destination "$SkinsPath..\CoreData\Updater" -PassThru
    $RmAPI.Log("Created: Updater")
}

function Create-Inlay {
    New-Item -Path "$SkinsPath..\CoreData" -Name "Inlay" -ItemType "directory"
    New-Item -Path "$SkinsPath..\CoreData\Inlay" -Name "Include.inc" -ItemType "file"
    Set-Content "$SkinsPath..\CoreData\Inlay\Include.inc" @"
[Box1]
Meter=Shape
X=(#scale#*25)
Y=(#scale#*100)
MeterStyle=BoxStyle
[Box1Icon]
Meter=Image
MeterStyle=IconStyle
[Box2]
Meter=Shape
MeterStyle=BoxStyle
[Box2Icon]
Meter=Image
MeterStyle=IconStyle
[Box3]
Meter=Shape
MeterStyle=BoxStyle
[Box3Icon]
Meter=Image
MeterStyle=IconStyle
[Box4]
Meter=Shape
MeterStyle=BoxStyle
[Box4Icon]
Meter=Image
MeterStyle=IconStyle
[Box5]
Meter=Shape
MeterStyle=BoxStyle
[Box5Icon]
Meter=Image
MeterStyle=IconStyle
"@
    New-Item -Path "$SkinsPath..\CoreData\Inlay" -Name "IconCache" -ItemType "directory"
    New-Item -Path "$SkinsPath..\CoreData\Inlay\IconCache" -Name "folder.png" -ItemType "file"

    $RmAPI.Log("Created: Inlay")
}

function Create-Combilaunch {
    New-Item -Path "$SkinsPath..\CoreData" -Name "Combilaunch" -ItemType "directory"
    New-Item -Path "$SkinsPath..\CoreData\Combilaunch" -Name "Include.inc" -ItemType "file"
    New-Item -Path "$SkinsPath..\CoreData\Combilaunch" -Name "Actions.inc" -ItemType "file"
    $RmAPI.Log("Created: Combilaunch")
}

function Create-VarInc {
    $source      = $RmAPI.VariableStr('SKINSPATH')
    $destination = Split-Path -Path $source -Parent
    New-Item -Path "$SkinsPath..\CoreData" -Name "Vars.inc" -ItemType "file" -Value "[Variables]`nRAINMETERPATH=$destination"
}

function Check-Data {
    If (Test-Path -Path "$SkinsPath..\CoreData") {
            $RmAPI.Log("Found coredata in programs")
        } else {
            $RmAPI.Log("Failed to find coredata in programs, generating")
            New-Item -Path "$SkinsPath..\" -Name "CoreData" -ItemType "directory"
            $RmAPI.Bang("!Refresh")
        }
    If (Test-Path -Path "$SkinsPath..\CoreData\Chord\IconCache") {
    } else {
        Create-Chord
    }
    If (Test-Path -Path "$SkinsPath..\CoreData\Slate") {
    } else {
        Create-Slate
    }
    # If (Test-Path -Path "$SkinsPath..\CoreData\Inlay\SingleRow.inc") {
    If (Test-Path -Path "$SkinsPath..\CoreData\Inlay\Include.inc") {
    } else {
        Create-Inlay
    }
    If (Test-Path -Path "$SkinsPath..\CoreData\Combilaunch") {
    } else {
        Create-Combilaunch
    }
    If (Test-Path -Path "$SkinsPath..\CoreData\Updater") {
    } else {
        Create-Updater
    }
    If (Test-Path -Path "$SkinsPath..\CoreData\Vars.inc") {
    } else {
        Create-VarInc
    }
}
