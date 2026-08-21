﻿
[Header.String]
Text=Warning!

[Description.String]
Text=Slate uses Windows' idle timer to check if you're away from your device. If this counter is not supported on your device, Slate can only be turned on manually using a hotkey.
InlinePattern=Windows' idle timer|manually using a hotkey

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.1.String]
Meter=String
Text=Got it
MeterStyle=Sec.String:S | Button.String:S
