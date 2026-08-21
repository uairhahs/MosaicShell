


[Option01]
Meter=String
Text="Width"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:01]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text="Font face"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:02]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text="Blur variant"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:03]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text="Rounding (Blur is not rounded on Windows 10)"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:04]
Meter=Shape
MeterStyle=Set.Div:S
[Option05]
Meter=String
Text="Show contrast border"
MeterStyle=Set.String:S | Set.OptionName:S

[ColorTitle]
Meter=String
Text="Color settings"
MeterStyle=Set.String:S | Set.OptionCat:S
[Option11]
Meter=String
Text="Background color"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:11]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text="Font color"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:13]
Meter=Shape
MeterStyle=Set.Div:S
[Option14]
Meter=String
Text="Behind-bar color"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:14]
Meter=Shape
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text="Use Windows accent color"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:15]
Meter=Shape
MeterStyle=Set.Div:S
[Option16]
Meter=String
Text="Accent color"
Fontcolor=[&Func:returnBool('SysAccent', '0', '#Set.Text_Color#', '#Set.Subtext_Color#')]
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;


[Scale]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0.5|2
Y=([Option00:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value00]
Meter=String
Text="#Scale#x"
MeterStyle=Set.String:S | Set.Value:S
[Width]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|200|600
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
Text="#Width#"
MeterStyle=Set.String:S | Set.Value:S
[Fontface]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Txt
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value03]
Meter=String
Text="#Fontface#"
MeterStyle=Set.String:S | Set.Value:S
[Blur:]
Meter=Shape
0Act=[!WriteKeyValue Variables Blur "None" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Blur "Acrylic" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables Blur "Blur" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Blur', 'Acrylic')]+[&Func:returnBool('Blur', 'Blur')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
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
[BlurCorner:]
Meter=Shape
0Act=[!WriteKeyValue Variables BlurCorner "None" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables BlurCorner "RoundSmall" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables BlurCorner "Round" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('BlurCorner', 'RoundSmall')]+[&Func:returnBool('BlurCorner', 'Round')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
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
[Border]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option05:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Primary]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
Type=Num|1|255
[Value11]
Meter=String
Text=#PrimaryOpacity#
MeterStyle=Set.String:S | Set.Pacity:S
[FontColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
[FontColor2]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option14:Y]-#Set.P#+(-30/2+8)*[Set.S])
[SysAccent]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option15:Y]-#Set.P#+(-20/2+8)*[Set.S])
[AccentColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option16:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(#SysAccent#)
; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
