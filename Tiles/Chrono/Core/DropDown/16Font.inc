[Variables]
SaveLocation="#SKINSPATH##Sec.Name#\Main\Styles\Vars\16.inc"
SaveKey=FontFace

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

[SF Pro Display]
Meter=String
Y=(#Sec.P#)R
MeterStyle=String:S
[Div:0]
Meter=Shape
MeterStyle=Div:S
[SF Pro Rounded]
Meter=String
MeterStyle=String:S
[Div:1]
Meter=Shape
MeterStyle=Div:S
[Poppins]
Meter=String
MeterStyle=String:S
[Div:2]
Meter=Shape
MeterStyle=Div:S
[Samsung Sharp Sans]
Meter=String
MeterStyle=String:S
[Div:3]
Meter=Shape
MeterStyle=Div:S
[Change custom font]
Meter=String
LeftMouseUpAction=["#SKINSPATH#Chrono\Main\Styles\Vars\16.inc"]
MeterStyle=String:S
