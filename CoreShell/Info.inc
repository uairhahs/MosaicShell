[Variables]
BarHandler=1

DeviantArtLinks=881177085|881628513|883898019|885183361|886577256|889349339|890749449|893506095|894951390|899004964|903806619|905969076|919259685:Combilaunch|Plainext|ModularClocks|Substrate|ModularPlayers|Keystrokes|Keylaunch|ValliStart|QuickNote|IdleStyle|ModularVisualizer|YourMixer|YourFlyouts

BetaSkinlist=

Sec.HeaderImageName=#Skin.name#
Sec.Status=Loading

; -------------------------------- Status text ------------------------------- ;
[Item1.String:Status:Loading]
Text=#Skin.Name##CRLF#Loading
[Item2.String:Status:Loading]
Text=Loading...
[Item1.String:Status:Clone]
Text=#Skin.Name##CRLF#This is a clone
[Item2.String:Status:Clone]
Text=Note that a clone will not receive updates
[Item1.String:Status:NotInstalled]
Text=#Skin.Name##CRLF#Not installed - [mVer]
[Item2.String:Status:NotInstalled]
Text=Click on the download button above to install #Skin.name# [mVer]!
[Item1.String:Status:Installed]
Text=#Skin.Name##CRLF#Installed  - #Version#
[Item2.String:Status:Installed]
Text=You're on the latest version of #Skin.Name#!
[Item1.String:Status:NotU2D]
Text=#Skin.Name##CRLF#Update available  - [mVer]
[Item2.String:Status:NotU2D]
Text=Click on the download button above to update #Skin.name# to [mVer]!
[Item1.String:Status:DevBuild]
Text=#Skin.Name##CRLF#Installed - #Version#
[Item2.String:Status:DevBuild]
Text=You're on a dev build of #SKin.Name#! Main branch [mVer]
[Item1.String:Status:Beta]
Text=#Skin.Name##CRLF#Not released - currently in beta!
[Item2.String:Status:Beta]
Text=Join the Discord server to participate in the public beta test!
; ------------------------------ Status text end ----------------------------- ;

@includepatchNoteChecker=#SKINSPATH##Skin.Name#\@Resources\PatchNoteVar.inc

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

; ------------------------------ Install handler ----------------------------- ;
[CoreInstallHandler]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Installer.ps1
ExecutionPolicy=Unrestricted

; ----------------------------- Check module type ---------------------------- ;
[VersionScript]
Measure=Script
ScriptFile=#@#Lua\VersionScript.lua
DynamicVariables=1

; ---------------- Parse the version and update mC4U measures ---------------- ;
[WebParser]
Measure=WebParser
URL=https://raw.githubusercontent.com/MosaicShell/#Skin.Name#/main/%40Resources/Version.inc
RegExp==(.*)$
Disabled=1
FinishAction=[!CommandMeasure VersionScript "CheckVersion()"]
Group=checkForBeta
CodePage=1200
[mVer]
Measure=WebParser
URL=[WebParser]
StringIndex=1
CodePage=1200

; --------------- Get user name to save when showing patchnote --------------- ;
[p.SysInfo.USER_NAME]
Measure=Plugin
Plugin=SysInfo
SysInfoType=USER_NAME
OnUpdateAction=[!Delay 100][!CommandMeasure VersionScript "CheckPatchNotes()"]


