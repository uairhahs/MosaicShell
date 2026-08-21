﻿[Header.Image]
ImageName=#SKINSPATH#Pulse\@Resources\Images\Warning.png

[Header.String]
Text=A Visualizer will use additional system resources.

[Description.String]
Text=Although the visualizer is as optimized as possible, it still would use more system resources depending on your system.
InlinePattern=NowPlaying|Windows API

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Got it
MeterStyle=Sec.String:S | Button.String:S
