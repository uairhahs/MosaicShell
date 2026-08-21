﻿

[m.StartAction]
Measure=String
OnUpdateAction=[!SetOption #Style#Box MeterStyle "Box:Selected"][!UpdateMeter #Style#Box][!UpdateMeterGroup Preload][!UpdateMeterGroup PreviewImages][!SHowMeterGroup Preload][!ShowMeterGroup PreviewImages]

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;


[Item.Shape:S]
X=(30*[Set.S])
Y=(35*[SEt.S])r
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
[Item.Arrow.String:S]
X=([ContentContainer:W]-#Set.P#)
Y=r
StringAlign=RightCenter
Text=>
Container=ContentContainer

[Box]
Shape=Rectangle  0,0,(([ContentContainer:W]-120*[Set.S])/5),(([ContentContainer:W]-120*[Set.S])/5),#set.R# | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Text_Color#,10
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Text_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Text_Color#,10"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!WriteKeyValue Variables Style "[&Func:trim('#CURRENTSECTION#', 'Box', '')]" "#Sec.SaveLocation#"][!Refresh][!UpdateMeasure Auto_Refresh:M]
DynamicVariables=1
Container=ContentContainer
Group=Preload
Hidden=1
[Box:Selected]
Shape=Rectangle  0,0,(([ContentContainer:W]-120*[Set.S])/5),(([ContentContainer:W]-120*[Set.S])/5),#set.R# | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Text_Color#,80
DynamicVariables=1
Container=ContentContainer
Group=Preload
Hidden=1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header00]
Meter=String
Text=Options
MeterStyle=Set.String:S | Set.OptionCat:S

[Item01.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables Skin.Name """#MosaicShell""" "#@#SecVar.inc"][!WriteKeyvalue Variables Skin.Set_Page GlobalVars "#@#SecVar.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item01.StringIcon]
Meter=String
Text=[\xe775]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item01.String]
Meter=String
Text=Change time format in MosaicShell global settings.
MeterStyle=Set.String:S | Item.String:S
[Item01.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Item02.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables Skin.Name """#MosaicShell""" "#@#SecVar.inc"][!WriteKeyvalue Variables Skin.Set_Page GlobalVars "#@#SecVar.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item02.StringIcon]
Meter=String
Text=[\xe774]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item02.String]
Meter=String
Text=Change weather location & units in MosaicShell global settings.
MeterStyle=Set.String:S | Item.String:S
[Item02.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Div0]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S

[Option01]
Meter=String
Text=Animated clocks update rate
MeterStyle=Set.String:S | Set.OptionName:S
[Div01]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Time format (en-US etc. "Local" to follow system)
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                  Style list                                  ;
; ---------------------------------------------------------------------------- ;

[Header1]
Meter=String
Text=Clock style // #Style#
MeterStyle=Set.String:S | Set.OptionCat:S

[Item11.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item11.StringIcon]
Meter=String
Text=[\xe71c]
fontFace=material icons round
fontsize=(20*[Set.S])
MeterStyle=Set.String:S | Item.StringIcon:S
[Item11.String]
Meter=String
Text=Styles with this icon have animations
MeterStyle=Set.String:S | Item.String:S

[Item12.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item12.StringIcon]
Meter=String
Text=[\xe3a2]
fontFace=material icons round
fontsize=(20*[Set.S])
MeterStyle=Set.String:S | Item.StringIcon:S
[Item12.String]
Meter=String
Text=Styles with this icon have render options
MeterStyle=Set.String:S | Item.String:S

[CenterBox]
Meter=Shape
X=(30*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Box
[MeasureBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box
[GraphBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box
[SmartBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box
[TextBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box

[CircTechBox]
Meter=Shape
X=(30*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Box
[TechBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box
[ArcBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box
[LightBox]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box
[16Box]
Meter=Shape
X=(20*[Set.S])R
Y=r
MeterStyle=Box

[Center]
Meter=Image
Imagename=#SKINSPATH#Chrono\@Resources\Images\Preview\#CURRENTSECTION#Preview.png
X=([#CURRENTSECTION#Box:X]-[ContentContainer:X])
Y=([#CURRENTSECTION#Box:Y]-[ContentContainer:Y])
W=(([ContentContainer:W]-120*[Set.S])/5)
H=(([ContentContainer:W]-120*[Set.S])/5)
Solidcolor=0,0,0,1
DynamicVariables=1
Group=PreviewImages
Container=ContentContainer
Hidden=1

[Measure]
Meter=Image
MeterStyle=Center

[Graph]
Meter=Image
MeterStyle=Center

[Smart]
Meter=Image
MeterStyle=Center

[Text]
Meter=Image
MeterStyle=Center

[CircTech]
Meter=Image
Imagename=#SKINSPATH#Chrono\@Resources\Images\Preview\#CURRENTSECTION#Preview.png
X=([#CURRENTSECTION#Box:X]-[ContentContainer:X])
Y=([#CURRENTSECTION#Box:Y]-[ContentContainer:Y])
W=(([ContentContainer:W]-120*[Set.S])/5)
H=(([ContentContainer:W]-120*[Set.S])/5)
Solidcolor=0,0,0,1
DynamicVariables=1
Group=PreviewImages
Container=ContentContainer
Hidden=1

[Tech]
Meter=Image
MeterStyle=CircTech

[Arc]
Meter=Image
MeterStyle=CircTech

[Light]
Meter=Image
MeterStyle=CircTech

[16]
Meter=Image
MeterStyle=CircTech

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[Header2]
Meter=String
Text=Customization
MeterStyle=Set.String:S | Set.OptionCat:S

[Item21.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables "Skin.Set_Page" "Appearance" "#@#SecVar.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item21.StringIcon]
Meter=String
Text=[\xe790]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item21.String]
Meter=String
Text=Customize clock in style options
MeterStyle=Set.String:S | Item.String:S
[Item21.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Rm.Update]
Meter=Shape
MeterStyle=Set.TextBox:S
Type=Int|0|50
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value000]
Meter=String
Text=#Rm.update#
MeterStyle=Set.String:S | Set.Value:S

[FormatLocale]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#FormatLocale#
MeterStyle=Set.String:S | Set.Value:S

[Page.Anchor]
Meter=String
Container=#Sec.Container#
Y=[Item21.Arrow.String:Y]
DynamicVariables=1
