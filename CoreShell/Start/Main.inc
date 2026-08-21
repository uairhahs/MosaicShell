﻿[Rainmeter]
OnRefreshAction=[!ZPos 1][!Draggable 0][!Move (#SCREENAREAWIDTH#/2-#Sec.W#/2) (#SCREENAREAHEIGHT#/2-#Sec.H#/2)][!UpdateMEter *][!Redraw]
AccurateText=1

; ---------------------------------------------------------------------------- ;
;                                  Export vars                                 ;
; ---------------------------------------------------------------------------- ;

[Variables]
@includeVersion=#@#Version.inc
@includeMosaicShellChecker=#SKINSPATH#\#MosaicShell\@Resources\Version.inc
Sec.W=870
Sec.H=540
Sec.P=50
Sec.L=350
Sec.WR=4
Sec.R=2.5
Sec.B=0
Sec.TB=10

Sec.Pri_Color=18,18,18
Sec.Sec_Color=28,28,28
Sec.Ter_Color=35,35,35
Sec.Accent_Color=242, 52, 0
Sec.Accent_Color_2=255, 183, 0
Sec.Text_Color=242,240,255
Sec.SubText_Color=120,120,120

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[MeasureProductName]
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows NT\CurrentVersion
RegValue=ProductName
Substitute="[a-zA-Z]":""
RegExpSubstitute=1
UpdateDivider=-1

[CheckWindows]
Measure=String
String=[MeasureProductName]
DynamicVariables=1
IfMatch=7
IfNotMatchAction=[!HideMeter Dep.PS.Shape][!HideMeter Dep.PS.String][!Redraw]

[CheckHotkey]
Measure=String
String=#ROOTCONFIG#
IFmatch=Substrate|Keystrokes|ValliStart|QuickNote|Keylaunch|IdleStyle|YourMixer|YourFlyouts
IfMatchaction=[!WriteKeyValue Variables RMPATH "#PROGRAMPATH#Rainmeter.exe" "#@#Actions\Hotkeys.ini"]

[CheckScale]
Measure=String
String=#ROOTCONFIG#
IFmatch=Substrate|Keystrokes|ValliStart|QuickNote|IdleStyle|YourMixer|YourFlyouts
IfMatchaction=[!WriteKeyValue Variables Scale "((#SCREENAREAWIDTH#/1920) < (#SCREENAREAHEIGHT#/1080) ? (#SCREENAREAWIDTH#/1920) : (#SCREENAREAHEIGHT#/1080))" "#@#Vars.inc"]

[CheckModular]
Measure=String
String=#ROOTCONFIG#
IFmatch=ModularClocks|ModularPlayers
IfNotMatchAction=[!HideMeter Dep.IM.Shape][!HideMeter Dep.IM.String][!Redraw]

[WebParser]
Measure=WebParser
URL=https://raw.githubusercontent.com/uairhahs/MosaicShell/main/%40Resources/Version.inc
RegExp==(.*)$
OnUpdateAction=[!Delay 1000][!CommandMeasure LogicalScript "check()"]
OnConnectionErrorAction=[!SetOption Dep.Core.Shape MeterStyle "Dep.Shape:S | Dep.ShapeNo:S"][!SetOption Dep.Core.String Text "//Failed to connect to MosaicShell github, if this is an issue please report to me"][!UpdateMEter *][!Redraw]
OnRegExpErrorAction=[!Log Developer:RegExpFalse]
CodePage=1200
Disabled=1

[WebParserCheck]
Measure=Calc
IfCondition=#Parsed# = 1
; Enables the parser if not refreshing the start panel
IfTrueAction=[!EnableMeasure WebParser][!UpdateMeasure WebParser][!CommandMeasure WebParser "Reset"][!Delay 100][!EnableMeasureGroup NUOL][!Log "Enabled NUOL"]

[InternetStatus]
Measure=PLUGIN
Plugin=SysInfo
SysInfoType=INTERNET_CONNECTIVITY
IfCondition=InternetStatus = -1
ifTrueaction=[!SetOption Dep.Core.Shape MeterStyle "Dep.Shape:S | Dep.ShapeNo:S"][!SetOption Dep.Core.String Text "MosaicShell // connection error, please check internet connection"][!UpdateMEter *][!Redraw]

[mVer]
Measure=WebParser
URL=[WebParser]
StringIndex=1
CodePage=1200

[LogicalScript]
Measure=Script
ScriptFile=Logic.lua

[MosaicShellYes]
Measure=String
Group=NUOL
Disabled=1
OnUpdateAction=[!SetOption Dep.Core.Shape MeterStyle "Dep.Shape:S | Dep.ShapeYes:S"][!SetOption Dep.Core.String Text "MosaicShell // v#Core.Ver# installed"][!ShowMeterGroup Button][!UpdateMEter *][!Redraw]

[MosaicShellNo]
Measure=String
Group=NUOL
Disabled=1
OnUpdateAction=[!SetOption Dep.Core.Shape MeterStyle "Dep.Shape:S | Dep.ShapeNo:S"][!SetOption Dep.Core.String Text "MosaicShell // click here to install"][!SetOption Dep.Core.Shape Leftmouseupaction """["#@#Actions\AHKv1.exe" "#@#Actions\Installer.ahk"][!DeactivateConfig]"""][!ShowMeterGroup Dep.Core.Button][!UpdateMEter *][!Redraw]

; [!SetOption Dep.Core.String Text "MosaicShell // download here if it doesn't automatically install"][!SetOption Dep.Core.Shape Leftmouseupaction """["https://github.com/uairhahs/MosaicShell/releases/download/v[mVer]/JaxCore_v[mVer].rmskin"][!UpdateMEter *][!Redraw]"""][!UpdateMEter *][!Redraw]

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[String:S]
FontColor=#Sec.Text_Color#
Fontface=Poppins
AntiAlias=1
X=r
Y=10R
Container=Content.Container
FontSize=10

[Dep.Shape:S]
X=#Sec.P#
Y=35r
Shape=Rectangle 0,0,(#SEc.W#-#Sec.L#-#Sec.P#*2),50,#Sec.R# | StrokeWidth 0 | Extend This
Shape2=Rectangle 0,0,5,50,#Sec.R# | StrokeWidth 0 | Fill Color #Sec.SubText_Color#
This=fill Color 0,0,0,1
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #SEc.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=Content.Container
[Dep.ShapeYes:S]
Shape2=Rectangle 0,0,5,50,#Sec.R# | StrokeWidth 0 | Fill Color 100,255,100
This=Fill Color 0,255,50,50
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color 100,255,100,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,255,50,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=Content.Container
[Dep.ShapeNo:S]
Shape2=Rectangle 0,0,5,50,#Sec.R# | StrokeWidth 0 | Fill Color 255,0,0,100
This=Fill Color 255,0,0,50
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color 255,0,0,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 255,0,0,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=Content.Container
[Dep.Text:S]
X=20r
Y=25r
W=(#SEc.W#-#Sec.L#-#Sec.P#*2-50)
clipString=2
StringAlign=LeftCenter
FontColor=#Sec.Text_Color#
Fontface=Roboto
AntiAlias=1
FontSize=12
InlinePattern=//.*
InlineSetting=Color | #Sec.SubText_Color#
InlinePattern2=//.*
InlineSetting2=Size | 10
Container=Content.Container
[Dep.Button.Shape:S]
X=(#SEc.W#-#Sec.L#-#Sec.P#*2-(20+30))r
Y=r
Shape=Ellipse 0,0,(20) | StrokeWidth 0 | Extend This
This=fill Color #Sec.Text_Color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #SEc.Text_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #sec.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=Content.Container
Hidden=1
[Dep.Button.StringIcon:S]
X=r
Y=r
StringAlign=CenterCenter
FontColor=#Sec.Text_Color#
Fontface=Segoe MDL2 Assets
AntiAlias=1
FontSize=20
InlineSetting=Shadow | 0 | 0 | 5 | 0,0,0
Container=Content.Container
Text=[\xF142]
Hidden=1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;


[Background.Shape]
Meter=Shape
X=(#Sec.B#/2)
Y=(#Sec.B#/2)
Shape=Rectangle 0,0, #Sec.W#, #Sec.H#, #Sec.WR# | StrokeWidth #Sec.B# | Fill Color [#Sec.Pri_Color] | Stroke Color [#Sec.Ter_Color]
[Image.Container]
Meter=Shape
X=(#Sec.W#-#SEc.L#)r
Y=r
Shape=Rectangle 0,0,(#Sec.L#),#Sec.H#,#sec.WR# | strokeWidth 0 | Fill LinearGradient This
This=0 | 0,0,0,255 ; 0.5 | 0,0,0,0 ; 1.0
[Content.Container]
Meter=Shape
X=(#Sec.B#/2)
Y=(#Sec.B#/2+#Sec.TB#)
Shape=Rectangle 0,0, #Sec.W#, #Sec.H#, #Sec.WR# | StrokeWidth 0

[DragBar.Shape]
Meter=Shape
X=(#Sec.B#/2)
Y=(#Sec.B#/2)
Shape=Rectangle 0,0,#Sec.W#,(30),#Sec.WR# | StrokeWidth #Sec.B# | Fill Color [#Sec.Ter_Color] | Stroke Color [#Sec.Ter_Color]
MouseOverAction=[!Draggable 1]
MouseLeaveAction=[!Draggable 0]
MouseActionCursor=0
[DragBar.Image]
Meter=Image
ImageName=Logo.png
X=(5)r
Y=(5)r
W=(30-10)
H=(30-10)
DynamicVariables=1
[DragBar.String]
Meter=String
X=(10)R
y=(#Sec.B#/2+30/2)
FontSize=(8)
FontColor=[#Sec.Subtext_color]
MeterStyle=String:S | DragBar.String:S1
StringAlign=LeftCenter
Container=
Text=MosaicShell module installation window

[Image.Image]
Meter=Image
Container=Image.Container
H=#Sec.H#
W=#Sec.L#
PreserveAspectRatio=2
ImageName=#ROOTCONFIG#.png

[Window.Esc]
Meter=Shape
DynamicVariables=1
Shape=Ellipse (#Sec.W#-15),(15),(5),(5) | StrokeWidth 10 | StrokeColor 0,0,0,1 | Extend FillE
FillE=Fill Color [#Sec.Text_Color],50
MouseOverAction=[!SetOption #CURRENTSECTION# FillE "Fill Color [#Sec.Text_Color]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# FillE "Fill Color [#Sec.Text_Color],50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!DeactivateConfig]

; ---------------------------------------------------------------------------- ;
;                                 Left content                                 ;
; ---------------------------------------------------------------------------- ;

[Header.1.String]
Meter=String
X=(#Sec.P#)
Y=#Sec.P#
Text=MosaicShell
InlineSetting=GradientColor | 180 | #Sec.Accent_Color# ; 0.0 | #Sec.Accent_Color_2# ; 1.0
FontSize=14
FontWeight=650
MeterStyle=String:S
[Header.2.String]
Meter=String
X=r
Y=R
Text=#ROOTCONFIG# #Version#
FontSize=32
FontWeight=650
MeterStyle=String:S

[Des]
Meter=String
Text=Here you can find the list of dependencies. Make sure you are connected to the internet.
W=(#SEc.W#-#Sec.L#-#Sec.P#*2)
clipString=2
MeterStyle=String:S

; ---------------------------------------------------------------------------- ;
;                                 List content                                 ;
; ---------------------------------------------------------------------------- ;

[Dep.Core.Shape]
Meter=Shape
X=r
Y=20R
MEterStyle=Dep.Shape:S
[Dep.Core.String]
Meter=String
Text=MosaicShell hub // Checking...
MeterStyle=Dep.Text:S
[Dep.Core.Button.Shape]
Meter=Shape
LeftMouseUpAction=["https://mosaicshell.gitbook.io/core/getting-started/introduction#why-is-mosaicshell-config-hub-needed"]
MEterStyle=Dep.Button.Shape:S
Group=Dep.Core.Button
[Dep.Core.Button.StringIcon]
Meter=String
MeterStyle=Dep.Button.StringIcon:S
Group=Dep.Core.Button

[Dep.Appreciation.Shape]
Meter=Shape
MEterStyle=Dep.Shape:S | Dep.ShapeYes:S
[Dep.Appreciation.String]
Meter=String
Text=Your appreciation
MeterStyle=Dep.Text:S

[Dep.IM.Shape]
Meter=Shape
MEterStyle=Dep.Shape:S
Leftmouseupaction=["https://github.com/khanhas/MagickMeter#how-to-install"]
[Dep.IM.String]
Meter=String
Text=ImageMagick // this is only needed for some styles.
MeterStyle=Dep.Text:S

[Dep.PS.Shape]
Meter=Shape
MEterStyle=Dep.Shape:S
[Dep.PS.String]
Meter=String
Text=Powershell 5.1 // Not pre-packaged for Windows 7
MeterStyle=Dep.Text:S


; ---------------------------------------------------------------------------- ;
;                                Continue button                               ;
; ---------------------------------------------------------------------------- ;

[ContinueButton]
Meter=Shape
X=(#Sec.P#)
Y=(#Sec.H#-#Sec.P#-40)
DynamicVariables=1
Shape=Rectangle 0,0,120,40,(#Sec.R#/3) | StrokeWidth (2) | Stroke Color "100,255,100" | Extend Fill
Fill=Fill Color 0,0,0,1
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color 100,255,100,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!WriteKeyvalue Variables Skin.Name #ROOTCONFIG# "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!WriteKeyvalue Variables Skin.Set_Page Info "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!ActivateConfig "#MosaicShell\Main" "Settings.ini"][!DeactivateConfig]
Hidden=1
Group=Button
Container=Content.Container
[ContinueText]
Meter=String
X=(60)r
y=(20)r
FontSize=(10)
Text=Continue
FontFace=Roboto
FontWeight=700
StringAlign=CenterCenter
MeterStyle=String:S
Hidden=1
Group=Button
Container=Content.Container
