[Rainmeter]
; OnUnfocusAction=[!UpdateMeasure UnloadHandler2]

[Variables]
@includeVars=#@#Vars.inc
Sec.W=(#SCREENAREAWIDTH#*0.65)
Sec.H=(0.65*#SCREENAREAWIDTH#*3/4)
Time1=1000
Time2=
TweenNode1=0
Sec.Pos1=899
Sec.Pos2=1009
Sec.Prog=1
Sec.W1=79
Sec.W2=77
Sec.W3=30
Sec.Pos3=1117
Sec.Pos4=360
Sec.W4=40
Sec.Pos5=210
Sec.W5=40

[mTween]
Measure=Script
ScriptFile=#@#LuaTween/Main.lua
TweenGroup=Tweenable
Tween0=Single | Variable | TweenNode1 | 0 | 1 | #Time1#
Optional0=Easing InOutExpo
UpdateDivider=1

[mActions]
Measure=Plugin
Plugin=ActionTimer
ActionList1=Wait 100 | EnableNUOL | Start0
ActionList2=Rewind0 | wait #Time1# | Deactivate
Start0=[!CommandMeasure mTween "Start(0)"]
Rewind0=[!CommandMeasure mTween "Rewind(0)"]
Deactivate=[!WriteKeyValue Variables Sec.Prog 1 "#ROOTCONFIGPATH#Accessories\Overlay\Variants\Guide.inc"][!WriteKeyValue Variables Sec.Guide 0 "#ROOTCONFIGPATH#Main\Home.ini"][!EnableMeasure GameMode][!UpdateMeasure GameMode][!DeactivateConfig]
EnableNUOL=[!EnableMeasureGroup NUOL]

[mSequencer]
Measure=Calc
Formula=#Sec.Prog#
IfCondition=mSequencer <= 4
IfTrueAction=[!SetVariable Sec.Prog (#Sec.Prog#+1)][!UpdateMeter *][!CommandMeasure mTween "Restart(0)"]
IfFalseAction=[!CommandMeasure mActions "Execute 2"]
IfConditionMode=1
DynamicVariables=1
Disabled=1
Group=NUOL

[GameMode]
Measure=Calc
Formula=0
IfCondition=GameMode = 1
IfTrueAction=[!manage gamemode]
IfConditionMode=1
Disabled=1

[Sec.String:S]
Container=Card.Container

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Card.Shape]
Meter=Shape
X=([#Sec.Pos[#Sec.Prog]]+[#SEc.W[#Sec.Prog]])
MeterStyle=Card.Shape.#Sec.Prog#:S
Group=Tweenable
CoreGrad=45 | #Set.Accent_Color# ; 1 | #Set.Accent_Color_2# ;0
DynamicVariables=1
[Card.Container]
Meter=Shape
Y=r
Group=Tweenable
MeterStyle=Card.Container.#Sec.Prog#:S
DynamicVariables=1

[Card.Header]
Meter=String
X=(#Sec.P#)
Y=(10*#Transfer.S#+#Sec.P#)
FontSize=(20*#Transfer.S#)
FontWeight=650
MeterStyle=Sec.String:S | Card.Header.#Sec.Prog#:S
DynamicVariables=1

[Card.Description]
Meter=String
X=r
Y=R
W=(400*#Transfer.S#-#Sec.P#*2)
CLipString=2
MeterStyle=Sec.String:S | Card.Description.#Sec.Prog#:S
DynamicVariables=1

[Card.Progress]
Meter=String
Text=Info #Sec.Prog# / 5
X=r
Y=(200*#Transfer.S#-#Sec.P#)
StringAlign=LeftBottom
FontSize=(8*#Transfer.S#)
FontColor=#Set.Subtext_Color#
DynamicVariables=1
MeterStyle=Sec.String:S

[Card.Button.Shape]
Meter=Shape
X=(400*#Transfer.S#-#Sec.P#)
Y=(200*#Transfer.S#-#Sec.P#)
Shape=Ellipse (-16*#Transfer.S#),(-8*#Transfer.S#),(16*#Transfer.S#) | StrokeWidth (2*#Transfer.S#) | Stroke Color #Set.Positive_Color# | Extend Fill
Fill=Fill Color #Set.Positive_COlor#,150
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_COlor#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!UpdateMeasure mSequencer]
Container=Card.Container
[Card.Button.String]
Meter=STring
X=(-16*#Transfer.S#)r
Y=(-8*#Transfer.S#)r
StringAlign=CenterCenter
Text=[\xe5cc]
FontSize=(20*#Transfer.S#)
FontFace=Material Icons Round
MeterStyle=Sec.String:S

[Card.Header.1:S]
Text=Modules
[Card.Description.1:S]
Text=Modules are tools that can be accessed with hotkeys and buttons. They can help you get the work done!
[Card.Header.2:S]
Text=Widgets
[Card.Description.2:S]
Text=Widgets are customizable skins on your desktop, from clocks to fully functional audio visualizers.
[Card.Header.3:S]
Text=Core settings
[Card.Description.3:S]
Text=You can change the weather location, units and customize core updates & performance here!
[Card.Header.4:S]
Text=News are cool
[Card.Description.4:S]
Text=Get updated on the latest development and roadmap updates here!
[Card.Header.5:S]
Text=Doesn't work? Confused?
[Card.Description.5:S]
Text=Feel free to open a GitHub issue if you need help.

; ------------------------------- Right content ------------------------------ ;
[Card.Shape.1:S]
Y=(#Transfer.S#*80)
Shape=Line 0,0,((-[#Sec.W[#Sec.Prog]])*#TweenNode1#),0 | STrokeWidth (6*#Transfer.S#) | Stroke Color #Set.Positive_COlor# | StrokeStartCap Round | StrokeEndCap Round
Shape2=Rectangle 0,(10*#Transfer.S#),(-400*#Transfer.S#*#TweenNode1#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth (2*#Transfer.S#) | Stroke LinearGradient CoreGrad | Fill Color #Set.Sec_Color#
[Card.Shape.2:S]
Y=(#Transfer.S#*80)
Shape=Line 0,0,((-[#Sec.W[#Sec.Prog]])*#TweenNode1#),0 | STrokeWidth (6*#Transfer.S#) | Stroke Color #Set.Positive_COlor# | StrokeStartCap Round | StrokeEndCap Round
Shape2=Rectangle 0,(10*#Transfer.S#),(-400*#Transfer.S#*#TweenNode1#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth (2*#Transfer.S#) | Stroke LinearGradient CoreGrad | Fill Color #Set.Sec_Color#
[Card.Shape.3:S]
Y=(#Transfer.S#*80)
Shape=Line 0,0,((-[#Sec.W[#Sec.Prog]])*#TweenNode1#),0 | STrokeWidth (6*#Transfer.S#) | Stroke Color #Set.Positive_COlor# | StrokeStartCap Round | StrokeEndCap Round
Shape2=Rectangle 0,(10*#Transfer.S#),(-400*#Transfer.S#*#TweenNode1#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth (2*#Transfer.S#) | Stroke LinearGradient CoreGrad | Fill Color #Set.Sec_Color#
; ------------------------------- Left content ------------------------------- ;
[Card.Shape.4:S]
Y=(#Sec.H#*0.7)
Shape=Line 0,0,((-[#Sec.W[#Sec.Prog]])*#TweenNode1#),0 | STrokeWidth (6*#Transfer.S#) | Stroke Color #Set.Positive_COlor# | StrokeStartCap Round | StrokeEndCap Round
Shape2=Rectangle (-[#Sec.W[#Sec.Prog]]),(10*#Transfer.S#),(400*#Transfer.S#*#TweenNode1#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth (2*#Transfer.S#) | Stroke LinearGradient CoreGrad | Fill Color #Set.Sec_Color#
[Card.Shape.5:S]
Y=(#Sec.H#*0.7)
Shape=Line 0,0,((-[#Sec.W[#Sec.Prog]])*#TweenNode1#),0 | STrokeWidth (6*#Transfer.S#) | Stroke Color #Set.Positive_COlor# | StrokeStartCap Round | StrokeEndCap Round
Shape2=Rectangle (-[#Sec.W[#Sec.Prog]]),(10*#Transfer.S#),(400*#Transfer.S#*#TweenNode1#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth (2*#Transfer.S#) | Stroke LinearGradient CoreGrad | Fill Color #Set.Sec_Color#


; ------------------------------ Right container ----------------------------- ;
[Card.Container.1:S]
X=(-400*#Transfer.S#)r
Shape=Rectangle (400*#Transfer.S#),(10*#Transfer.S#),((-400*#TweenNode1#)*#Transfer.S#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth 0
[Card.Container.2:S]
X=(-400*#Transfer.S#)r
Shape=Rectangle (400*#Transfer.S#),(10*#Transfer.S#),((-400*#TweenNode1#)*#Transfer.S#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth 0
[Card.Container.3:S]
X=(-400*#Transfer.S#)r
Shape=Rectangle (400*#Transfer.S#),(10*#Transfer.S#),((-400*#TweenNode1#)*#Transfer.S#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth 0
; ------------------------------- Left content ------------------------------- ;
[Card.Container.4:S]
X=(-[#SEc.W[#Sec.Prog]])r
Shape=Rectangle 0,(10*#Transfer.S#),((400*#TweenNode1#)*#Transfer.S#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth 0
[Card.Container.5:S]
X=(-[#SEc.W[#Sec.Prog]])r
Shape=Rectangle 0,(10*#Transfer.S#),((400*#TweenNode1#)*#Transfer.S#),(200*#Transfer.S#),(2*#Transfer.S#) | StrokeWidth 0
