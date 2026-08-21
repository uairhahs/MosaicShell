[Description.String]
Text=Unfortunately Windows 11 does not support hiding the default volume icon. Do you want to use an alternative mixer icon for Mixdeck?
InlinePattern=does not support

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(620*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyvalue variables IconTheme "MixerDark" "#SKINSPATH#Mixdeck\@Resources\Vars.inc"][!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.1.String]
Meter=String
Text=Yes
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.2.String]
Meter=String
Text=No
MeterStyle=Sec.String:S | Button.String:S
