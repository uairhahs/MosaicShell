[Option015]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S


[Option06]
Meter=String
Text=Rotation
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19w20dsdaw328]
Meter=Shape
MeterStyle=Set.Div:S
[Option07]
Meter=String
Text=Stroke width
MeterStyle=Set.String:S | Set.OptionName:S
[w328]
Meter=Shape
MeterStyle=Set.Div:S
[Option08]
Meter=String
Text=Color Render: Default font color
MeterStyle=Set.String:S | Set.OptionName:S
[w321238]
Meter=Shape
MeterStyle=Set.Div:S
[Option09]
Meter=String
Text=Color Render: Overlapped font color
MeterStyle=Set.String:S | Set.OptionName:S
[w311541528]
Meter=Shape
MeterStyle=Set.Div:S
[Option10]
Meter=String
Text=Color Render: Background color
MeterStyle=Set.String:S | Set.OptionName:S
; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
[FontFace]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option015:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value015]
Meter=String
Text=#FontFace#
MeterStyle=Set.String:S | Set.Value:S

[Button06]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option06:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\Rotation.inc]], '#CURRENTSECTION#', '#skin.Name#', 'Rotation')"]
[Value06]
Meter=String
Text=#Rotation#
MeterStyle=Set.String:S | Set.Value:S

[Stroke]
Meter=Shape
MeterStyle=Set.TextBox:S
Type=Int|0|5
Y=([Option07:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value07]
Meter=String
Text=#Stroke#
MeterStyle=Set.String:S | Set.Value:S

[FontDefault]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option08:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value08]
Meter=String
Text=#FontDefaultOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

[FontOver]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option09:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value09]
Meter=String
Text=#FontOverOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

[Background]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option10:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value10]
Meter=String
Text=#BackgroundOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