; ---------------------------------------------------------------------------- ;
;                                  meterStyle                                  ;
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
[Item.Shape.Tall:S]
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(120*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
[Item.Shape.Big:S]
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(200*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
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
Shape=Rectangle 0,0,(30*[Set.S]),(30*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend Stroke
Stroke=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'Color', '#Set.Positive_color#')],100
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'Color', '#Set.Positive_color#')],200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'Color', '#Set.Positive_color#')],100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.Button.StringIcon:S]
X=(30*[Set.S]/2)r
Y=(30*[Set.S]/2)r
StringAlign=CenterCEnter
Text=[\xe5ce]
FontFace=Material Icons Round
Container=ContentContainer
[Item.Button.Shape.Tall:S]
X=([ContentContainer:W]-#Set.P#-60*[SEt.S])
Y=(-30*[Set.S])r
Shape=Rectangle 0,0,(60*[Set.S]),(60*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend Stroke
[Item.Button.StringIcon.Tall:S]
X=(60*[Set.S]/2)r
Y=(60*[Set.S]/2)r
Fontsize=(20*[SEt.s])

[Tag.Shape:S]
X=(30*[Set.S])R
Y=(-15*[Set.S])r
Shape=Rectangle 0,0,([&[&Func:trim('#CURRENTSECTION#', '.Shape', '')].String:W]+40*[Set.S]),(30*[Set.S]),(15*[Set.S]) | StrokeWidth 0 | Extend Stroke
Stroke=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'Color', '#Set.Subtext_Color#')],100
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'Color', '#Set.Subtext_Color#')],200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'Color', '#Set.Subtext_Color#')],100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
Group=Tags
[Tag.String:S]
Container=ContentContainer
DynamicVariables=1
X=(20*[Set.S])r
Y=(15*[Set.S])r
FontSize=(11*[Set.S])
Stringalign=Leftcenter
Group=Tags

