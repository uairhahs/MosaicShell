[Rainmeter]
Update=1000

[Idletime]
Measure=SysInfo
SysInfoType=IDLE_TIME
UpdateDivider=1

[Header.String]
Text=Idle duration: [IdleTime]
DynamicVariables=1
UpdateDivider=1

[Description.String]
Text=Please do not touch your inputs and wait for five seconds. Does the timer above increase?
InlinePattern=do not touch your inputs|five seconds



[Button.1.Shape]
Meter=Shape
X=(#W#/2-(600*[S]+10*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "Supported" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Yes
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "Unsupported" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neg_Button.Shape:S
[Button.2.String]
Meter=String
Text=No
MeterStyle=Sec.String:S | Button.String:S
