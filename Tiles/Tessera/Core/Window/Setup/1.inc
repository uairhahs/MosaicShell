﻿; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[Variables]
Sec.SaveLocation=#SKINSPATH#Tessera\Main\Vars\Center.inc
Sec.SaveLocation2=#SKINSPATH#Tessera\Main\Vars\Fluent.inc
Sec.SaveLocation3=#SKINSPATH#Tessera\Main\Vars\Win11.inc
Sec.SaveLocation4=#SKINSPATH#Tessera\@Resources\Vars.inc

[MeasureSysInfo]
Measure=SysInfo
Sysinfotype=OS_VERSION
IfMatch=11
IfMatchAction=[!WriteKeyValue Variables "BlurCorner" "Round" "#Sec.Savelocation#"][!WriteKeyValue Variables "BlurCorner" "Round" "#Sec.Savelocation2#"][!WriteKeyValue Variables "BlurCorner" "Round" "#Sec.Savelocation3#"][!WriteKeyValue Variables "Border" "0" "#Sec.Savelocation2#"][!WriteKeyValue Variables "Border" "0" "#Sec.Savelocation3#"]
IfNotMatchAction=[!WriteKeyValue Variables "BlurCorner" "None" "#Sec.Savelocation#"][!WriteKeyValue Variables "BlurCorner" "None" "#Sec.Savelocation2#"][!WriteKeyValue Variables "BlurCorner" "None" "#Sec.Savelocation3#"][!WriteKeyValue Variables "Border" "1" "#Sec.Savelocation2#"][!WriteKeyValue Variables "Border" "1" "#Sec.Savelocation3#"]

[MeasureCurrentBuild]
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows NT\CurrentVersion
RegValue=CurrentBuild
IfCondition=MeasureCurrentBuild >= 22533
IfTrueAction=[!Log "Windows insider edition detected."][!WriteKeyValue Variables "LegacyVol" "1" "#Sec.SaveLocation4#"]
IfFalseAction=[!Log "User is not using an insider edition of Windows"][!WriteKeyValue Variables "LegacyVol" "0" "#Sec.SaveLocation4#"]
UpdateDivider=-1

; ---------------------------------------------------------------------------- ;
;                                    content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Welcome to Tessera!

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "2" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Let's go!
MeterStyle=Sec.String:S | Button.String:S
