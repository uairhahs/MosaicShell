﻿[Variables]
@includeSkinList=#ROOTCONFIGPATH#CoreWebResources.inc
Parsed=0

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[PSRM]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\GlobalStatusChecker.ps1
ExecutionPolicy=Unrestricted

[CoreInstallHandler]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Installer.ps1
ExecutionPolicy=Unrestricted

[AnchorChecker]
Measure=Calc
Formula=(Clamp((#H#-([Header.Description:Y]+50*[S])-#Sec.P#)-[#Sec.AnchorName#:Y], -5000, 0))

[MouseScrollBar]
Measure=Plugin
Plugin=Mouse
MouseMoveAction=[!SetVariable Scroll "(Clamp(($MouseY$-#ScrollClickedPos#-([Header.Description:Y]+50*[S]))/(#H#/2-([Header.Description:Y]+50*[S]))*[AnchorChecker],[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[List.Item.Shape:S]
X=(#Sec.P#)
Y=(40*[S])r
Shape=Rectangle 0,0,(#W#-#Sec.P#*2),(60*[S]),(#Set.R#/2) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Group=List
Container=ContentContainer
[List.Item.Title.String:S]
X=(#Sec.P#)r
Y=(60*[S]/2)r
StringAlign=leftCenter
Group=List
FontSize=(12*[S])
[List.Item.VersionDiff.String:S]
X=(#W#/2)
Y=r
StringAlign=CenterCenter
Group=List
Fontcolor=#Set.Subtext_Color#
FontSize=(12*[S])
InlinePattern=^(\d\.\d+) >> \d\.\d+
InlineSetting=Color | #set.Accent_color#
InlinePattern2=^\d\.\d+ (>> \d\.\d+)
InlineSetting2=Color | #set.Positive_color#
InlinePattern3=^\d\.\d+ 🛠️$
InlineSetting3=Color | #Set.Accent_Color_2#
InlinePattern4=^Not installed ❌$
InlineSetting4=Color | #Set.Subtext_Color#,150
[List.Item.Button_Eq.Shape:S]
X=(#W#-#Sec.P#*2-40*[S])
Y=(-20*[S])r
Shape=Rectangle 0,0,(40*[S]),(40*[S]),(#Set.R#/2) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Text_Color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Text_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Group=List
Container=ContentContainer
[List.Item.Button_Gt.Shape:S]
X=(#W#-#Sec.P#*2-40*[S])
Y=(-20*[S])r
Shape=Rectangle 0,0,(40*[S]),(40*[S]),(#Set.R#/2) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Positive_color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Group=List
Container=ContentContainer
[List.Item.Button.String:S]
X=(40*[S]/2)r
Y=(23*[S])r
StringAlign=CenterCenter
Group=List
FontFace=Material Icons Round
FontSize=(18*[S])

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;


[Header.Image]
Meter=Image
X=(#W#/2-100*[S]/2)
Y=((#B#/2)+30*[S]+#Sec.P#)
W=(100*[S])
H=(100*[S])
ImageName=#@#Images\Unified\MosaicShell.png
DynamicVariables=1
[Header.String]
Meter=String
X=(#W#/2)
Y=(#Sec.P#/2)R
StringAlign=Center
Text=MosaicShell Global Status Checker
MeterStyle=Sec.String:S | Header.String:0
FontSize=(14*[S])
FontWeight=650
Container=
[Header.Description]
Meter=String
X=(#W#/2)
Y=(#Sec.P#/2)R
Text=
StringAlign=Center
W=(#W#-#Sec.P#*2)
ClipString=2
FontColor=#Subtext_Color#
MeterStyle=Sec.String:S | Header.Description:0
Container=

; -------------------------- Includes generated page ------------------------- ;

[ContentContainer]
Meter=Shape
X=(#B#/2)
Y=([Header.Description:Y]+50*[S])
Shape=Rectangle 0,0, #W#, (#H#-[ContentContainer:Y]-#Sec.P#), #WR# | StrokeWidth #B#
DynamicVariables=1
MouseScrollDownAction=[!SetVariable Scroll "(Clamp(#Scroll#-50,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseScrollUpAction=[!SetVariable Scroll "(Clamp(#Scroll#+50,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseActionCursor=0
[ScrollBar]
X=([ContentContainer:XW]-10*[Set.S])
Y=([ContentContainer:Y]+(#Scroll#/([AnchorChecker]+1)*([ContentContainer:H]-([ContentContainer:H]/([ContentContainer:H]-[AnchorChecker])*[ContentContainer:H]))))

[List.Item0.Shape]
Y=(#Scroll#)
DynamicVariables=1

@includeList=#@#Includes\GlobalVersionList.inc
; ---------------------------- Page scroll anchor ---------------------------- ;

[Anchor]
Meter=String
Container=ContentContainer
x=r
Y=R
