﻿[Header.String]
Text=Stuck while using Slate?

[Description.String]
Text=Simply press ESC key or CTRL + Right Click -> Unload to close Slate if it malfunctions
InlinePattern=extremely customizable

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "5" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Okay, will do!
MeterStyle=Sec.String:S | Button.String:S
