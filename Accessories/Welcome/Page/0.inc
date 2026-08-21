[Rainmeter]
OnRefreshAction=[!SetWindowPosition 50% 50% 50% 50%][!CommandMeasure p.ActionTimer "Execute 1"]

[Variables]
process.tween_node_0=0

[p.SysInfo.USER_NAME]
Measure=Plugin
Plugin=SysInfo
SysInfoType=USER_NAME
UpdateDivider=-1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Center.Image]
Meter=Image
X=(#Sec.W#/2-(256/2)*#Sec.S#)
Y=(#Sec.H#/2-(256/2)*#Sec.S#)
W=(256*#Sec.S#)
H=(256*#Sec.S#)
ImageName=#@#Images\CoreAssets\#Set.IconStyle#Logo256.png
TransformationMatrix=(Cos(Rad(#process.tween_node_1#*360)));(-Sin(Rad(#process.tween_node_1#*360)));(Sin(Rad(#process.tween_node_1#*360)));(Cos(Rad(#process.tween_node_1#*360)));(([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])-([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])*Cos(Rad(#process.tween_node_1#*360))-([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])*Sin(Rad(#process.tween_node_1#*360)));(([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])+([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])*Sin(Rad(#process.tween_node_1#*360))-([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])*Cos(Rad(#process.tween_node_1#*360)))
ImageAlpha=(255*#process.tween_node_1#)
Container=a.Container
DynamicVariables=1
Group=Tweenable

[SubtleButton]
Meter=Shape
X=(#Sec.W#/2-120*#Sec.S#/2)
Y=(#Sec.P#)R
Shape=Rectangle  0,0,(120*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Fill=Fill Color #Set.Positive_color#
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
LeftMouseUpAction=[!CommandMeasure p.ActionTimer "Execute 2"]
Container=a.Container
[SubtleText]
Meter=String
X=(60*#Sec.s#)r
Y=(20*#sec.s#)r
FontColor=#Set.Pri_color#
StringAlign=CenterCenter
Text=Get started
FontWeight=600
MeterStyle=Sec.String:S

[SkipText]
Meter=String
X=r
Y=(20*#Sec.S#+#SEc.P#)r
FontColor=#Set.Sec_Color#
FontSize=(8*#sec.s#)
StringAlign=CenterCenter
Text=Skip setup wizard
DynamicVariables=1
MouseOverAction=[!SetOption "#CURRENTSECTION#" InlineSetting "Underline"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" InlineSetting ""][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables UserNameCheckVariable "[p.SysInfo.USER_NAME]" "#@#Vars.inc"][!WriteKeyValue Variables Sec.Page 1 "#ROOTCONFIGPATH#Main\Home.ini"][!ActivateConfig "#MosaicShell\Main" "Home.ini"][!DeactivateConfig]
MeterStyle=Sec.String:S
