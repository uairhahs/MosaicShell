
[Header.String]
Text=Appearance customization

[Description.String]
Text=By default, the accent colors of your system are used. You can specify an accent color in the appearance settings.
InlinePattern=accent colors|appearance settings

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "7" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Got it
MeterStyle=Sec.String:S | Button.String:S
