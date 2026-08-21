; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Skin.Name=Phono

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Welcome to Phono!

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "2" "#CURRENTPATH#Main.ini"][!ActivateConfig  "Phono\Main"][!Refresh "#MosaicShell\Main"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Let's go!
MeterStyle=Sec.String:S | Button.String:S
