﻿[Header.String]
Text=Installed DLCs
[Header.Button.Icon.String]
Text=[\xe627]
[Header.Button.Text.String]
Text=Refresh

[Variables]
Page.Items.P=(10*[Set.S])
Page.Items.H=(50*[Set.S])
Page.Items.Content.P=(30*[Set.S])

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[p.FileChoose]
Measure=Plugin
Plugin=FileChoose
ReturnValue=WholePath
FileInitialDirectory=
DynamicVariables=1
Command1=[!CommandMeasure p.PSRM.DLC """moveDLC "$Path$""""]

[p.PSRM.DLC]
Measure=Plugin
Plugin=PowershellRm
ScriptFile=#@#Powershell\MosaicShellSupporter.ps1
ExecutionPolicy=Unrestricted

[m.Complete_Reinstallation]
Measure=Calc
Formula=#Page.Complete_Reinstallation#
IfCondition=m.Complete_Reinstallation = 1
IfTrueAction=[!Delay 100][!CommandMeasure p.PSRM.DLC "reinstall_all"]

; ---------------------------------------------------------------------------- ;
;                                     Style                                    ;
; ---------------------------------------------------------------------------- ;

[List.Item.Shape:S]
X=(-#Page.Items.Content.P#-#Page.Items.H#/2)r
Y=(#Page.Items.H#/2+#Page.Items.P#)r
Shape=Rectangle 0,0,((#Set.W#-#HomePage.P#*3)/2),#Page.Items.H#,(2*[Set.S]) | StrokeWidth 0 | Extend Fill | Extend This
Fill=Fill Color #Set.Sec_Color#
This=Scale 1,1
MouseOverAction=[!SetOption #CURRENTSECTION# This "Scale 1.01,1.01"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Scale 1,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=#Sec.Container#
[List.Item.Shape:Green]
Fill=Fill Color #Set.Positive_Color#
[List.Item.Shape:CoreGrad]
Fill=Fill LinearGradient CoreGrad
CoreGrad=45 | #Set.Accent_Color# ; 1.5 | #Set.Accent_Color_2# ; -0.5
[List.Item.StringIcon:S]
X=(#Page.Items.H#/2)r
Y=(#Page.Items.H#/2)r
FontSize=(16*[SEt.S])
StringAlign=leftCenter
FontFace=Material Icons Round
Container=#Sec.Container#
[List.Item.String:S]
X=(#Page.Items.Content.P#)r
Y=r
W=((#Set.W#-#HomePage.P#*3)/2-#Page.Items.H#/2-#Page.Items.Content.P#*2)
ClipString=1
StringAlign=LeftCenter
Container=#Sec.Container#
[List.Item.String:Green]
FontColor=#Set.Pri_Color#
FontWeight=600

[List.Header.String:S]
FontSize=(16*[Set.S])
[List.Header.Shape:S]
X=r
Y=(#Page.Items.P#)R
Shape=Line 0,0,((#Set.W#-#HomePage.P#*3)/2),0 | StrokeWidth (2*[Set.S]) | Stroke Color #set.Ter_Color# | StrokeStartCap Round | StrokeEndCap Round
DynamicVariables=1
Container=#Sec.Container#


; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; ---------------------------------- Install --------------------------------- ;

[InstalledList.Header.String]
Meter=String
Text=List of DLCs
X=(#HomePage.P#)
Y=(110*[Set.S]+#Scroll#)
MeterStyle=Set.String:S | List.Header.String:S
[InstalledList.Header.Shape]
Meter=Shape
MeterStyle=List.Header.Shape:S

[InstalledList.Anchor]
Meter=String
X=(#Page.Items.Content.P#+#Page.Items.H#/2)r
Y=(-#Page.Items.H#/2)r
DynamicVariables=1
Container=#Sec.Container#


@includeListOfInstalledDLCs=#SKINSPATH#..\CoreData\@DLCs\Includes\Includer.inc

; --------------------------------- Installed -------------------------------- ;

[InstallList.Header.String]
Meter=String
Text=Install here!
X=(#Set.W#/2+#HomePage.P#/2)
Y=(110*[Set.S]+#Scroll#)
MeterStyle=Set.String:S | List.Header.String:S
[InstallList.Header.Shape]
Meter=Shape
MeterStyle=List.Header.Shape:S

[InstallList.Item1.Shape]
Meter=Shape
X=r
Y=(#Page.Items.P#)r
LeftMouseUpAction=[!CommandMeasure p.FileChoose "ChooseFile 1"]
MeterStyle=List.Item.Shape:S | List.Item.Shape:Green
[InstallList.Item1.StringIcon]
Meter=String
Text=[\xe89e]
FontWeight=400
MeterStyle=Set.String:S | List.Item.StringIcon:S | List.Item.String:Green
[InstallList.Item1.String]
Meter=String
Text=Select & Install a DLC package...
MeterStyle=Set.String:S | List.Item.String:S | List.Item.String:Green

[InstallList.Item2.Shape]
Meter=Shape
LeftMouseUpAction=["https://youtu.be/AGEkN9E7DlY"]
MeterStyle=List.Item.Shape:S | List.Item.Shape:CoreGrad
[InstallList.Item2.StringIcon]
Meter=String
Text=[\xe038]
MeterStyle=Set.String:S | List.Item.StringIcon:S | List.Item.String:Green
[InstallList.Item2.String]
Meter=String
Text=Walk-through video
MeterStyle=Set.String:S | List.Item.String:S | List.Item.String:Green

[InstallList.Item3.Shape]
Meter=Shape
LeftMouseUpAction=[!CommandMeasure p.PSRM.DLC "reinstall_all"]
MeterStyle=List.Item.Shape:S
[InstallList.Item3.StringIcon]
Meter=String
Text=[\xf001]
MeterStyle=Set.String:S | List.Item.StringIcon:S
[InstallList.Item3.String]
Meter=String
Text=Re-install all DLCs
MeterStyle=Set.String:S | List.Item.String:S

[InstallList.Item4.Shape]
Meter=Shape
LeftMouseUpAction=["#SKINSPATH#..\CoreData\@DLCs\Packages\"]
MeterStyle=List.Item.Shape:S
[InstallList.Item4.StringIcon]
Meter=String
Text=[\xe2c8]
MeterStyle=Set.String:S | List.Item.StringIcon:S
[InstallList.Item4.String]
Meter=String
Text=Open DLC package directory
MeterStyle=Set.String:S | List.Item.String:S

; ------------------------------- Error message ------------------------------ ;

[Error.Shape]
Meter=Shape
X=(#Set.W#/2-600*[Set.S]/2)
Y=(#Set.H#-(90+20)*[Set.S])
Shape=Rectangle 0,0,(600*[Set.S]),(90*[Set.S]),(#Set.R#) | StrokeWidth 0 | Fill Color 195, 9, 8
DynamicVariables=1
Group=ErrorDialog
Hidden=1
[Error.String]
Meter=String
Text=Title#CRLF#Description
X=(600/2*[Set.S])r
Y=(90/2*[Set.S])r
W=((600-40)*[Set.S])
ClipString=2
Container=
FontSize=(16*[Set.S])
FontWeight=650
InlinePattern=#CRLF#.*
InlineSetting=Size | (10*[SEt.S])
InlinePattern2=#CRLF#.*
InlineSetting2=Weight | 400
StringAlign=CenterCenter
MeterStyle=Set.String:S
Group=ErrorDialog
Hidden=1
