; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Skin.Name=Mixdeck

[Preset:M]
Measure=Plugin
Plugin=powershellrm
ScriptFile=#@#Powershell\PresetLoad.ps1

[MeasureSysInfo]
Measure=SysInfo
Sysinfotype=OS_VERSION
IfMatch=11
IfMatchAction=[!CommandMeasure Preset:M "ChangeToFiles -theme 'Fluent11'"]
IfNotMatchAction=[!CommandMeasure Preset:M "ChangeToFiles -theme 'Fluent10'"]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Welcome to Mixdeck!

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
