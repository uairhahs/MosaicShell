[Option015]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=:00 representation
MeterStyle=Set.String:S | Set.OptionName:S
[Divider1d9wd]
Meter=Shape
MeterStyle=Set.Div:S

[Option08]
Meter=String
Text=Color Render: Primary color
MeterStyle=Set.String:S | Set.OptionName:S
[w321238]
Meter=Shape
MeterStyle=Set.Div:S
[Option09]
Meter=String
Text=Color Render: Secondary color
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

[ZeroText]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value04]
Meter=String
Text=#ZeroText#
MeterStyle=Set.String:S | Set.Value:S


[Primary]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option08:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value08]
Meter=String
Text=#PrimaryOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

[Secondary]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option09:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value09]
Meter=String
Text=#SecondaryOpacity#
MeterStyle=Set.String:S | Set.Pacity:S

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
