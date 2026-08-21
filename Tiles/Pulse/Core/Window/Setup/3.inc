[Header.String]
Text=The most customization visualizer

[Description.String]
Text=Start by heading over to the layout page and select a pre-built style! Then customize it with the other setting tabs.
InlinePattern=NowPlaying|Windows API

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Got it
MeterStyle=Sec.String:S | Button.String:S
