[Variables]
SaveLocation="#SKINSPATH##Sec.Name#\Main\Styles\Vars\Measure.inc"
SaveKey=Rotation

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[String:S]
W=(#Sec.W#-#Sec.P#*2)
CLipString=2
LeftMouseUpAction=[!WriteKeyValue Variables #SaveKey# "#CURRENTSECTION#" "#SaveLocation#"][!Refresh "#Sec.Name#\Main"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Vert]
Meter=String
Y=(#Sec.P#)R
Text=Vertical
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Hori]
Meter=String
Text=Horizontal
MeterStyle=String:S
