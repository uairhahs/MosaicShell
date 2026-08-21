[Description.String]
Text=You can hide the default windows volume icon from the windows settings.#CRLF#Taskbar - Turn system icons on or off
InlinePattern=hide the default windows volume icon
InlinePattern2=#CRLF#.*
InlineSetting2=Size | (16*[Set.S])
InlinePattern3=#CRLF#.*
InlineSetting3=Color | #Set.Subtext_Color#

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(620*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[Shell:::{0DF44EAA-FF21-4412-828E-260A8728E7F1}]
MeterStyle=Neu_Button.Shape:S
[Button.1.String]
Meter=String
Text=Go to Windows settings
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.2.String]
Meter=String
Text=Ok
MeterStyle=Sec.String:S | Button.String:S
