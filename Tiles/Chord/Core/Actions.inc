[Variables]
@includeAct=#SKINSPATH#..\CoreData\Keylaunch\Act.inc
Sec.SaveLocation=#SKINSPATH#..\CoreData\Keylaunch\Act.inc
Sec.ForceWriteVariables=0
Sec.AnchorName=AnchorSuppli

[GenLua]
Measure=Script
ScriptFile="#SKINSPATH##Skin.name#\Core\Gen.lua"

[GenPs1]
Measure=Plugin
Plugin=PowershellRM
ScriptFile="#SKINSPATH##Skin.name#\Core\Gen.ps1"
OnUpdateAction=[!CommandMeasure GenPs1 Load]

; ---------------------------------------------------------------------------- ;
;                                    Styles                                    ;

[Item.Shape:S]
X=(20*[Set.S])
Y=(35*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.StringIcon:S]
X=(50*[Set.S]/2)r
Y=(50*[Set.S]/2)r
StringAlign=leftCenter
FontFace=Material Icons Round
Container=ContentContainer
[Item.String:S]
X=(#Set.P#/2)R
Y=r
StringAlign=LeftCenter
Container=ContentContainer
[Item.Action.Shape:S]
X=(25*[SEt.s])r
Y=(-15*[Set.S])r
Shape=Rectangle 0,0,(30*[Set.S]),(30*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'OverColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=
DynamicVariables=1
Container=ContentContainer
[Item.Action.StringIcon:S]
X=(30*[Set.S]/2)r
Y=(30*[Set.S]/2)r
StringAlign=CenterCEnter
FontFace=Material Icons Round
Container=ContentContainer

[Action.Shape:S]
X=(20*[Set.S])
Y=(35*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure GenLua "TakeDrag('#CURRENTSECTION#')"]
LeftMouseDownAction=[!CommandMeasure GenLua "InitDrag('#CURRENTSECTION#')"]
DynamicVariables=1
Container=ContentContainer
[Action.String:S]
X=(50*[Set.S]/2)r
Y=(50*[Set.S]/2)r
Text=[&Func:trim('#CURRENTSECTIOn#','.String','')] · [#[&Func:trim('#CURRENTSECTIOn#','.String','')]Name]
StringAlign=LeftCenter
Container=ContentContainer
[Action.Hotkey.Shape:S]
X=([ContentContainer:W]-#Set.P#-185*[SEt.S])
Y=(-15*[Set.S])r
Shape=Rectangle 0,0,(150*[Set.S]),(30*[Set.S]),(5*[Set.S]) | StrokeWidth 2 | Extend Stroke
Stroke=Stroke Color 0,0,0,1 | Fill Color 0,0,0,1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,25 | Fill Color #Set.Subtext_Color#,25"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color 0,0,0,1 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
; LeftMouseUpAction=[!CommandMEasure Func "interactionBox([[#SKINSPATH##Skin.Name#\\Core\\InteractionBox\\Hotkey.inc]], '[&Func:trim('#CURRENTSECTIOn#','.Hotkey.Shape','')]')"]
LeftMouseUpAction=[!CommandMEasure Func "startSide([[#SKINSPATH##Skin.Name#\Core\Overlay\Hotkey.inc]], '[&Func:trim('#CURRENTSECTIOn#','.Hotkey.Shape','')]')"]
DynamicVariables=1
Container=ContentContainer
[Action.Hotkey.String:S]
X=(-10*[Set.s])R
Y=(30*[Set.S]/2)r
StringAlign=RightCenter
Text=[#Key[&Func:trim('#CURRENTSECTIOn#','.Hotkey.String','')]InString]
Container=ContentContainer
[Action.Delete.Shape:S]
X=(20*[SEt.S])r
Y=(-15*[Set.S])r
Shape=Rectangle 0,0,(30*[Set.S]),(30*[Set.S]),(5*[Set.S]) | StrokeWidth 2 | Extend Stroke
Stroke=Stroke Color 0,0,0,1 | Fill Color #Set.Subtext_Color#,75
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color# | Fill Color #Set.Subtext_Color#,75"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color 0,0,0,1 | Fill Color #Set.Subtext_Color#,75"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure GenPs1 "WriteRemove -startingIndex [&Func:trim('#CURRENTSECTIOn#','.Delete.Shape','')]"]
DynamicVariables=1
Container=ContentContainer
[Action.Delete.StringIcon:S]
X=(30*[Set.S]/2)r
Y=(30*[Set.S]/2)r
StringAlign=CenterCEnter
Text=[\xe872]
FontFace=Material Icons Round
Container=ContentContainer
; ---------------------------------------------------------------------------- ;
;                                   Includes                                   ;
; ---------------------------------------------------------------------------- ;

[Item1.Shape]
Meter=Shape
Y=(30*[Set.S])R
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xe9ef]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Total number of actions: #Total#
MeterStyle=Set.String:S | Item.String:S

[Item2.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xe86f]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Text=Drag to switch, click to edit
MeterStyle=Set.String:S | Item.String:S
[Item2.Action1.Shape]
Meter=Shape
X=([ContentContainer:W]-#Set.P#-60*[SEt.S])
OverColor=#set.Positive_Color#,250
LeaveColor=#set.Positive_Color#,150
LeftMouseUpAction=[!CommandMeasure GenPs1 "WriteAdd"]
MeterStyle=Item.Action.Shape:S
[Item2.Action1.StringIcon]
Meter=String
Text=[\xe145]
MeterStyle=Set.String:S | Item.Action.StringIcon:S
[Item2.Action2.Shape]
Meter=Shape
OverColor=255,0,50,150
LeaveColor=255,0,50,100
LeftMouseUpAction=[!CommandMeasure GenLua "Reset()"]
MeterStyle=Item.Action.Shape:S
[Item2.Action2.StringIcon]
Meter=String
Text=[\xf053]
MeterStyle=Set.String:S | Item.Action.StringIcon:S

[Div.1]
MEter=Shape
X=(20*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Set.Div:S

[1.Shape]
Y=(10*[Set.S])R

@includeInclude=#SKINSPATH#..\CoreData\Keylaunch\Include.inc


; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R

@includeKeys=#SKINSPATH#..\CoreData\Keylaunch\Hotkeys.inc
