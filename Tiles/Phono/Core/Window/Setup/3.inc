[Header.String]
Text=Choose and customize!

[Description.String]
Text=Choose a layout in general settings, and configure the layout in style options!
InlinePattern=customize|core settings

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Okay, will do!
MeterStyle=Sec.String:S | Button.String:S
