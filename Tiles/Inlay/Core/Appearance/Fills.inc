﻿
; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
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

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header14]
Meter=String
Text=Background blending
MeterStyle=Set.String:S | Set.OptionCat:S
[Option15]
Meter=String
Text=Use an image as the background

MeterStyle=Set.String:S | Set.OptionName:S
[Divider16]
Meter=Shape
MeterStyle=Set.Div:S
[Option16]
Meter=String
Text=Background image path
FontColor=#Set.Text_Color#,(#ImageUnderlay# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider17]
Meter=Shape
MeterStyle=Set.Div:S
[Option17]
Meter=String
Text=Background image opacity
FontColor=#Set.Text_Color#,(#ImageUnderlay# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Header18]
Meter=String
Text=Fill & Color
MeterStyle=Set.String:S | Set.OptionCat:S


[Item1.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xf1ad]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Background blur's rounded corner is only supported for Windows 11
MeterStyle=Set.String:S | Item.String:S

[Item2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables "Skin.Set_Page" "ColorScheme" "#@#SecVar.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xe790]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Text=Choose a pre-made color theme here!
MeterStyle=Set.String:S | Item.String:S
[Item2.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Divider19]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option19]
Meter=String
Text=Blur type
MeterStyle=Set.String:S | Set.OptionName:S
[Divider20]
Meter=Shape
MeterStyle=Set.Div:S
[Option20]
Meter=String
Text=Blur rounding size

MeterStyle=Set.String:S | Set.OptionName:S
[Divider21]
Meter=Shape
MeterStyle=Set.Div:S
[Option21]
Meter=String
Text=Start menu opacity

MeterStyle=Set.String:S | Set.OptionName:S
[Divider22]
Meter=Shape
MeterStyle=Set.Div:S
[Option22]
Meter=String
Text=Accent color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider23]
Meter=Shape
MeterStyle=Set.Div:S
[Option23]
Meter=String
Text=Primary color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider24]
Meter=Shape
MeterStyle=Set.Div:S
[Option24]
Meter=String
Text=Secondary color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider25]
Meter=Shape
MeterStyle=Set.Div:S
[Option25]
Meter=String
Text=Tertiary color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider26]
Meter=Shape
MeterStyle=Set.Div:S
[Option26]
Meter=String
Text=Font color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider27]
Meter=Shape
MeterStyle=Set.Div:S
[Option27]
Meter=String
Text=Darker color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider28]
Meter=Shape
MeterStyle=Set.Div:S
[Option28]
Meter=String
Text=Element stroke / Window border / divider color

MeterStyle=Set.String:S | Set.OptionName:S
[Header29]
Meter=String
Text=Legacy module colors
MeterStyle=Set.String:S | Set.OptionCat:S
[Option30]
Meter=String
Text=Box border color

MeterStyle=Set.String:S | Set.OptionName:S
[Divider31]
Meter=Shape
MeterStyle=Set.Div:S
[Option31]
Meter=String
Text=Box color

MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;
[ImageUnderlay]
Meter=Shape
MeterStyle=Set.Bool:S


Y=([Option15:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Button16]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option16:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_Color#,150
LeaveColor=#Set.Accent_Color#,50
Act=[!CommandMEasure Func "interactionBox([[#SKINSPATH##Skin.Name#\Core\InteractionBox\ValliImageUnderlay.inc]])"]
Hidden=(1-#ImageUnderlay#)
[Icon16]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=(14*[Set.S])
Hidden=(1-#ImageUnderlay#)
MeterStyle=Set.String:S | Set.Value:S
[Value16]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=[&Func:trim('#ImageUnderlayPath#','^.*\','')]
Hidden=(1-#ImageUnderlay#)
MeterStyle=Set.String:S | Set.Value:S
[ImageUnderlayAlpha]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|1|255
Hidden=(1-#ImageUnderlay#)

Y=([Option17:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value17]
Meter=String
Text=#ImageUnderlayAlpha#
Hidden=(1-#ImageUnderlay#)
MeterStyle=Set.String:S | Set.Value:S
[Blur:]
Meter=Shape
0Act=[!WriteKeyValue Variables Blur "None" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Blur "Acrylic" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables Blur "Blur" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Blur', 'Acrylic')]+[&Func:returnBool('Blur', 'Blur')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option19:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Blur:0]
Meter=String
Text=None
FontColor=[&Func:returnBool('Blur', 'None', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_0:S

[Blur:1]
Meter=String
Text=Acrylic
FontColor=[&Func:returnBool('Blur', 'Acrylic', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_1:S

[Blur:2]
Meter=String
Text=Standard
FontColor=[&Func:returnBool('Blur', 'Blur', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_2:S
Hidden=

[BlurCorner:]
Meter=Shape
0Act=[!WriteKeyValue Variables BlurCorner "None" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables BlurCorner "RoundSmall" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables BlurCorner "Round" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('BlurCorner', 'RoundSmall')]+[&Func:returnBool('BlurCorner', 'Round')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option20:Y]-#Set.P#+(-30/2+8)*[Set.S])

[BlurCorner:0]
Meter=String
Text=None
FontColor=[&Func:returnBool('BlurCorner', 'None', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_0:S

[BlurCorner:1]
Meter=String
Text=Small
FontColor=[&Func:returnBool('BlurCorner', 'RoundSmall', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_1:S

[BlurCorner:2]
Meter=String
Text=Standard
FontColor=[&Func:returnBool('BlurCorner', 'Round', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_2:S
Hidden=

[Alpha]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|0|255


Y=([Option21:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value21]
Meter=String
Text=#Alpha#

MeterStyle=Set.String:S | Set.Value:S
[Accent_Color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option22:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Pri_Color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option23:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Sec_Color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option24:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Ter_Color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option25:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Font_Color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option26:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Dark_Color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option27:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Border]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option28:Y]-#Set.P#+(-30/2+8)*[Set.S])

[BoxBorder]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option30:Y]-#Set.P#+(-30/2+8)*[Set.S])

[BoxColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option31:Y]-#Set.P#+(-30/2+8)*[Set.S])

[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
