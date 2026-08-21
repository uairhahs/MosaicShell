﻿; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Sec.SaveLocation=#SKINSPATH#Inlay\WinBlock\Vars\Winblock.inc

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Drag the red box to the location of the start button. If the box does not overlap well with the start button, you can change it's size in the settings later.
InlinePattern=red box | location of the start button
InlineSetting=Color | 255,0,0

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables "Mode" "1" "#Sec.Savelocation#"][!Refresh "Inlay\WinBlock"][!WriteKeyValue Variables Sec.arg2 "ReplaceStartAgain" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Done!
MeterStyle=Sec.String:S | Button.String:S
