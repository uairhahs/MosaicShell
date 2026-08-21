
[p.SysInfo.USER_NAME]
Measure=Plugin
Plugin=SysInfo
SysInfoType=USER_NAME
UpdateDivider=-1

[Func]
MEasure=Script
ScriptFile=#@#Lua\Func.lua

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;


[Action.Shape:S]
Shape=Rectangle  0,0,(200*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Fill=Fill Color #Set.Ter_Color#
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
Container=a.Container
[Action.String:S]
X=(200/2*#Sec.s#)r
Y=(20*#sec.s#)r
FontSize=(10*#Sec.S#)
StringAlign=CenterCenter
Container=a.Container

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Text]
Meter=String
Text=Thank you for choosing MosaicShell!#CRLF#Start by getting some modules or widgets
X=(#Sec.W#/2)
Y=(#Sec.H#/2-([Text:H]+#Sec.P#+40*#Sec.S#)/2)
W=(#Sec.W#-#Sec.WindowPaddingY#*2)
FontSize=(24*#Sec.S#)
ClipString=2
InlinePattern=.*#CRLF#
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 1.0 | #Set.Accent_Color_2# ; 0.0
INlinePattern2=#CRLF#.*
InlineSetting2=Size | (10*#Sec.S#)
INlinePattern3=#CRLF#.*
InlineSetting3=Color | 160,155,145
INlinePattern4=#CRLF#.*
InlineSetting4=Weight | 400
StringAlign=Center
MeterStyle=Sec.String:S
FontWeight=650
DynamicVariables=1

; ----------------------------------- Boxes ---------------------------------- ;

; ---------------------------------- Bottom ---------------------------------- ;

[Action2.Shape]
Meter=Shape
X=(#Sec.W#/2-(200*#Sec.S#)/2)
Y=(#Sec.P#)R
Fill=Fill Color #Set.Positive_Color#
LeftMouseUpAction=[!CommandMeasure p.Actiontimer "Execute 3"]
MeterStyle=Action.Shape:S
[Action2.String]
Meter=String
FontColor=#Set.Pri_Color#
FontWeight=600
Text=Proceed
MeterStyle=Sec.String:S | Action.String:S
