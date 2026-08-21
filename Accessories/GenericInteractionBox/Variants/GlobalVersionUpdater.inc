[Variables]
Sec.W=(600*#Sec.S#)
Sec.H=(600*#Sec.S#)
AniDir=Top
Animation_Steps=25
Scroll=0
ScrollClickedPos=0
@includeSkinList=#ROOTCONFIGPATH#CoreWebResources.inc
[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#WORKAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[CoreInstallHandler]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ExecutionPolicy=Unrestricted
ScriptFile=#@#Powershell\Installer.ps1

; ---------------------------------- Circle ---------------------------------- ;

[s.CircleBarHelper]
Measure=Script
ScriptFile=#@#Lua\CircleBarHelper.lua
BarMeter=SCRIPTED.ProgressBar.Shape
BarShapeIndex=2

[CircleBarCenterX]
Measure=Calc
Formula=(#Sec.W#/2)
[CircleBarCenterY]
Measure=Calc
Formula=(#Sec.H#/2-(#SEc.P#+[ProgressBar.Title.String:H])/2)
DynamicVariables=1

[CircleBarRadius]
Measure=Calc
Formula=(128*#Sec.S#)
DynamicVariables=1
OnUpdateAction=[!CommandMeasure s.CircleBarHelper "ImportCircleBar()"][!CommandMeasure s.CircleBarHelper "DrawCircleBar(100)"][!Delay 500][!CommandMeasure CoreInstallHandler "CheckAvailableUpdates"]

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[Button.Shape:S]
X=(#Sec.P#+150/2*#Sec.S#)r
Y=(-40/2*#sec.s#)r
Shape=Rectangle  0,0,(150*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
Group=Button
[Button.String:S]
Meter=String
X=(150/2*#Sec.s#)r
Y=(40/2*#sec.s#)r
StringAlign=CenterCenter
DynamicVariables=1
Group=Button

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[SCRIPTED.ProgressBar.Shape]
Meter=shape
Line=0,0 | LineTo 0,0
Shape=Ellipse [CircleBarCenterX],[CircleBarCenterY],[CircleBarRadius] | StrokeWidth (2*#Sec.S#) | Stroke Color #Set.Subtext_Color# | Fill Color 0,0,0,0
Shape2=Path Line
CircleBarLine=StrokeWidth (12*#Sec.S#) | Stroke LinearGradient AccentGradient | StrokeStartCap Round | StrokeEndCap Round | Fill Color 0,0,0,0
AccentGradient=45 | #Set.Accent_Color# ; 0 | #Set.Accent_Color_2# ; 1
Group=CircleBar
DynamicVariables=1

[ProgressBar.String]
Meter=String
X=[CircleBarCenterX]
Y=[CircleBarCenterY]
Text=0
FontWeight=600
StringAlign=CenterCenter
FontSize=(20*#Sec.S#)
Group=Progress
DynamicVariables=1
MeterStyle=Sec.String:S

[ProgressBar.Title.String]
Meter=String
X=r
Y=([CircleBarRadius]+#Sec.P#)r
Text=Checking for updates...
StringAlign=Center
Group=Progress
FontSize=(10*#Sec.S#)
DynamicVariables=1
MeterStyle=Sec.String:S

[Button1.Shape]
Meter=Shape
X=(#Sec.W#/2-(150*#Sec.S#*2+#Sec.P#)/2)
Y=(#Sec.H#-#Sec.P#-40*#Sec.S#)
Fill=Fill Color #Set.Ter_Color#
LeftMouseUpAction=[!CommandMeasure ActionTimer "Execute 2"][!WriteKeyValue Variables Sec.Page 1 "#ROOTCONFIGPATH#Main\Home.ini"][!ActivateConfig "#MosaicShell\Main" "Home.ini"]
MeterStyle=Button.Shape:S
[Button1.String]
Meter=String
Text="Cancel"
MeterStyle=Sec.String:S | Button.String:S
[Button2.Shape]
Meter=Shape
Fill=Fill Color #Set.Positive_color#
MeterStyle=Button.Shape:S
[Button2.String]
Meter=String
Text="Loading..."
FontColor=#Set.Pri_color#
FontWeight=600
MeterStyle=Sec.String:S | Button.String:S

[Esc]
Meter=
