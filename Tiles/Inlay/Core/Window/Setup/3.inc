﻿
[Description.String]
Text=By default, Inlay uses the windows search menu for search functionalities. You can use a 3rd party launcher of your choice after installation.
InlinePattern=windows search menu

[Button.1.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Okay!
MeterStyle=Sec.String:S | Button.String:S
