﻿; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Sec.SaveLocation=#SKINSPATH#Inlay\WinBlock\Vars\Winblock.inc

[MeasureSysInfo]
Measure=SysInfo
Sysinfotype=OS_VERSION
Substitute="Windows ":""
IfMatch=11
IfMatchAction=[!WriteKeyValue Variables "X" "11" "#Sec.Savelocation#"][!WriteKeyValue Variables "Y" "4" "#Sec.Savelocation#"][!WriteKeyValue Variables "W" "42" "#Sec.Savelocation#"][!WriteKeyValue Variables "H" "42" "#Sec.Savelocation#"][!WriteKeyValue Variables "R" "5" "#Sec.Savelocation#"][!WriteKeyValue Variables "AW" "60" "#Sec.Savelocation#"][!WriteKeyValue Variables "AH" "48" "#Sec.Savelocation#"]
IfNotMatchAction=[!WriteKeyValue Variables "X" "0" "#Sec.Savelocation#"][!WriteKeyValue Variables "Y" "0" "#Sec.Savelocation#"][!WriteKeyValue Variables "W" "50" "#Sec.Savelocation#"][!WriteKeyValue Variables "H" "40" "#Sec.Savelocation#"][!WriteKeyValue Variables "R" "0" "#Sec.Savelocation#"][!WriteKeyValue Variables "AW" "50" "#Sec.Savelocation#"][!WriteKeyValue Variables "AH" "40" "#Sec.Savelocation#"]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=By default, Inlay only replaces the start menu from the windows key. Do you want to replace the start button too?
InlinePattern=from the windows key

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(600*[S]+10*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "ReplaceStart[MeasureSysInfo]" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.1.String]
Meter=String
Text=Yes
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.2.String]
Meter=String
Text=No
MeterStyle=Sec.String:S | Button.String:S
