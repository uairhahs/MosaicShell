[Variables]
SaveLocation="#@#Vars.inc"
; SaveKey=Fill

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Small]
Meter=String
Y=(#Sec.P#)
LeftMouseUpAction=[!WriteKeyValue Variables Set.W 600 #SaveLocation#][!WriteKeyValue Variables Set.H 450 #SaveLocation#][!WriteKeyValue Variables UserScale 0.75 "#@#ScaleVars.inc"][!SetVariable UserScale 0.75][!SetOption Set.S Formula "(#DpiScale#*#UserScale#)"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[Medium]
Meter=String
LeftMouseUpAction=[!WriteKeyValue Variables Set.W 750 #SaveLocation#][!WriteKeyValue Variables Set.H 500 #SaveLocation#][!WriteKeyValue Variables UserScale 0.9 "#@#ScaleVars.inc"][!SetVariable UserScale 0.9][!SetOption Set.S Formula "(#DpiScale#*#UserScale#)"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
MeterStyle=String:S
[Div:2]
Meter=Shape
MeterStyle=Div:S
[Big]
Meter=String
LeftMouseUpAction=[!WriteKeyValue Variables Set.W 1000 #SaveLocation#][!WriteKeyValue Variables Set.H 800 #SaveLocation#][!WriteKeyValue Variables UserScale 1 "#@#ScaleVars.inc"][!SetVariable UserScale 1][!SetOption Set.S Formula "(#DpiScale#*#UserScale#)"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
MeterStyle=String:S
[Div:3]
Meter=Shape
MeterStyle=Div:S
[Fullscreen]
Meter=String
LeftMouseUpAction=[!WriteKeyValue Variables Set.W (#SCREENAREAWIDTH#) #SaveLocation#][!WriteKeyValue Variables Set.H (#SCREENAREAHEIGHT#) #SaveLocation#][!WriteKeyValue Variables UserScale 1 "#@#ScaleVars.inc"][!SetVariable UserScale 1][!SetOption Set.S Formula "(#DpiScale#*#UserScale#)"][!Refresh "#MosaicShell\Main"][!Move 0 0 "#MosaicShell\Main"][!DeactivateConfig]
MeterStyle=String:S
