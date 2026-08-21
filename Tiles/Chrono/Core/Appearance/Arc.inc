[Option015]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S

[Option06]
Meter=String
Text=Color Render: Arc color
MeterStyle=Set.String:S | Set.OptionName:S

[Divider19wdaw328]
Meter=Shape
MeterStyle=Set.Div:S

[Option07]
Meter=String
Text=Color Render: Text color
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

[PrimaryColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option06:Y]-#Set.P#+(-30/2+8)*[Set.S])

[Secondarycolor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option07:Y]-#Set.P#+(-30/2+8)*[Set.S])

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
