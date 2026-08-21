﻿; ---------------------------------------------------------------------------- ;
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

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysT2itle]
Meter=String
Text=Customization
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Blur
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S1913s138]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Background color
MeterStyle=Set.String:S | Set.OptionName:S

[JDWS]
Meter=String
Text=Jelle's weather skin's configuration
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

[Set.Div:14]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text=Flip details layout
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:15]
Meter=Shape
MeterStyle=Set.Div:S
[Option16]
Meter=String
Text=Icons style
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:16]
Meter=Shape
MeterStyle=Set.Div:S
[Option17]
Meter=String
Text=Skin aligment
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;


[Blur:]
Meter=Shape
0Act=[!WriteKeyValue Variables Blur "None" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Blur "Acrylic" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables Blur "Blur" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Blur', 'Acrylic')]+[&Func:returnBool('Blur', 'Blur')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Blur:0]
Meter=String
Text=None
FontColor=([&Func:returnBool('Blur', 'None')] = 1 ? 0 : 255),([&Func:returnBool('Blur', 'None')] = 1 ? 0 : 255),([&Func:returnBool('Blur', 'None')] = 1 ? 0 : 255)
MeterStyle=Set.String:S | Set.PickOption3_0:S
[Blur:1]
Meter=String
Text=Acrylic
FontColor=([&Func:returnBool('Blur', 'Acrylic')] = 1 ? 0 : 255),([&Func:returnBool('Blur', 'Acrylic')] = 1 ? 0 : 255),([&Func:returnBool('Blur', 'Acrylic')] = 1 ? 0 : 255)
MeterStyle=Set.String:S | Set.PickOption3_1:S
[Blur:2]
Meter=String
Text=Standard
FontColor=([&Func:returnBool('Blur', 'Blur')] = 1 ? 0 : 255),([&Func:returnBool('Blur', 'Blur')] = 1 ? 0 : 255),([&Func:returnBool('Blur', 'Blur')] = 1 ? 0 : 255)
MeterStyle=Set.String:S | Set.PickOption3_2:S

[Background]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value21]
Meter=String
Text=#BackgroundOpacity#
MeterStyle=Set.String:S | Set.Pacity:S


[Info:JDWS]
Meter=Shape
MeterStyle=Set.RectButton:S
Y=([JDWS:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Info02]
Meter=String
MeterStyle=Set.String:S | Set.Icon:S

[DetailsFlip]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option15:Y]-#Set.P#+(-20/2+8)*[Set.S])

[IconStyle:]
Meter=Shape
Shape2=Rectangle ([&Func:returnBool('IconStyle', 'Color')] = 0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables IconStyle "Mono" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables IconStyle "Color" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick:S
Y=([Option16:Y]-#Set.P#+(-30/2+8)*[Set.S])
[IconStyle:0]
Meter=String
Text=Mono
FontColor=([&Func:returnBool('IconStyle', 'Color')] = 0 ? 0 : 255),([&Func:returnBool('IconStyle', 'Color')] = 0 ? 0 : 255),([&Func:returnBool('IconStyle', 'Color')] = 0 ? 0 : 255)
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
[IconStyle:1]
Meter=String
Text=Color
FontColor=([&Func:returnBool('IconStyle', 'Color')] = 1 ? 0 : 255),([&Func:returnBool('IconStyle', 'Color')] = 1 ? 0 : 255),([&Func:returnBool('IconStyle', 'Color')] = 1 ? 0 : 255)
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S

[Alignment:]
Meter=Shape
0Act=[!WriteKeyValue Variables Alignment "Left" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Alignment "Center" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables Alignment "Right" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Alignment', 'Center')]+[&Func:returnBool('Alignment', 'Right')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option17:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Alignment:0]
Meter=String
Text=Left
FontColor=([&Func:returnBool('Alignment', 'Left')] = 1 ? 0 : 255),([&Func:returnBool('Alignment', 'Left')] = 1 ? 0 : 255),([&Func:returnBool('Alignment', 'Left')] = 1 ? 0 : 255)
MeterStyle=Set.String:S | Set.PickOption3_0:S
[Alignment:1]
Meter=String
Text=Center
FontColor=([&Func:returnBool('Alignment', 'Center')] = 1 ? 0 : 255),([&Func:returnBool('Alignment', 'Center')] = 1 ? 0 : 255),([&Func:returnBool('Alignment', 'Center')] = 1 ? 0 : 255)
MeterStyle=Set.String:S | Set.PickOption3_1:S
[Alignment:2]
Meter=String
Text=Right
FontColor=([&Func:returnBool('Alignment', 'Right')] = 1 ? 0 : 255),([&Func:returnBool('Alignment', 'Right')] = 1 ? 0 : 255),([&Func:returnBool('Alignment', 'Right')] = 1 ? 0 : 255)
MeterStyle=Set.String:S | Set.PickOption3_2:S

