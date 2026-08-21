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

[Position]
Meter=String
Text=Position
MeterStyle=Set.String:S | Set.OptionCat:S
[Item01.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue variables SmartOverlayDetection "0" "#Sec.SaveLocation#"][!UpdateMeasure Auto_Refresh:M][!Refresh]
MeterStyle=Item.Shape:S
[Item01.StringIcon]
Meter=String
Text=[\xec7a]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item01.String]
Meter=String
Text=If you are experiencing issues with screens not covered, click here.
MeterStyle=Set.String:S | Item.String:S

[Set.Div:123123]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Screen to display Slate on
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:1233]
Meter=Shape
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text=Manually edit extender's position
FontColor=#Set.Text_Color#,(255-105*#SmartOverlayDetection#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:23]
Meter=Shape
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text=Smart detection
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:1233123]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Number of screens to stretch horizontally
FontColor=#Set.Text_Color#,(150+105*#SmartOverlayDetection#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:1231123]
Meter=Shape
MeterStyle=Set.Div:S
[Option14]
Meter=String
Text=Alignment of screensaver
FontColor=#Set.Text_Color#,(150+105*#SmartOverlayDetection#)
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[SmartOverlayDetection]
Meter=Shape
MeterStyle=Set.Bool:S
ToolTipTitle=Smart detection
ToolTipText=Automatically detects the monitors that has to be covered.
Y=([Option11:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Location]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
Type=Int|1|10
ToolTipTitle=Screen to display on
ToolTipText=The index of the screen to display the main section of Slate
[Value12]
Meter=String
Text=#Location#
MeterStyle=Set.String:S | Set.Value:S

[Stretch]
Meter=Shape
MeterStyle=Set.Textbox:S
ToolTipTitle=Number of screens to stretch
ToolTipText=The number of screens that you want Slate to cover. If you only want it to cover your primary monitor, set this value to 0.
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
Type=Int|0|10
Hidden=(1-#SmartOverlayDetection#)
[Value13]
Meter=String
Text=#Stretch#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(1-#SmartOverlayDetection#)

[Align:]
Meter=Shape
Shape2=Rectangle ([&Func:returnBool('Align', '-1')] = 0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables Align "1" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Align "-1" "#Sec.SaveLocation#"][!WriteKeyValue Variables Location "(#Location# = 1 ? 2 : #Location#)" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick:S
ToolTipTitle=Screen to display on
ToolTipText=Direction of the screens from the main sections' position
Hidden=(1-#SmartOverlayDetection#)
Y=([Option14:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Align:0]
Meter=String
Text=Left
FontColor=([&Func:returnBool('Align', '-1')] = 0 ? 0 : 255),([&Func:returnBool('Align', '-1')] = 0 ? 0 : 255),([&Func:returnBool('Align', '-1')] = 0 ? 0 : 255)
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
Hidden=(1-#SmartOverlayDetection#)
[Align:1]
Meter=String
Text=Right
FontColor=([&Func:returnBool('Align', '-1')] = 1 ? 0 : 255),([&Func:returnBool('Align', '-1')] = 1 ? 0 : 255),([&Func:returnBool('Align', '-1')] = 1 ? 0 : 255)
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S
Hidden=(1-#SmartOverlayDetection#)

[Button15]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option15:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_color#,150
LeaveColor=#Set.Accent_color#,50
Act=[!ActivateConfig "Slate\Launch\Addons\ExtenderEditor"]
Hidden=(#SmartOverlayDetection#)
[Value15]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=(14*[Set.S])
MeterStyle=Set.String:S | Set.Value:S
Hidden=(#SmartOverlayDetection#)
[Value151]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=Open editor
MeterStyle=Set.String:S | Set.Value:S
Hidden=(#SmartOverlayDetection#)
