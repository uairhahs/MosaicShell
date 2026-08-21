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
LeftMouseUpAction=[!CommandMeasure Preset:M "ChangeToFiles -theme 'CenterColorTheme-#CURRENTSECTION#'"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Contrast]
Meter=String
Y=(#Sec.P#)
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Bluish]
Meter=String
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[Black]
Meter=String
MeterStyle=String:S
