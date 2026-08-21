; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Option01]
Meter=String
Text=Mixer Width
MeterStyle=Set.String:S | Set.OptionName:S
[Divider01]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Application name width
MeterStyle=Set.String:S | Set.OptionName:S
[Divider02]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Font face
MeterStyle=Set.String:S | Set.OptionName:S
[Divider03]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=Background style
MeterStyle=Set.String:S | Set.OptionName:S
[Divider04]
Meter=Shape
MeterStyle=Set.Div:S
[Option05]
Meter=String
Text=Show divider line between volume and apps
MeterStyle=Set.String:S | Set.OptionName:S

[Header1]
Meter=String
Text=Colors
MeterStyle=Set.String:S | Set.OptionCat:S

[Option11]
Meter=String
Text=Color theme
MeterStyle=Set.String:S | Set.OptionName:S
[Divider11]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Background opacity
MeterStyle=Set.String:S | Set.OptionName:S
[Divider12]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Accent color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider13]
Meter=Shape
MeterStyle=Set.Div:S
[Option14]
Meter=String
Text=Handle color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider14]
Meter=Shape
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text=When hovering over handle, change color to
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[Scale]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0.5|2
Y=([Option00:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value00]
Meter=String
Text=#Scale#x
MeterStyle=Set.String:S | Set.Value:S
[Width]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|#name_width#|#screenareawidth#
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
Text=#width# px
MeterStyle=Set.String:S | Set.Value:S
[name_width]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|100|#Width#
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#name_width# px
MeterStyle=Set.String:S | Set.Value:S
[font]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value03]
Meter=String
Text=#font#
MeterStyle=Set.String:S | Set.Value:S
[Button04]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\Fluent\background_style.inc]], '#CURRENTSECTION#', '#skin.Name#', 'color_theme')"]
[Value04]
Meter=String
Text=#background_style#
MeterStyle=Set.String:S | Set.Value:S
[Stroke]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option05:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Button11]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\Fluent\color_theme.inc]], '#CURRENTSECTION#', '#skin.Name#', 'color_theme')"]
[Value11]
Meter=String
Text=#color_theme#
MeterStyle=Set.String:S | Set.Value:S
[Alpha]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|1|255
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value12]
Meter=String
Text=#Alpha#
MeterStyle=Set.String:S | Set.Value:S
[Accent]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
[AccentNode]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option14:Y]-#Set.P#+(-30/2+8)*[Set.S])
[AccentNodeHover]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option15:Y]-#Set.P#+(-30/2+8)*[Set.S])
