[Variables]
@includeCacheVarsConfigurator=#@#CacheVars\Configurator.inc
SaveLocation=#SKINSPATH##skin.name#\@Resources\Vars.inc

[String:S]
LeftMouseUpAction=[!WriteKeyvalue variables "#arg1#" "#CURRENTSECTION#" "#SaveLocation#"][!Refresh "#MosaicShell\Main"][!Refresh "#Skin.Name#\Main"][!DeactivateConfig]
Prefix="From "

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Left]
Meter=String
Y=(#Sec.P#)
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Top]
Meter=String
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[Right]
Meter=String
MeterStyle=String:S
[Div:2]
Meter=Shape
MeterStyle=Div:S
[Bottom]
Meter=String
MeterStyle=String:S
