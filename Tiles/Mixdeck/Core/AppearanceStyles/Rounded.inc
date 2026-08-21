; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Option01]
Meter=String
Text=Corner roundness
MeterStyle=Set.String:S | Set.OptionName:S
[Divider02]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Border size
MeterStyle=Set.String:S | Set.OptionName:S
[Divider03]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Font face
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
Text=Bar color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider12]
Meter=Shape
MeterStyle=Set.Div:S

[Header2]
Meter=String
Text=Custom colors
MeterStyle=Set.String:S | Set.OptionCat:S

[Option21]
Meter=String
Text=Background color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider21]
Meter=Shape
MeterStyle=Set.Div:S
[Option22]
Meter=String
Text=Foreground color
MeterStyle=Set.String:S | Set.OptionName:S
[Divider22]
Meter=Shape
MeterStyle=Set.Div:S
[Option23]
Meter=String
Text=Highlight / border color
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
[Corner]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|#name_width#|#screenareawidth#
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
Text=#Corner# px
MeterStyle=Set.String:S | Set.Value:S
[Stroke]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0|100
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#Stroke#
MeterStyle=Set.String:S | Set.Value:S
[font]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value03]
Meter=String
Text=#font#
MeterStyle=Set.String:S | Set.Value:S

[Button11]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\Rounded\color_theme.inc]], '#CURRENTSECTION#', '#skin.Name#', 'color_theme')"]
[Value11]
Meter=String
Text=#color_theme#
MeterStyle=Set.String:S | Set.Value:S
[bar_color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])

[background_color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option21:Y]-#Set.P#+(-30/2+8)*[Set.S])
[foreground_color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option22:Y]-#Set.P#+(-30/2+8)*[Set.S])
[foreground_hover_color]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option23:Y]-#Set.P#+(-30/2+8)*[Set.S])
