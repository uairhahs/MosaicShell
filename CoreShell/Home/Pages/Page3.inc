﻿[Variables]
Page.Items.P=(10*[Set.S])
Page.Items.H=(60*[Set.S])
Page.Items.Content.P=(10*[Set.S])
@includeSkinList=#ROOTCONFIGPATH#COreWebResources.inc

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[LibraryPageHelper]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\LibraryPageHelper.ps1

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[List.Header.String:S]
FontSize=(16*[Set.S])
[List.Header.Shape:S]
X=r
Y=(#Page.Items.P#)R
Shape=Line 0,0,((#Set.W#-#HomePage.P#*3)/2),0 | StrokeWidth (2*[Set.S]) | Stroke Color #set.Ter_Color# | StrokeStartCap Round | StrokeEndCap Round
DynamicVariables=1
Container=#Sec.Container#

[ListItem.Shape:S]
X=(-(#Set.W#-#HomePage.P#*3)/2+#Page.Items.Content.P#)r
Y=(#Page.Items.H#/2+#Page.Items.P#)r
Shape=Rectangle 0,0,((#Set.W#-#HomePage.P#*3)/2),#Page.Items.H#,(#Set.R#) | StrokeWidth 0 | Extend This
This=Fill Color #Set.Sec_Color#
Container=#Sec.Container#
DynamicVariables=1
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_color# | Scale 1.01,1.01"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure Func "corepage('[&Func:trim('#CURRENTSECTION#', '.Shape', '')]')"]
Group=List
Hidden=1


[ListItem.Image:S]
X=#Page.Items.Content.P#r
Y=#Page.Items.Content.P#r
W=(#Page.Items.H#-#Page.Items.Content.P#*2)
H=(#Page.Items.H#-#Page.Items.Content.P#*2)
ImageName="#@#Icons\LibraryIcons\[&Func:trim('#CURRENTSECTION#', '.Image', '')].png"
Container=#Sec.Container#
ImageTint=#Set.Text_Color#
ImageAlpha=100
DynamicVariables=1
Group=List
Hidden=1
[ListItem.Image:Installed]
ImageAlpha=255

[ListItem.Name.String:S]
X=(#Page.Items.Content.P#)R
Y=(#Page.Items.H#/2-#Page.Items.Content.P#)r
Text=[&Func:trim('#CURRENTSECTION#', '.Name.String', '')] (Not Installed)
FontColor=#Set.Subtext_Color#
StringAlign=LeftCenter
InlinePattern=\(.*\)
InlineSetting=Size | (10*[Set.S])
DynamicVariables=1
Group=List
Hidden=1
[ListItem.Name.String:Installed]
Text=[&Func:trim('#CURRENTSECTION#', '.Name.String', '')]
FontColor=#Set.Text_Color#

[ListItem.Button.String:S]
X=((#Set.W#-#HomePage.P#*3)/2-#Page.Items.Content.P#*3-(#Page.Items.H#-#Page.Items.Content.P#*2))r
Y=r
Text=+
FontSize=(20*[set.S])
FontColor=#Set.Subtext_Color#
StringAlign=RightCenter
DynamicVariables=1
Group=List
Hidden=1
[ListItem.Button.String:Installed]
Text=>



; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header.String]
Text=Library

[Header.Button.Icon.String]
Meter=String
MeterStyle=Set.String:S
FontFace=Material Icons Round
X=(#Set.W#-#HomePage.P#)
Y=r
Text=[\xe923]
InlineSetting=GradientColor | 45 | #SEt.Accent_Color_2# ; 1 | #Set.Accent_color# ;0
StringAlign=RightCenter
FontSize=(32*[Set.S])
LeftMouseUpAction=[!CommandMeasure Func "interactionBox('GlobalVersionUpdater')"]
[Header.Button.Text.String]
Meter=String
X=(-[Header.Button.Icon.String:W]-10*[Set.S])r
Y=r
Text=Check & install available updates
FontColor=#Set.Subtext_Color#
StringAlign=RightCenter
MeterStyle=Set.String:S

; -------------------------------- Module list ------------------------------- ;

[ModuleList.Header.String]
Meter=String
Text=Modules
X=(#HomePage.P#)
Y=(110*[Set.S]+#Scroll#)
MeterStyle=Set.String:S | List.Header.String:S
[ModuleList.Header.Shape]
Meter=Shape
MeterStyle=List.Header.Shape:S

[YourFlyouts.Shape]
Meter=Shape
X=r
Y=(#Page.Items.P#)R
MEterStyle=ListItem.Shape:S
[YourFlyouts.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[YourFlyouts.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[YourFlyouts.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[YourMixer.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[YourMixer.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[YourMixer.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[YourMixer.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[ValliStart.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[ValliStart.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[ValliStart.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[ValliStart.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[IdleStyle.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[IdleStyle.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[IdleStyle.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[IdleStyle.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Keylaunch.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Keylaunch.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Keylaunch.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Keylaunch.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[QuickNote.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[QuickNote.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[QuickNote.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[QuickNote.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Keystrokes.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Keystrokes.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Keystrokes.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Keystrokes.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

; -------------------------------- Widget list ------------------------------- ;

[WidgetList.Header.String]
Meter=String
Text=Widgets
X=(#Set.W#/2+#HomePage.P#/2)
Y=(110*[Set.S]+#Scroll#)
MeterStyle=Set.String:S | List.Header.String:S
[WidgetList.Header.Shape]
Meter=Shape
MeterStyle=List.Header.Shape:S

[Overlayer.Shape]
Meter=Shape
X=r
Y=(#Page.Items.P#)R
MEterStyle=ListItem.Shape:S
[Overlayer.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Overlayer.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Overlayer.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[ModularVisualizer.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[ModularVisualizer.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[ModularVisualizer.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[ModularVisualizer.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[ModularClocks.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[ModularClocks.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[ModularClocks.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[ModularClocks.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[ModularPlayers.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[ModularPlayers.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[ModularPlayers.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[ModularPlayers.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Plainext.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Plainext.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Plainext.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Plainext.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[GoTo.Shape]
Meter=Shape
LeftMouseUpAction=[!CommandMeasure Func "interactionBox('GoToPage', 'Enter module or widget name here')"]
MEterStyle=ListItem.Shape:S
[GoTo.Image]
Meter=Image
MeterStyle=ListItem.Image:S | ListItem.Image:Installed
[GoTo.Name.String]
Meter=String
Text=Go to settings of widget or module...
MeterStyle=Set.String:S | ListItem.Name.String:S | ListItem.Name.String:Installed
[GoTo.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S | ListItem.Button.String:Installed

[AnchorPuller]
MEter=String
DynamicVariables=1
Y=[Keystrokes.Shape:YH]
Container=BC
