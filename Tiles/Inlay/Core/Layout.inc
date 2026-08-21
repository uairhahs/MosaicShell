﻿[Variables]
Sec.Anchor=AnchorSuppli

[SharePs1]
Measure=Plugin
Plugin=PowershellRM
ScriptFile=#SKINSPATH##SKin.Name#\Core\Share.ps1

[ContentContainer]
MouseScrollDownAction=[!SetVariable Scroll "(Clamp(#Scroll#-100,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseScrollUpAction=[!SetVariable Scroll "(Clamp(#Scroll#+100,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]

[Preset:M]
Measure=Plugin
Plugin=powershellrm
ScriptFile=#@#Powershell\PresetLoad.ps1

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;
[Item.Shape:S]
X=(20*[Set.S])
Y=(40*[SEt.S])r
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
[Item.Button.Shape:S]
X=([ContentContainer:W]-#Set.P#-30*[SEt.S])
Y=(-15*[Set.S])r
Shape=Rectangle 0,0,(30*[Set.S]),(30*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend Fill
OverColor=#Set.Positive_color#,200
LeaveColor=#Set.Positive_color#,100
Fill=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')],#Set.Alpha#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'OverColor')],#Set.Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')],#Set.Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.Button.StringIcon:S]
X=(30*[Set.S]/2)r
Y=(30*[Set.S]/2)r
StringAlign=CenterCEnter
Text=[\xe5ce]
FontFace=Material Icons Round
Container=ContentContainer

[Module.Shape:S]
X=(20*[Set.S])
Y=(60*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),([ContentContainer:W]/4+50*[Set.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Shape2=Line (#Set.P#*0.5),([ContentContainer:W]/4),([ContentContainer:W]-#Set.P#*2),([ContentContainer:W]/4) | STrokeWidth 2 | Extend Stroke
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!CommandMeasure Preset:M "ChangeToFiles -theme 'LY-[&Func:trim('#CURRENTSECTIOn#','.Shape','')]'"][!WriteKeyValue Variables ModuleAllNone 0 "#Sec.SaveLocation#"][!UpdateMeasure Auto_Refresh:M][!Refresh]
DynamicVariables=1
Container=ContentContainer
[Module.Image:S]
X=(10*[Set.S])r
Y=(10*[Set.S])r
W=([ContentContainer:W]-#Set.P#-20*[Set.S])
H=([ContentContainer:W]/4-20*[SEt.S])
ImageName=#SKINSPATH##Skin.Name#\Core\Layout\[&Func:trim('#CURRENTSECTIOn#','.Image','')].png
DynamicVariables=1
Container=ContentContainer
MaskImageName=#SKINSPATH##Skin.Name#\Core\Layout\#LayoutMask.png
[Module.String:S]
X=(#Set.P#/2)r
Y=R
Text=[&Func:trim('#CURRENTSECTIOn#','.String','')]
StringAlign=LeftBottom
FontSize=((20*[Set.S])*#FontSizeModifier#)
FontWeight=650
DynamicVariables=1
Container=ContentContainer
[Module.Description.String:S]
X=r
Y=(20*[Set.S])r
; W=(((#W#-#Set.P#*4)/3)-#Set.P#*2-40*[Set.S])
; H=(#Sec.Box.P#-#Set.P#)
FontSize=((12*[Set.S])*#FontSizeModifier#)
ClipString=2
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
Text=Layouts will modify related settings, modules, color and position.
MeterStyle=Set.String:S | Item.String:S

[Item2.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xe72d]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Text=Share your setup!
MeterStyle=Set.String:S | Item.String:S
[Item2.Button1]
Meter=Shape
ToolTipText=Share your setup!
OverColor=88, 101, 242
LeaveColor=88, 101, 242
X=([ContentContainer:W]-#Set.P#-110*[SEt.S])
LeftMouseUpAction=["https://discord.gg/JmgehPSDD6"]
MeterStyle=Item.Button.Shape:S
[Item2.Button1.StringIcon]
Meter=String
Text=[\xe80d]
MeterStyle=Set.String:S | Item.Button.StringIcon:S
[Item2.Button2]
Meter=Shape
; OverColor=#Set.Accent_Color_2#,255
; LeaveColor=#Set.Accent_Color_2#,200
Overcolor=#Set.Text_Color#,25
LeaveColor=#Set.Text_Color#,25
X=([ContentContainer:W]-#Set.P#-70*[SEt.S])
ToolTipText=Work in progress
; LeftMouseUpAction=[!CommandMeasure SharePs1 "StartImport"]
MeterStyle=Item.Button.Shape:S
[Item2.Button2.StringIcon]
Meter=String
Text=[\xe9fc]
Fontcolor=#set.Subtext_Color#
MeterStyle=Set.String:S | Item.Button.StringIcon:S
[Item2.Button3]
Meter=Shape
ToolTipText=Export your setup file to "@Resources\Presets\LY-Export.inc"
LeftMouseUpAction=[!CommandMeasure SharePs1 "StartExport"]
MeterStyle=Item.Button.Shape:S
[Item2.Button3.StringIcon]
Meter=String
Text=[\xe161]
MeterStyle=Set.String:S | Item.Button.StringIcon:S

[IncludeBlocks]
@includeInstalledBlockIncluder=Layout\Includer.inc

[Win11.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Win11.Image]
Meter=Image
MeterStyle=Module.Image:S
[Win11.String]
Meter=String
MEterStyle=Set.String:S | Module.STring:S
[Win11.Description.String]
Meter=String
Text=Give Valli a Windows 11 look
MEterStyle=Set.String:S | Module.Description.STring:S

[SideBar.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[SideBar.Image]
Meter=Image
MeterStyle=Module.Image:S
[SideBar.String]
Meter=String
MEterStyle=Set.String:S | Module.STring:S
[SideBar.Description.String]
Meter=String
Text=A fluent sidebar.
MEterStyle=Set.String:S | Module.Description.STring:S

[ClassicWavy.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[ClassicWavy.Image]
Meter=Image
MeterStyle=Module.Image:S
[ClassicWavy.String]
Meter=String
MEterStyle=Set.String:S | Module.STring:S
[ClassicWavy.Description.String]
Meter=String
Text=The classic Inlay layout, but with a new wavy background
MEterStyle=Set.String:S | Module.Description.STring:S

[Flat.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Flat.Image]
Meter=Image
MeterStyle=Module.Image:S
[Flat.String]
Meter=String
MEterStyle=Set.String:S | Module.STring:S
[Flat.Description.String]
Meter=String
Text=Flat, informational, clean
MEterStyle=Set.String:S | Module.Description.STring:S

[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
