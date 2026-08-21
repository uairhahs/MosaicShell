[Header.String]
Text=Customize everything!

[Description.String]
Text=You can customize almost every aspect of the mixer, and it's tray icon in the core settings.
InlinePattern=customize|core settings

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
