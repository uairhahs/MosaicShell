[Variables]
SaveLocation="#SKINSPATH##sec.name#\Main\Vars\Pixel.inc"

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[String:S]
W=(#Sec.W#-#Sec.P#*2)
CLipString=2
Tooltiptitle=#CURRENTSECTION#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Mixdeck]
Meter=String
Y=(#Sec.P#)
ToolTipText=Launches MosaicShell's Mixdeck. Recommended.
MeterStyle=String:S
LeftMouseUpAction=[!WriteKeyValue Variables BottomActionBang """[!UpdateMeasure mToggle "Mixdeck\Main"]""" "#SaveLocation#"][!WriteKeyValue Variables BottomActionName "#CURRENTSECTION#" "#SaveLocation#"][!Refresh "Tessera\Main"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
[Div:0]
Meter=Shape
MeterStyle=Div:S
[Windows Mixer]
Meter=String
ToolTipText=Launches the default windows application mixer
MeterStyle=String:S
LeftMouseUpAction=[!WriteKeyValue Variables BottomActionBang """[SndVol.exe]""" "#SaveLocation#"][!WriteKeyValue Variables BottomActionName "#CURRENTSECTION#" "#SaveLocation#"][!Refresh "Tessera\Main"][!Refresh "#MosaicShell\Main"][!DeactivateConfig]
