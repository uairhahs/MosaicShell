; ---------------------------------------------------------------------------- ;
;                                    Styles                                    ;
; ---------------------------------------------------------------------------- ;

[ContentContainer]
MouseScrollDownAction=[!SetVariable Scroll "(Clamp(#Scroll#-200,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseScrollUpAction=[!SetVariable Scroll "(Clamp(#Scroll#+200,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]

[Slow_Refresh:M]
Measure=Calc
Formula=100
IfCondition=m.ConfigActive = 1
IfTrueAction=[!Deactivateconfig "#Skin.Name#\Main"][!Log de][!Delay 1000][!ActivateConfig "#Skin.Name#\Main"][!Log ac][!Refresh]
IfConditionMode=1
DynamicVariables=1
Group=NUOL
Disabled=1

[Variables]
page.page=1

[Preset:M]
Measure=Plugin
Plugin=powershellrm
ScriptFile=#@#Powershell\PresetLoad.ps1
OnUpdateAction=[!Delay 50][!SetOption "#Layout#Box" MeterStyle "SelectBox:S | SelectBox:Selected"][!UpdateMeter "#Layout#Box"][!ShowMeterGroup Select][!Redraw]

[DLC:S]
X=(20*[Set.S])
Y=(20*[Set.S])r
W=([ContentContainer:W]-40*[Set.S])
H=(([ContentContainer:W]/2-20*[Set.S])/2)
ImageName=#SKINSPATH##Skin.Name#\Core\Layout\#CURRENTSECTIOn#.png
PreserveAspectRatio=2
DynamicVariables=1
ImageAlpha=200
MouseOverAction=[!SetOption #CURRENTSECTION# ImageAlpha "255"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# ImageAlpha "200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!CommandMeasure Preset:M "ChangeToFiles -theme '#CURRENTSECTION#'"][!UpdateMeasure Slow_Refresh:M]
MaskImageName=#SKINSPATH##Skin.Name#\Core\Layout\MaskDLC.png
Container=ContentContainer

[DLC.String:S]
X=(20*[Set.S])r
Y=(-10*[Set.S])R
StringAlign=LeftBottom
FontSize=(32*[Set.S])
FontWeight=650
InlineSetting=Shadow | 0 | 0 | 5 | 0,0,0
Container=ContentContainer

[Select:S]
X=(20*[Set.S])R
Y=r
W=([ContentContainer:W]/2-22*[Set.S])
H=(([ContentContainer:W]/2-22*[Set.S])/1.625)
ImageName=#SKINSPATH##Skin.Name#\Core\Layout\#CURRENTSECTIOn#.png
PreserveAspectRatio=2
DynamicVariables=1
Leftmouseupaction=[!CommandMeasure Preset:M "ChangeToFiles -theme '#CURRENTSECTION#'"][!UpdateMeasure Slow_Refresh:M]
MaskImageName=#SKINSPATH##Skin.Name#\Core\Layout\Mask.png
Container=ContentContainer
Hidden=1
Group=Select
ToolTipTitle=Layout: #CURRENTSECTION#
ToolTipText=Click to select this layout
[SelectBox:S]
X=r
Y=r
Shape=Rectangle 0,0,([ContentContainer:W]/2-22*[Set.S]),(([ContentContainer:W]/2-22*[Set.S])/1.625),(8*[Set.S]) | StrokeWidth (2*[Set.S]) | Extend Stroke | Fill Color 0,0,0,0
Stroke=Stroke Color 0,0,0,0
CoreGrad=45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
Container=#sec.Container#
DynamicVariables=1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Stroke "Stroke Color #Set.Text_Color#"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Stroke "Stroke Color 0,0,0,0"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
[SelectBox:Selected]
Stroke=Stroke LinearGradient CoreGrad
MouseOverAction=
MouseLeaveAction=
Hidden=1
Group=Select

[Item.Shape:S]
X=(20*[Set.S])
Y=(35*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyvalue Variables page.page 2 "#SKINSPATH##Skin.Name#\Core\General.inc"][!Refresh]
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
FontSize=(10*[Set.S])
StringAlign=LeftCenter
Container=ContentContainer
[Item.Arrow.String:S]
X=([ContentContainer:W]-#Set.P#)
Y=r
StringAlign=RightCenter
Text=>
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysTitle]
Meter=String
Text=DLCs
MeterStyle=Set.String:S | Set.OptionCat:S

@includeIncluder=#SKINSPATH##Skin.Name#\Core\Layout\Includer.inc

@includePage=#SKINSPATH##Skin.Name#\Core\Layout\#page.page#.inc


[Anchor]
Meter=String

