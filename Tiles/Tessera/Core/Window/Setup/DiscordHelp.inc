
[Description.String]
Text=Need help? Open a GitHub issue and continue setup when you're ready.

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(600*[S]+10*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=["https://github.com/uairhahs/MosaicShell/issues"][!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Dis_Button.Shape:S
[Button.1.String]
Meter=String
Text=Okay, continue with setup
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neg_Button.Shape:S
[Button.2.String]
Meter=String
Text=No thanks, continue with setup first
MeterStyle=Sec.String:S | Button.String:S
