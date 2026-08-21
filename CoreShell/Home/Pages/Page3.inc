[Variables]
Page.Items.P=(10*[Set.S])
Page.Items.H=(60*[Set.S])
Page.Items.Content.P=(10*[Set.S])
@includeSkinList=#ROOTCONFIGPATH#CoreWebResources.inc

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
W=((#Set.W#-#HomePage.P#*3)/2-#Page.Items.Content.P#*3-(#Page.Items.H#-#Page.Items.Content.P#*2)-(24*[Set.S]))
ClipString=1
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
X=(#Page.Items.Content.P#)R
Y=r
Text=+
FontSize=(20*[set.S])
FontColor=#Set.Subtext_Color#
StringAlign=LeftCenter
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
InlineSetting=GradientColor | 45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
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

[Tessera.Shape]
Meter=Shape
X=r
Y=(#Page.Items.P#)R
MEterStyle=ListItem.Shape:S
[Tessera.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Tessera.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Tessera.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Mixdeck.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Mixdeck.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Mixdeck.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Mixdeck.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Inlay.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Inlay.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Inlay.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Inlay.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Slate.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Slate.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Slate.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Slate.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Chord.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Chord.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Chord.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Chord.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Substrate.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Substrate.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Substrate.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Substrate.Button.String]
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

[Chrono.Shape]
Meter=Shape
X=r
Y=(#Page.Items.P#)R
MEterStyle=ListItem.Shape:S
[Chrono.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Chrono.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Chrono.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Phono.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Phono.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Phono.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Phono.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Pulse.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Pulse.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Pulse.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Pulse.Button.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Button.String:S

[Canvas.Shape]
Meter=Shape
MEterStyle=ListItem.Shape:S
[Canvas.Image]
Meter=Image
MeterStyle=ListItem.Image:S
[Canvas.Name.String]
Meter=String
MeterStyle=Set.String:S | ListItem.Name.String:S
[Canvas.Button.String]
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
Y=[Substrate.Shape:YH]
Container=BC
