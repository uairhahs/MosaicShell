﻿[Variables]
@includeSecvars=#@#SecVar.inc

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[Preset:M]
Measure=Plugin
Plugin=PowershellRM
ScriptFile=#@#Powershell\PresetLoad.ps1

[String:S]
LeftMouseUpAction=[!CommandMeasure Preset:M "ChangeToFiles -theme 'PixelColorTheme-#CURRENTSECTION#'"][!Refresh "#MosaicShell\Main"][!Refresh "Tessera\Main"][!DeactivateConfig]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[You Blue]
Meter=String
Y=(#Sec.P#)
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[You Green]
Meter=String
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[You Pink]
Meter=String
MeterStyle=String:S
[Div:2]
Meter=Shape
MeterStyle=Div:S
[You Gold]
Meter=String
MeterStyle=String:S
[Div:3]
Meter=Shape
MeterStyle=Div:S
[You Blue-Light]
Meter=String
MeterStyle=String:S
