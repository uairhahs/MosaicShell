[Variables]
Sec.W=(600*#Sec.S#)
Sec.H=(800*#Sec.S#)
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
ScriptFile=#@#Powershell\Installer.ps1
ExecutionPolicy=Unrestricted
OnUpdateAction=[!SetOption Anchor Y [Anchor:Y]][!EnableMeasure AnchorChecker][!UpdateMeasure AnchorChecker][!CommandMeasure CoreInstallHandler BatchInstall-CreateList]

[AnchorChecker]
Measure=Calc
Formula=(Clamp([List.Container:YH]-[Anchor:Y], -5000, 0))
IfCondition=AnchorChecker=0
IfTrueAction=[!DisableMouseAction List.Container "MouseScrollUpAction|MouseScrollDownAction"][!UpdateMeterGroup Background][!Redraw]
IfFalseAction=[!ShowMeter ScrollBar][!UpdateMeterGroup Background][!Redraw]
IfConditionMode=1
Group=AnchorChecker
DynamicVariables=1
Disabled=1

[MouseScrollBar]
Measure=Plugin
Plugin=Mouse
MouseMoveAction=[!SetVariable Scroll "(Clamp(($MouseY$-#ScrollClickedPos#-[List.Container:Y])/(#Sec.H#/2-#Sec.P#)*[AnchorChecker],[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
LeftMouseUpAction=[!SetOption ScrollBar MeterStyle ScrollBar:Idle][!SetOption ScrollBar Fill "Stroke Color #Set.Ter_Color#"][!CommandMeasure MouseScrollBar "Stop"][!DisableMeasure MouseScrollBar][!UpdateMeter ScrollBar][!Redraw]
RequireDragging=1
RelativeToSkin=1
DynamicVariables=1
Disabled=1

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
Formula=([List.Container:YH]-[List.Container:H]/2)
DynamicVariables=1

[CircleBarRadius]
Measure=Calc
Formula=(128*#Sec.S#)
DynamicVariables=1
OnUpdateAction=[!CommandMeasure s.CircleBarHelper "ImportCircleBar()"][!CommandMeasure s.CircleBarHelper "DrawCircleBar(0)"]


; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[ListItem.Shape:S]
X=(#Sec.P#)
Y=(40*#Sec.S#)r
Shape=Rectangle 0,0,(#Sec.W#-#Sec.P#*2),(50*#Sec.S#),(#Sec.R#/2) | Extend Fill | Extend Stroke | Extend Scale | Stroke LinearGradient CoreGrad
Fill=Fill Color #Set.Sec_Color#
Stroke=StrokeWidth 0
Scale=Scale 1,1
CoreGrad=45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Group=List
Container=List.Container
LeftMouseDownAction=[!SetOption #CURRENTSECTION# Scale "Scale 0.95,0.95"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure CoreInstallHandler "BatchInstall-AddToList('[&Func:trim('#CURRENTSECTION#', '.Shape', '')]')"][!SetOption #CURRENTSECTION# MeterStyle "ListItem.Shape:S | ListItem.Shape:Selected"][!SetOption #CURRENTSECTION# Scale "Scale 1,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
[ListItem.Shape:Selected]
Stroke=StrokeWidth (2*#Sec.S#)
LeftMouseUpAction=[!CommandMeasure CoreInstallHandler "BatchInstall-RemoveFromList('[&Func:trim('#CURRENTSECTION#', '.Shape', '')]')"][!SetOption #CURRENTSECTION# MeterStyle "ListItem.Shape:S"][!SetOption #CURRENTSECTION# Scale "Scale 1,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
[ListItem.Image:S]
X=(#Sec.P#)r
Y=((50-32)*#Sec.S#/2)r
W=(32*#Sec.S#)
H=(32*#Sec.S#)
ImageName="#@#Icons\LibraryIcons\[&Func:trim('#CURRENTSECTION#', '.Image', '')].png"
Group=List
DynamicVariables=1
Container=List.Container
[ListItem.String:S]
X=(#Sec.P#)R
Y=(32*#Sec.S#/2)r
W=((#SEc.W#-#Sec.P#*4)*0.65)
ClipString=1
StringAlign=leftCenter
Group=List
Text=[&Func:trim('#CURRENTSECTION#', '.String','')]
DynamicVariables=1
Container=List.Container
[ListItem.Button.String:S]
X=(#Sec.W#-#Sec.P#*2)
Y=r
ClipString=1
StringAlign=RightCenter
Group=List
FontFace=Material Icons Round
FontSize=(16*#Sec.S#)
FontColor=#Set.Subtext_Color#
Text=[\xe89e]
DynamicVariables=1
LeftMouseUpAction=["https://github.com/MosaicShell/[&Func:trim('#CURRENTSECTION#', '.Button.String','')]"]
Container=List.Container

[Category.String:S]
X=(#Sec.W#/2)
Y=(32*#Sec.S#/2+#Set.P#)r
ClipString=1
Group=List
StringAlign=Center
FontColor=#Set.Subtext_Color#
Text=[&Func:trim('#CURRENTSECTION#', '.Category.String','')]
DynamicVariables=1
Container=List.Container

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


[Header.Image]
Meter=Image
X=(#Sec.W#/2-100*#Sec.S#/2)
Y=(#Sec.P#)
W=(100*#Sec.S#)
H=(100*#Sec.S#)
ImageName=#@#Images\BatchInstall.png
DynamicVariables=1
[Header.String]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.P#/2)R
StringAlign=Center
MeterStyle=Sec.String:S | Header.String:0
FontSize=(14*#sec.s#)
FontWeight=650
[Header.String:0]
Text=Core Batch Installer
[Header.String:1]
Text=Uh oh, something went wrong
[Header.Description]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.P#/2)R
StringAlign=Center
W=(#Sec.W#-#Sec.P#*2)
ClipString=2
FontColor=#Sec.Subtext_Color#
MeterStyle=Sec.String:S | Header.Description:0
[Header.Description:0]
Text=Choose what modules or widgets you would like to start with.
[Header.Description:1]
Text=MosaicShell is unable to fetch the version list from GitHub. Please report this as a GitHub issue.

[List.Container]
Meter=Shape
Y=(#sec.P#)R
Shape=Rectangle 0,0,(#Sec.W#),(#Sec.H#-[#CURRENTSECTION#:Y]-#Sec.P#-100*#Sec.S#),#Set.WinR# | StrokeWidth 0 | Extend Fill
DynamicVariables=1
Group=List
Fill=Fill Color 0,0,0,255
MouseScrollDownAction=[!SetVariable Scroll "(Clamp(#Scroll#-50,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseScrollUpAction=[!SetVariable Scroll "(Clamp(#Scroll#+50,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseActionCursor=0


[SCRIPTED.ProgressBar.Shape]
Meter=shape
Line=0,0 | LineTo 0,0
Shape=Ellipse [CircleBarCenterX],[CircleBarCenterY],[CircleBarRadius] | StrokeWidth (2*#Sec.S#) | Stroke Color #Set.Subtext_Color# | Fill Color 0,0,0,0
Shape2=Path Line
CircleBarLine=StrokeWidth (12*#Sec.S#) | Stroke LinearGradient AccentGradient | StrokeStartCap Round | StrokeEndCap Round | Fill Color 0,0,0,0
AccentGradient=45 | #Set.Accent_Color# ; 0 | #Set.Accent_Color_2# ; 1
Group=CircleBar | Hidden
Hidden=1
DynamicVariables=1

[ProgressBar.String]
Meter=String
X=[CircleBarCenterX]
Y=[CircleBarCenterY]
Text=0%
FontWeight=600
StringAlign=CenterCenter
FontSize=(20*#Sec.S#)
Group=Progress | Hidden
DynamicVariables=1
Hidden=1
MeterStyle=Sec.String:S

[ProgressBar.Title.String]
Meter=String
X=r
Y=R
Text=...
StringAlign=Center
FontSize=(10*#Sec.S#)
Group=Hidden
DynamicVariables=1
Hidden=1
MeterStyle=Sec.String:S

; -------------------------- Includes generated page ------------------------- ;

[Modules.Category.String]
Meter=String
Y=#Scroll#
MeterStyle=Sec.String:S | Category.String:S

[Tessera.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S | ListItem.Shape:Selected
[Tessera.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Tessera.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Tessera.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Mixdeck.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S | ListItem.Shape:Selected
[Mixdeck.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Mixdeck.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Mixdeck.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Inlay.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Inlay.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Inlay.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Inlay.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Slate.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Slate.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Slate.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Slate.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Chord.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Chord.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Chord.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Chord.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Substrate.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Substrate.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Substrate.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Substrate.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Widgets.Category.String]
Meter=String
MeterStyle=Sec.String:S | Category.String:S

[Chrono.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Chrono.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Chrono.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Chrono.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Phono.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Phono.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Phono.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Phono.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Pulse.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Pulse.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Pulse.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Pulse.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

[Canvas.Shape]
Meter=Shape
MeterStyle=ListItem.Shape:S
[Canvas.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Canvas.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.String:S
[Canvas.Button.String]
Meter=String
MeterStyle=Sec.String:S | ListItem.Button.String:S

; ---------------------------- Page scroll anchor ---------------------------- ;

[Anchor]
Meter=String
Container=List.Container
x=r
Y=(100*#Sec.S#)r

; ------------------------------ Bottom buttons ------------------------------ ;

[Button1.Shape]
Meter=Shape
X=(#Sec.W#/2-(150*#Sec.S#*2+#Sec.P#)/2)
Y=(#Sec.H#-#Sec.P#-40*#Sec.S#)
Fill=Fill Color #Set.Ter_Color#
LeftMouseUpAction=[!CommandMeasure ActionTimer "Execute 2"][!WriteKeyValue Variables Sec.Page 1 "#ROOTCONFIGPATH#Main\Home.ini"][!ActivateConfig "#MosaicShell\Main" "Home.ini"]
MeterStyle=Button.Shape:S
[Button1.String]
Meter=String
Text="I'll get 'em later"
MeterStyle=Sec.String:S | Button.String:S
[Button2.Shape]
Meter=Shape
Fill=Fill Color #Set.Positive_color#
LeftMouseUpAction=[!WriteKeyValue "#CURRENTCONFIG#" Active 0 "#SETTINGSPATH#Rainmeter.ini"][!CommandMeasure CoreInstallHandler "InstallBatch"]
MeterStyle=Button.Shape:S
[Button2.String]
Meter=String
Text="Install"
FontColor=#Set.Pri_color#
FontWeight=600
MeterStyle=Sec.String:S | Button.String:S

; -------------------------------- Scroll bar -------------------------------- ;

[ScrollBar:Scrolling]
Fill=Stroke Color #Set.Subtext_Color#
[ScrollBar:Idle]
LeftMouseDownAction=[!SetVariable ScrollClickedPos "$MouseY$"][!SetOption ScrollBar MeterStyle ScrollBar:Scrolling][!EnableMeasure MouseScrollBar][!UpdateMeasure MouseScrollBar][!CommandMeasure MouseScrollBar "Start"]
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #Set.Subtext_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
[ScrollBar]
Meter=Shape
X=([List.Container:XW]-10*#sec.s#)
Y=([List.Container:Y]+(#Scroll#/([AnchorChecker]+1)*(([List.Container:H]-#Sec.P#*2)-([List.Container:H]/([List.Container:H]-[AnchorChecker])*([List.Container:H]-#Sec.P#*2)))))
Shape=Line 0,0,0,([List.Container:H]/([List.Container:H]-[AnchorChecker])*([List.Container:H]-#Sec.P#*2)) | StrokeWidth (4*#sec.s#) | StrokeStartCap Round | StrokeEndCap Round | Extend Fill
Fill=Stroke Color #Set.Ter_Color#
DynamicVariables=1
Group=Background | Scrollbar
Hidden=1
MeterStyle=ScrolLBar:Idle

[Esc]
Meter=
