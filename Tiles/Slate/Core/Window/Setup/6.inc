
[Description.String]
Text=If you have multiple monitors, you can choose to blacken / draw over them too! These configurations can be found in the general settings.
InlinePattern=video file|requires an additional executable|animation tab

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "7" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Cool
MeterStyle=Sec.String:S | Button.String:S
