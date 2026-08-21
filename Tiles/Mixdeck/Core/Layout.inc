; ---------------------------------------------------------------------------- ;
;                                    Styles                                    ;
; ---------------------------------------------------------------------------- ;

[Preset:M]
Measure=Plugin
Plugin=powershellrm
ScriptFile=#@#Powershell\PresetLoad.ps1

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
Leftmouseupaction=[!CommandMeasure Preset:M "ChangeToFiles -theme '#CURRENTSECTION#'"][!UpdateMeasure Auto_Refresh:M][!Refresh]
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
W=([ContentContainer:W]/2-20*[Set.S])
H=(([ContentContainer:W]/2-20*[Set.S])/1.625)
ImageName=#SKINSPATH##Skin.Name#\Core\Layout\#CURRENTSECTIOn#.png
PreserveAspectRatio=2
DynamicVariables=1
ImageAlpha=200
MouseOverAction=[!SetOption #CURRENTSECTION# ImageAlpha "255"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# ImageAlpha "200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!CommandMeasure Preset:M "ChangeToFiles -theme '#CURRENTSECTION#'"][!UpdateMeasure Auto_Refresh:M][!Refresh]
MaskImageName=#SKINSPATH##Skin.Name#\Core\Layout\Mask.png
Container=ContentContainer

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

[OtherTitle]
Meter=String
Text=Default bundled layouts
MeterStyle=Set.String:S | Set.OptionCat:S

[Fluent10]
Meter=Image
X=(20*[Set.S])
Y=(30*[Set.S])R
MeterStyle=Select:S

[Fluent11]
Meter=Image
MeterStyle=Select:S

[Rounded]
Meter=Image
X=(20*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Select:S

[Center]
Meter=Image
MeterStyle=Select:S
