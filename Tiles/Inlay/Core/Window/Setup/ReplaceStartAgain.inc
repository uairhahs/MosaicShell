; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Sec.SaveLocation=#SKINSPATH#Inlay\WinBlock\Vars\Winblock.inc

[MeasureSysInfo]
Measure=SysInfo
Sysinfotype=OS_VERSION
; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Start button aligned left for [MeasureSysInfo] has been overriden. Try clicking on the start button, is it blocked?
InlinePattern=[MeasureSysInfo]

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(600*[S]+10*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Yes
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "DiscordHelp" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=NEg_Button.Shape:S
[Button.2.String]
Meter=String
Text=Still no...
MeterStyle=Sec.String:S | Button.String:S
