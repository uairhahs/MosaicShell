﻿[Rainmeter]
SkinWidth=#Sec.W#

[Variables]
Sec.W=(500*#Sec.S#)
Sec.H=(450*#Sec.S#)
AniDir=Bottom
@includeSecVars=#@#SecVar.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
@includeSkinvars=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

[ActionTimer]
Custom1=[!SetOption #IconTheme# MeterStyle "Shape:S | Shape2:S"][!UpdateMeter #IconTheme#][!Redraw]

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
[Set.Value:S]
X=(5*#Sec.S#)r
Y=(15*#Sec.S#)r
StringAlign=LeftCenter
FontSize=(10*#Sec.S#)
DynamicVariables=1

[Set.TextBox:S]
X=(#Sec.W#-#Sec.P#-150*#Sec.S#)
DynamicVariables=1
Shape=Rectangle 0,0,150,30,3,3 | StrokeWidth 2 | Fill Color 0,0,0,1 | Extend Fill | Scale #Sec.S#,#Sec.S#,0,0
Fill=Stroke Color #Set.Text_Color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #Set.Text_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!SetVariable Editing "#CURRENTSECTION#"][!SetOption Input:M X "([#CURRENTSECTION#:X]+5*#Sec.S#)"][!SetOption Input:M Y "([#CURRENTSECTION#:Y]+5*#Sec.S#)"][!CommandMeasure Input:M "ExecuteBatch 1"]

[Set.Button:S]
X=(#Sec.W#-#Sec.P#-150*#Sec.S#)
OverColor=100,100,100,200
LeaveColor=100,100,100,100
DynamicVariables=1
Shape=Rectangle 0,0,150,30,3,3 | StrokeWidth 0 | Extend Fill | Scale #Sec.S#,#Sec.S#,0,0
Fill=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'OverColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[&Func:LocalVar('#CURRENTSECTION#', 'Act')]

[Input:M]
Measure=Plugin
Plugin=InputText
SolidColor=#Set.Sec_Color#
FontColor=#Set.Text_Color#
FontFace=#Set.Font#
FontSize=(10*#Sec.S#)
StringAlign=LeftCenter
W=((150-10)*#Sec.S#)
H=((30-10)*#Sec.S#)
DynamicVariables=1
DefaultValue="[##Editing#]"
Command1=[!SetVariable #Editing# "$UserInput$"][!UpdateMeasure SaveAndLoad:M]
[SaveAndLoad:M]
Measure=Calc
DynamicVariables=1
OnUpdateAction=[!WriteKeyvalue Variables #Editing# "[##Editing#]" "#Sec.SaveLocation#"][!SetVariable #Editing# "[##Editing#]"][!UpdateMeter *][!Redraw][!Refresh "#MosaicShell\Main"][!Refresh "#Skin.Name#\Main"]
Disabled=1
Group=NUOL

[Shape:S]
X=(#Sec.p#)
Y=(#sec.P#*1.5)R
Shape=Rectangle 0,0,((#Sec.W#-#sec.P#*3)/2),(32*#sec.S#+2*#Sec.P#),#Sec.R# | Extend This | Extend Stroke
Stroke=StrokeWidth 0
This=Fill Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #set.SubText_Color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables IconTheme "#CURRENTSECTION#" "#Sec.SaveLocation#"][!Refresh "#MosaicShell\Main"][!Refresh "#Skin.Name#\Main"][!Refresh]
DynamicVariables=1
[Shape2:S]
Leftmouseupaction=
Stroke=StrokeWidth 2 | Stroke Color #set.Accent_Color#



[Image:S]
X=(2*#Sec.s#)R
Y=r
W=(32*#Sec.s#)
H=(32*#Sec.s#)
ImageName=#SKINSPATH##SKin.Name#\@Resources\Icons\Tray\[&Func:trim('#CURRENTSECTION#', '%d', '')]\Preview\Volume[&Func:trim('#CURRENTSECTION#', '[a-zA-Z]', '')].png
DynamicVariables=1
; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[KeyLabel]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.P#)
FontSize=(10*#Sec.S#)
FontColor=#Set.Subtext_Color#
Text=Select an icon theme for #Skin.Name#!
inlinepattern=#CRLF#.*
inlinesetting=Size | (8*#Sec.S#)
DynamicVariables=1
StringAlign=Center
MeterStyle=Sec.String:S

[Stuff1]
Meter=String
X=(#Sec.P#)
Y=(#Sec.P#)R
Text=Included icon themes
FontSize=(15*#sec.S#)
FontWeight=600
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
MeterStyle=Sec.String:S

[SegoeDark]
Meter=Shape
X=r
Y=R
MeterStyle=Shape:S
[SegoeDark0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#*5+8*#sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S
[SegoeDark1]
Meter=Image
MeterStyle=Image:S
[SegoeDark2]
Meter=Image
MeterStyle=Image:S
[SegoeDark3]
Meter=Image
MeterStyle=Image:S
[SegoeDark4]
Meter=Image
MeterStyle=Image:S

[SegoeLight]
Meter=Shape
MeterStyle=Shape:S
[SegoeLight0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#*5+8*#sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S
[SegoeLight1]
Meter=Image
MeterStyle=Image:S
[SegoeLight2]
Meter=Image
MeterStyle=Image:S
[SegoeLight3]
Meter=Image
MeterStyle=Image:S
[SegoeLight4]
Meter=Image
MeterStyle=Image:S

[MaterialDark]
Meter=Shape
MeterStyle=Shape:S
[MaterialDark0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#*5+8*#sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S
[MaterialDark1]
Meter=Image
MeterStyle=Image:S
[MaterialDark2]
Meter=Image
MeterStyle=Image:S
[MaterialDark3]
Meter=Image
MeterStyle=Image:S
[MaterialDark4]
Meter=Image
MeterStyle=Image:S

[MaterialLight]
Meter=Shape
MeterStyle=Shape:S
[MaterialLight0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#*5+8*#sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S
[MaterialLight1]
Meter=Image
MeterStyle=Image:S
[MaterialLight2]
Meter=Image
MeterStyle=Image:S
[MaterialLight3]
Meter=Image
MeterStyle=Image:S
[MaterialLight4]
Meter=Image
MeterStyle=Image:S

[FluentColored]
Meter=Shape
X=(#Sec.W#/2+#Sec.P#/2)
Y=[SegoeDark:Y]
MeterStyle=Shape:S
[FluentColored0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#*5+8*#sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S
[FluentColored1]
Meter=Image
MeterStyle=Image:S
[FluentColored2]
Meter=Image
MeterStyle=Image:S
[FluentColored3]
Meter=Image
MeterStyle=Image:S
[FluentColored4]
Meter=Image
MeterStyle=Image:S

[MixerDark]
Meter=Shape
X=(#Sec.W#/2+#Sec.P#/2)
MeterStyle=Shape:S
[MixerDark0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S

[MixerLight]
Meter=Shape
X=(#Sec.W#/2+#Sec.P#/2)
MeterStyle=Shape:S
[MixerLight0]
Meter=Image
X=(((#Sec.W#-#sec.P#*3)/2)/2-(32*#Sec.s#)/2)r
Y=(#Sec.P#)r
MeterStyle=Image:S




; [Stuff2]
; Meter=String
; X=(#Sec.P#)
; Y=(#Sec.P#*2)R
; Text=Custom icon theme
; FontSize=(15*#sec.S#)
; FontWeight=600
; InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
; MeterStyle=Sec.String:S
; [01]
; Meter=String
; Text=Icon theme name
; X=r
; Y=(#Sec.P#)R
; MeterStyle=Sec.String:S
; [02]
; Meter=String
; Text=Open icon theme directory
; X=r
; Y=(#Sec.P#)R
; MeterStyle=Sec.String:S

; ; ---------------------------------------------------------------------------- ;
; ;                                     Input                                    ;
; ; ---------------------------------------------------------------------------- ;

; [IconTheme]
; MEter=Shape
; Y=[01:Y]
; MeterStyle=Set.TextBox:S
; [01value]
; Meter=String
; Text=#IconTheme#
; MeterStyle=Sec.String:S | Set.Value:S

; [button02]
; MEter=Shape
; Y=[02:Y]
; Act=["#SKINSPATH##Skin.Name#\@Resources\Icons\"]
; MeterStyle=Set.Button:S
; [02value]
; Meter=String
; Text=Open
; MeterStyle=Sec.String:S | Set.Value:S
