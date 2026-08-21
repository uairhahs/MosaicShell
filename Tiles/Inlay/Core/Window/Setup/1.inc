
; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Sec.SaveLocation=#SKINSPATH#Inlay\@Resources\Vars.inc

[MeasureSysInfo]
Measure=SysInfo
Sysinfotype=OS_VERSION
IfMatch=11
IfMatchAction=[!WriteKeyValue Variables "WinR" "8" "#Sec.Savelocation#"]
IfNotMatchAction=[!WriteKeyValue Variables "WinR" "0" "#Sec.Savelocation#"]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Welcome to Inlay!

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
