﻿[Variables]
SaveLocation="#SKINSPATH##sec.name#\@Resources\Vars.inc"
SaveKey=CollapseIcons

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[String:S]
W=(#Sec.W#-#Sec.P#*2)
CLipString=2
LeftMouseUpAction=[!WriteKeyValue Variables #SaveKey# "#CURRENTSECTION#" "#SaveLocation#"][!Refresh "Mixdeck\Main"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
Tooltiptitle=#CURRENTSECTION#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Separate]
Meter=String
Y=(#Sec.P#)
ToolTipText=Have separate rows for additional channels from an app
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Hide]
Meter=String
ToolTipText=Hide additional channels from an app
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[Combine]
Meter=String
ToolTipText=Combine additional channels from an app to a single row (up to 4)
MeterStyle=String:S
