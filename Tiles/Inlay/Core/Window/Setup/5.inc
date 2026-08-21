
[Description.String]
Text=Inlay has nothing in it initially. Select a layout from the layout settings after completing the setup!
InlinePattern=Select a layout

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "6" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Okay, will do!
MeterStyle=Sec.String:S | Button.String:S
