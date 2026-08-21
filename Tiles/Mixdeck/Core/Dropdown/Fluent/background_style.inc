[Variables]
@includeSecvars=#@#CacheVars\Configurator.inc

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

[Fluent10]
Meter=String
Y=(#Sec.P#)
Text=Win10
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Fluent11]
Meter=String
Text=Win11
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[Solid]
Meter=String
MeterStyle=String:S
