[Variables]
Sec.StartAction=[!CommandMeasure Func "startSide('Hotkey', '2')"]


[Description.String]
Text=Enter the hotkey for your third party launcher below.
InlinePattern=hotkey

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(600*[S]+10*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=My hotkey is #Key2InString#
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.2.String]
Meter=String
Text=Refresh
MeterStyle=Sec.String:S | Button.String:S

@includeKeys=#SKINSPATH#Inlay\@Resources\Actions\HotKeys.ini
