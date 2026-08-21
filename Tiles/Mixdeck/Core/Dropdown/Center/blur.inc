﻿[Variables]
@includeSecvars=#@#SecVar.inc
SaveLocation=#SKINSPATH##Skin.Name#\Main\Elements\Unload\Vars.inc
SaveKey=Blur

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[String:S]
LeftMouseUpAction=[!WriteKeyValue Variables #SaveKey# "#CURRENTSECTION#" "#SaveLocation#"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
tooltiptitle=#CURRENTSECTION#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Acrylic]
Meter=String
Y=(#Sec.P#)
Tooltiptext=Modern Windows blur, creates best contrast effect
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Blur]
Meter=String
Tooltiptext=Classic Windows blur, less resource intensive
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[None]
Meter=String
MeterStyle=String:S
