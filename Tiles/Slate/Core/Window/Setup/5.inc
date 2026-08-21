﻿
[Description.String]
Text=Slate also supports using a video file as screensaver! Simply change to the video style in the animation tab. This requires an additional executable to be downloaded. A guide can be found in said page.
InlinePattern=video file|requires an additional executable|animation tab

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "6" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Cool
MeterStyle=Sec.String:S | Button.String:S
