[Variables]
; SaveLocation="#@#Vars.ini"

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;


[Preset:M]
Measure=Plugin
Plugin=PowershellRM
ScriptFile=#@#Powershell\PresetLoad.ps1

[String:S]
LeftMouseUpAction=[!CommandMeasure Preset:M "ChangeToFiles -theme '#CURRENTSECTION#'"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[CoreUIv2]
Meter=String
Y=(#Sec.P#)
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[VSCodeGrey]
Meter=String
MeterStyle=String:S
[Div:x]
Meter=Shape
MeterStyle=Div:S
[FrostedGlass]
Meter=String
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[CoreUIv1]
Meter=String
MeterStyle=String:S
[Div:2]
Meter=Shape
MeterStyle=Div:S
[GoldenInsider]
Meter=String
MeterStyle=String:S
[Div:3]
Meter=Shape
MeterStyle=Div:S
[Frosted]
Meter=String
MeterStyle=String:S
[Div:4]
Meter=Shape
MeterStyle=Div:S
[Amarena]
Meter=String
MeterStyle=String:S
[Div:5]
Meter=Shape
MeterStyle=Div:S
[Light]
Meter=String
MeterStyle=String:S
[Div:6]
Meter=Shape
MeterStyle=Div:S
[Amoled]
Meter=String
MeterStyle=String:S
[Div:7]
Meter=Shape
MeterStyle=Div:S
[Classic]
Meter=String
MeterStyle=String:S
[Div:8]
Meter=Shape
MeterStyle=Div:S
[RunicX]
Meter=String
FontFace=RunicX
MeterStyle=String:S
