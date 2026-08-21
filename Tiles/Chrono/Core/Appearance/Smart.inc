[Option02]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Align
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19s8]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=Draw shadow behind clock
MeterStyle=Set.String:S | Set.OptionName:S
[Divider11239s8]
Meter=Shape
MeterStyle=Set.Div:S
[Option05]
Meter=String
Text=Show time
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19203s8]
Meter=Shape
MeterStyle=Set.Div:S
[Option06]
Meter=String
Text=Show date
MeterStyle=Set.String:S | Set.OptionName:S
[Divider1920328]
Meter=Shape
MeterStyle=Set.Div:S
[Option07]
Meter=String
Text=Show current weather
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19w20328]
Meter=Shape
MeterStyle=Set.Div:S
[Option08]
Meter=String
Text=Date-Time scaling ratio
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19w203238]
Meter=Shape
MeterStyle=Set.Div:S
[Option09]
Meter=String
Text=Show leading zero
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19w238]
Meter=Shape
MeterStyle=Set.Div:S
[Option10]
Meter=String
Text=Show seconds
MeterStyle=Set.String:S | Set.OptionName:S

[Header1]
Meter=String
Text=Colors
MeterStyle=Set.String:S | Set.OptionCat:S

[Option11]
Meter=String
Text=Tint color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19132w20328]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Tint the icon's color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19132w28]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Shadow color
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;
[FontFace]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#FontFace#
MeterStyle=Set.String:S | Set.Value:S

[Align:]
Meter=Shape
0Act=[!WriteKeyValue Variables Align "Left" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Align "Center" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables Align "Right" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Align', 'Center')]+[&Func:returnBool('Align', 'Right')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Align:0]
Meter=String
Text=Left
FontColor=[&Func:returnBool('Align', 'Left', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_0:S
[Align:1]
Meter=String
Text=Center
FontColor=[&Func:returnBool('Align', 'Center', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_1:S
[Align:2]
Meter=String
Text=Right
FontColor=[&Func:returnBool('Align', 'Right', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_2:S

[Shadow]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option04:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Time]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option05:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Date]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option06:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Weather]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option07:Y]-#Set.P#+(-20/2+8)*[Set.S])

[DateTimeRatio]
Meter=Shape
MeterStyle=Set.TextBox:S
Type=Num|0.1|2
Y=([Option08:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value08]
Meter=String
Text=#DateTimeRatio#
MeterStyle=Set.String:S | Set.Value:S

[ShowLeadingZero]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option09:Y]-#Set.P#+(-20/2+8)*[Set.S])

[ShowSeconds]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option10:Y]-#Set.P#+(-20/2+8)*[Set.S])

[TintColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])

[TintIcon]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option12:Y]-#Set.P#+(-20/2+8)*[Set.S])

[ShadowColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
