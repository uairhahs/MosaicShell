﻿[Description.String]
Text=Great! Tessera works properly on your device.
InlinePattern=Windows' idle timer|manually using a hotkey

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.1.String]
Meter=String
Text=Continue
MeterStyle=Sec.String:S | Button.String:S