[Act.Shape:S]
X=(((([ContentContainer:W]-#Set.P#-(50+50)*[Set.S])/4)/2)+20*[Set.S])r
Y=(-40*[Set.S])r
Shape=Rectangle 0,0,(([ContentContainer:W]-#Set.P#-(50+50)*[Set.S])/4),(60*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend Stroke
Stroke=Fill Color #Set.SubText_Color#,100
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color #Set.SubText_Color#,200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color #Set.SubText_Color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Act.String:S]
Container=ContentContainer
DynamicVariables=1
X=((([ContentContainer:W]-#Set.P#-(50+50)*[Set.S])/4)/2)r
Y=(25*[Set.S])r
FontSize=(18*[Set.S])
StringAlign=CenterCenter
FontFace=Material Icons Round
[Act.Des.String:S]
X=r
Y=(15*[SEt.S])r
FontSize=(8*[Set.S])
StringAlign=Center
Container=ContentContainer
DynamicVariables=1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header]
Meter=String
X=(20*[Set.S])
Y=(20*[Set.S]+#Scroll#)
DynamicVariables=1
Text=Information
FontSize=(25*[Set.S])
FontWeight=600
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0
MeterStyle=Set.String:S
Container=ContentContainer


[Item1.Shape]
Meter=Shape
Y=(#Set.P#)R
MeterStyle=ITem.Shape:S | ITem.Shape.Tall:S
[Item1.Image]
Meter=Image
X=(10*[Set.S])r
Y=(10*[Set.S])r
W=(100*[Set.S])
H=(100*[Set.S])
ImageName=#@#Images\Unified\#Sec.HeaderImageName#.png
DynamicVariables=1
Container=ContentContainer
[Item1.String]
Meter=String
X=(#Set.P#/2)R
Y=(100*[Set.S]/2)r
StringAlign=LeftCenter
FontWeight=650
InlinePattern=(.*)#CRLF#.*
InlineSetting=Size | (24*[SEt.S])
InlinePattern2=#CRLF#.*
InlineSetting2=Weight | 300
Container=ContentContainer
MEterStyle=Set.String:S | Item1.String:Status:#Sec.Status#
Group=Status
[Item1.Button.Shape]
Meter=Shape
Hidden=1
Group=Download
LeftMouseUpAction=[!DeactivateConfig "#Skin.Name#\Main"][!CommandMeasure CoreInstallHandler "Install #Skin.Name#"]
MeterStyle=Item.Button.Shape:S | Item.Button.Shape.Tall:S
[Item1.Button.StringIcon]
Meter=String
Text=[\xe884]
Hidden=1
Group=Download
MeterStyle=Set.String:S | Item.Button.StringIcon:S | Item.Button.StringIcon.Tall:S

[Item2.Shape]
Meter=Shape
Y=(100*[Set.S]/2+25*[Set.S])r
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xe946]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Group=Status
MeterStyle=Set.String:S | Item.String:S | Item2.String:Status:#Sec.Status#
[Item3.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
; ---------------------------------- Github ---------------------------------- ;
[Item3.Tag1.Shape]
Meter=Shape
X=(10*[Set.S])r
Y=(10*[Set.S])r
LeftMouseUpAction=["https://github.com/MosaicShell/#Skin.Name#"]
MeterStyle=Tag.Shape:S
[Item3.Tag1.String]
Meter=String
Text=Github
MEterStyle=Set.String:S | Tag.String:S
; -------------------------------- DeviantArt -------------------------------- ;
[Item3.Tag2.Shape]
Meter=Shape
Color=57, 159, 89
LeftMouseUpAction=[!CommandMeasure CoreInstallHandler "Open-DALink"]
MeterStyle=Tag.Shape:S
[Item3.Tag2.String]
Meter=String
Text=DeviantArt
MEterStyle=Set.String:S | Tag.String:S
; --------------------------------- PatchNote -------------------------------- ;
[Item3.Tag3.Shape]
Meter=Shape
Color=#set.Accent_Color#
LeftMouseUpAction=[!commandMeasure Func "interactionBox('PatchNote')"]
MeterStyle=Tag.Shape:S
[Item3.Tag3.String]
Meter=String
Text=Patch notes
MEterStyle=Set.String:S | Tag.String:S
; ---------------------------------- Discord --------------------------------- ;
[Item3.Tag4.Shape]
Meter=Shape
Color=88, 101, 242
LeftMouseUpAction=["https://discord.gg/JmgehPSDD6"]
MeterStyle=Tag.Shape:S
[Item3.Tag4.String]
Meter=String
Text=Discord server
MEterStyle=Set.String:S | Tag.String:S

; ---------------------------------------------------------------------------- ;
;                                 Smart actions                                ;
; ---------------------------------------------------------------------------- ;

[Item4.Shape]
Meter=Shape
MeterStyle=ITem.Shape:S | ITem.Shape.Tall:S
[Item4.StringIcon]
Meter=String
Text=[\xf271]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item4.String]
Meter=String
Text=Actions
MeterStyle=Set.String:S | Item.String:S
; ------------------------------ Duplicate skin ------------------------------ ;
[Item4.Act1.Shape]
Meter=Shape
X=(40*[Set.S])
Y=(20*[Set.S])r
LeftMouseUpAction=[!CommandMeasure Func "interactionBox('DuplicateSkin', '#SKIN.NAME#')"]
MeterStyle=Act.Shape:S
Group=HideIsClone
[Item4.Act1.String]
Meter=String
Text=[\xe14d]
Group=HideIsClone
MEterStyle=Set.String:S | Act.String:S
[Item4.Act1.Des.String]
Meter=String
Text=Make clone
Group=HideIsClone
MEterStyle=Set.String:S | Act.Des.String:S
; -------------------------------- Auto scale -------------------------------- ;
[Item4.Act2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Scale "((#SCREENAREAWIDTH#/1920) < (#SCREENAREAHEIGHT#/1080) ? (#SCREENAREAWIDTH#/1920) : (#SCREENAREAHEIGHT#/1080))" "#Sec.SaveLocation#"][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Act.Shape:S
[Item4.Act2.String]
Meter=String
Text=[\xe85b]
MEterStyle=Set.String:S | Act.String:S
[Item4.Act2.Des.String]
Meter=String
Text=Apply AutoScale
MEterStyle=Set.String:S | Act.Des.String:S
; ------------------------------ Reset location ------------------------------ ;
[Item4.Act3.Shape]
Meter=Shape
LeftMouseUpAction=[!Move 0 0 "#Skin.Name#\Main"]
MeterStyle=Act.Shape:S
[Item4.Act3.String]
Meter=String
Text=[\xe1b3]
MEterStyle=Set.String:S | Act.String:S
[Item4.Act3.Des.String]
Meter=String
Text=Reset location
MEterStyle=Set.String:S | Act.Des.String:S
; ------------------------------ Reset location ------------------------------ ;
[Item4.Act4.Shape]
Meter=Shape
LeftMouseUpAction=[!CommandMeasure Func "startOverlay('DebugActions')"]
MeterStyle=Act.Shape:S
[Item4.Act4.String]
Meter=String
Text=[\xe868]
MEterStyle=Set.String:S | Act.String:S
[Item4.Act4.Des.String]
Meter=String
Text=Debug actions
MEterStyle=Set.String:S | Act.Des.String:S
