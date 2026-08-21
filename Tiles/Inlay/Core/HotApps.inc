﻿[Variables]
@includeSaveHotAppVars=#SKINSPATH##Skin.Name#\@Resources\HotApps.inc


[LogicLua]
Measure=Script
ScriptFile=#SKINSPATH##Skin.name#\Core\Module\Logic.lua

; ---------------------------------------------------------------------------- ;
;                                    Styles                                    ;
; ---------------------------------------------------------------------------- ;
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
FontFace=Segoe Fluent Icons
Container=ContentContainer
[Item.String:S]
X=(#Set.P#/2)R
Y=r
StringAlign=LeftCenter
Container=ContentContainer

[HotApp.Shape:S]
X=(20*[Set.S])
Y=(40*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure LogicLua "TakeDragApps('#CURRENTSECTION#')"]
LeftMouseDownAction=[!CommandMeasure LogicLua "InitDragApps('#CURRENTSECTION#')"]
DynamicVariables=1
Container=ContentContainer
[HotApp.StringIcon:S]
X=(50*[Set.S]/2)r
Y=(50*[Set.S]/2)r
StringAlign=leftCenter
Text="[\x[#[#[&Func:trim('#CURRENTSECTIOn#','.StringIcon','')]Actual]Icon]]"
FontFace=Segoe Fluent Icons
FontSize=((16*[Set.S])*#FontSizeModifier#)
FontColor=#Set.Text_Color#,(150+105*[#[&Func:trim('#CURRENTSECTIOn#','.StringIcon','')]Bool])
Container=ContentContainer
[HotApp.String:S]
X=(#Set.P#/2)R
Y=r
Text="[#[&Func:trim('#CURRENTSECTIOn#','.String','')]Actual]"
FontColor=#Set.Text_Color#,(150+105*[#[&Func:trim('#CURRENTSECTIOn#','.String','')]Bool])
StringAlign=LeftCenter
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Item1.Shape]
Meter=Shape
Y=(30*[Set.S])R
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xe946]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Drag between hotapps to switch, click to toggle it on / off
MeterStyle=Set.String:S | Item.String:S

[Div.1]
MEter=Shape
X=(20*[Set.S])
MeterStyle=Set.Div:S

[HotApp1.Shape]
Meter=Shape
Y=(10*[Set.S])R
MeterStyle=HotApp.Shape:S
[HotApp1.StringIcon]
Meter=String
MeterStyle=Set.String:S | HotApp.StringIcon:S
[HotApp1.String]
Meter=String
MeterStyle=Set.String:S | HotApp.String:S

[HotApp2.Shape]
Meter=Shape
MeterStyle=HotApp.Shape:S
[HotApp2.StringIcon]
Meter=String
MeterStyle=Set.String:S | HotApp.StringIcon:S
[HotApp2.String]
Meter=String
MeterStyle=Set.String:S | HotApp.String:S

[HotApp3.Shape]
Meter=Shape
MeterStyle=HotApp.Shape:S
[HotApp3.StringIcon]
Meter=String
MeterStyle=Set.String:S | HotApp.StringIcon:S
[HotApp3.String]
Meter=String
MeterStyle=Set.String:S | HotApp.String:S

[HotApp4.Shape]
Meter=Shape
MeterStyle=HotApp.Shape:S
[HotApp4.StringIcon]
Meter=String
MeterStyle=Set.String:S | HotApp.StringIcon:S
[HotApp4.String]
Meter=String
MeterStyle=Set.String:S | HotApp.String:S

[HotApp5.Shape]
Meter=Shape
MeterStyle=HotApp.Shape:S
[HotApp5.StringIcon]
Meter=String
MeterStyle=Set.String:S | HotApp.StringIcon:S
[HotApp5.String]
Meter=String
MeterStyle=Set.String:S | HotApp.String:S

[HotApp6.Shape]
Meter=Shape
MeterStyle=HotApp.Shape:S
[HotApp6.StringIcon]
Meter=String
MeterStyle=Set.String:S | HotApp.StringIcon:S
[HotApp6.String]
Meter=String
MeterStyle=Set.String:S | HotApp.String:S

; ---------------------------------------------------------------------------- ;
;                                   Settings                                   ;
; ---------------------------------------------------------------------------- ;

[S12313]
Meter=String
Text=Settings
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Program list directory
MeterStyle=Set.String:S | Set.OptionName:S
[Div:a33d48]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=List show icons
MeterStyle=Set.String:S | Set.OptionName:S
[Div:123848]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=List show folders (might show junk folders)
MeterStyle=Set.String:S | Set.OptionName:S
[Div:123822]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=List maximum number of rows
MeterStyle=Set.String:S | Set.OptionName:S
[Div:123322]
Meter=Shape
MeterStyle=Set.Div:S
[Option05]
Meter=String
Text=Open holiday xml file
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[ProgramListDirectory]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
W=(400*[Set.S])
ClipString=1
Text=#ProgramListDirectory#
MeterStyle=Set.String:S | Set.Value:S

[ListShowIcon]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option02:Y]-#Set.P#+(-20/2+8)*[Set.S])

[ListShowFolders]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option03:Y]-#Set.P#+(-20/2+8)*[Set.S])

[ListMax]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value04]
Meter=String
Type=Num|0|100
Text=#ListMax# rows
MeterStyle=Set.String:S | Set.Value:S

[Button05]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option05:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_Color#,150
LeaveColor=#Set.Accent_Color#,50
Act=["#SKINSPATH#Inlay\@Resources\Calendars\Holidays.xml"]
[Icon05]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=((14*[Set.S])*#FontSizeModifier#)
MeterStyle=Set.String:S | Set.Value:S
[Value05]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=Open xml file
MeterStyle=Set.String:S | Set.Value:S
