[Variables]
Sec.W=(600*#Sec.S#)
Sec.H=(800*#Sec.S#)
AniDir=Top
Animation_Steps=25
Scroll=0
ScrollClickedPos=0
@includeCoreVars=#@#Vars.inc
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

[PSRM]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Rollback.ps1
ExecutionPolicy=Unrestricted
OnUpdateAction=[!SetOption Anchor Y [Anchor:Y]][!EnableMeasure AnchorChecker][!UpdateMeasure AnchorChecker]

[CoreInstallHandler]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Installer.ps1
ExecutionPolicy=Unrestricted

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

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[List.Item.Shape:S]
X=(#Sec.P#)
Y=(40*#Sec.S#)r
Shape=Rectangle 0,0,(#Sec.W#-#Sec.P#*2),(50*#Sec.S#),(#Sec.R#/2) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Group=List
Container=List.Container
[List.Item.Title.String:S]
X=(#Sec.P#)r
Y=(50*#Sec.S#/2)r
W=((#SEc.W#-#Sec.P#*4)*0.65)
ClipString=1
StringAlign=leftCenter
Group=List
Container=List.Container
[List.Item.Date.String:S]
X=((#SEc.W#-#Sec.P#*4)*0.8)
Y=r
StringAlign=leftCenter
Group=List
Fontcolor=#Set.Subtext_Color#
Container=List.Container
[List.Item.Version.String:S]
X=(#SEc.W#-#Sec.P#*2)
Y=r
StringAlign=RightCenter
Group=List
Container=List.Container

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;


[Header.Image]
Meter=Image
X=(#Sec.W#/2-100*#Sec.S#/2)
Y=(#Sec.P#)
W=(100*#Sec.S#)
H=(100*#Sec.S#)
ImageName=#@#Images\Unified\#Arg.1#.png
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
Text=Downgrade #Arg.1#
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
Text=Please only downgrade if you are experiencing issues. Older versions might not work as expected or might not be compatible with the latest version of Core.
[Header.Description:1]
Text=MosaicShell is unable to fetch the version list from github. Please report this issue in the Discord server.

[List.Container]
Meter=Shape
Y=(#sec.P#)R
Shape=Rectangle 0,0,(#Sec.W#),(#Sec.H#-[#CURRENTSECTION#:Y]-#Sec.P#),#Set.WinR# | StrokeWidth 0
DynamicVariables=1
Group=List
MouseScrollDownAction=[!SetVariable Scroll "(Clamp(#Scroll#-50,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseScrollUpAction=[!SetVariable Scroll "(Clamp(#Scroll#+50,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]

; -------------------------- Includes generated page ------------------------- ;

[List.Item0.Shape]
Y=#Scroll#
DynamicVariables=1

@includeList=#@#Includes\RollbackList.inc

; ---------------------------- Page scroll anchor ---------------------------- ;

[Anchor]
Meter=String
Container=List.Container
x=r
Y=(40*#Sec.S#)r

; -------------------------------- Scroll bar -------------------------------- ;

[ScrollBar:Scrolling]
Fill=Stroke Color #Set.Subtext_Color#
[ScrollBar:Idle]
LeftMouseDownAction=[!SetVariable ScrollClickedPos "$MouseY$"][!SetOption ScrollBar MeterStyle ScrollBar:Scrolling][!EnableMeasure MouseScrollBar][!UpdateMeasure MouseScrollBar][!CommandMeasure MouseScrollBar "Start"]
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #Set.Subtext_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
[ScrollBar]
Meter=Shape
X=([List.Container:XW]-10*[Set.S])
Y=([List.Container:Y]+(#Scroll#/([AnchorChecker]+1)*(([List.Container:H]-#Sec.P#*2)-([List.Container:H]/([List.Container:H]-[AnchorChecker])*([List.Container:H]-#Sec.P#*2)))))
Shape=Line 0,0,0,([List.Container:H]/([List.Container:H]-[AnchorChecker])*([List.Container:H]-#Sec.P#*2)) | StrokeWidth (4*[Set.S]) | StrokeStartCap Round | StrokeEndCap Round | Extend Fill
Fill=Stroke Color #Set.Ter_Color#
DynamicVariables=1
Group=Background | Scrollbar
Hidden=1
MeterStyle=ScrolLBar:Idle
