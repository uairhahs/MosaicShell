[Option02]
Meter=String
Text=Time font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Date font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider1sd9wd]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=:00 representation
MeterStyle=Set.String:S | Set.OptionName:S
[Divider1d9wd]
Meter=Shape
MeterStyle=Set.Div:S

[Option06]
Meter=String
Text=Color Render: color
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
[TimeFont]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#TimeFont#
MeterStyle=Set.String:S | Set.Value:S

[DateFont]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value03]
Meter=String
Text=#DateFont#
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
Y=([Option06:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value06]
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
