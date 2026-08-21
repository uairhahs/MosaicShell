

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;


[Action.Shape:S]
X=(150/2*#Sec.s#+#sec.P#/2)r
Y=(-20*#sec.s#)r
Shape=Rectangle  0,0,(150*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Fill=Fill Color #Set.Ter_Color#
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
Container=a.Container
[Action.String:S]
X=(150/2*#Sec.s#)r
Y=(20*#sec.s#)r
FontSize=(10*#Sec.S#)
StringAlign=CenterCenter
Container=a.Container

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Text]
Meter=String
Text=Welcome to MosaicShell#CRLF#A configurable desktop shell built from composable surfaces. Arrange the pieces, tune the surface, and build an environment that fits the way you work.
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
InlineSetting3=Color | #Set.Text_Color#
INlinePattern4=#CRLF#.*
InlineSetting4=Weight | 400
StringAlign=Center
MeterStyle=Sec.String:S
FontWeight=650
DynamicVariables=1

; ----------------------------------- Boxes ---------------------------------- ;

; ---------------------------------- Bottom ---------------------------------- ;

[Action1.Shape]
Meter=Shape
X=(#Sec.W#/2-(150*1*#Sec.S#+#Sec.P#*0)/2)
Y=(#Sec.p#)R
Fill=Fill Color #Set.Positive_Color#
LeftMouseUpAction=[!CommandMeasure p.ActionTimer "Execute 2"]
MeterStyle=Action.Shape:S
[Action1.String]
Meter=String
FontColor=#Set.Pri_Color#
FontWeight=600
Text=Continue
MeterStyle=Sec.String:S | Action.String:S
; [Action2.Shape]
; Meter=Shape
; LeftMouseUpAction=[!WriteKeyValue Variables PostWelcome.GameMode 1 "#ROOTCONFIGPATH#CoreShell\Home\Page1.inc"][!CommandMeasure p.ActionTimer "Execute 2"]
; MeterStyle=Action.Shape:S
; [Action2.String]
; Meter=String
; Text=Configure after
; MeterStyle=Sec.String:S | Action.String:S
; [Action3.Shape]
; Meter=Shape
; LeftMouseUpAction=[!WriteKeyValue Variables PostWelcome.GameMode 0 "#ROOTCONFIGPATH#CoreShell\Home\Page1.inc"][!CommandMeasure p.ActionTimer "Execute 2"]
; MeterStyle=Action.Shape:S
; [Action3.String]
; Meter=String
; Text=Skip
; MeterStyle=Sec.String:S | Action.String:S
