[Option015]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S

[Option06]
Meter=String
Text=Color Render: color
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

[Primary]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option06:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#PrimaryOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
