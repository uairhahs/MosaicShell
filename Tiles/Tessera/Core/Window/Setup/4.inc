[Header.Image]
Y=(50*[S])

[Header.String]
Text=Accent colors

[Description.String]
Text=You can change the variant of the layout in the layouts tabs, and further customize it in the appearance settings!
InlinePattern=variant of the layout|further customize

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "6" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Got it
MeterStyle=Sec.String:S | Button.String:S
