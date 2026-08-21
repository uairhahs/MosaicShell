[Option02]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider3]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Draw shadow behind clock
MeterStyle=Set.String:S | Set.OptionName:S

[Header1]
Meter=String
Text=Colors
MeterStyle=Set.String:S | Set.OptionCat:S

[Option11]
Meter=String
Text=Font color and opacity
MeterStyle=Set.String:S | Set.OptionName:S
[Divider1]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Line color and opacity
MeterStyle=Set.String:S | Set.OptionName:S
[Divider2]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Shadow color
MeterStyle=Set.String:S | Set.OptionName:S
; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[FontFace]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#FontFace#
MeterStyle=Set.String:S | Set.Value:S

[Shadow]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option03:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Font]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value08]
Meter=String
Text=#FontOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

[Line]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value09]
Meter=String
Text=#LineOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

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
