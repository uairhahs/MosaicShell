﻿[Description.String]
Text=Unfortunately, Tessera will not be able to activate automatically on your device. You can still activate Tessera using a hotkey which can be configurated in the general settings later.
InlinePattern=will not be able to activate automatically
InlineSetting=Color | 255,0,0

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
