[Option01]
Meter=String
Text="Layout height"
MeterStyle=Set.String:S | Set.OptionName:S

[ElementTitle]
Meter=String
Text="Element settings"
MeterStyle=Set.String:S | Set.OptionCat:S
[Option21]
Meter=String
Text="Hide volume level"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:21]
Meter=Shape
MeterStyle=Set.Div:S
[Option22]
Meter=String
Text="Mixer icon action"
MeterStyle=Set.String:S | Set.OptionName:S

[ColorTitle]
Meter=String
Text="Color settings"
MeterStyle=Set.String:S | Set.OptionCat:S
[Option10]
Meter=String
Text="Select a color theme"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:10]
Meter=Shape
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text="Background color"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:11]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text="Behind-bar color"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:12]
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
Text="Font color (secondary)"
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:14]
Meter=Shape
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text="Accent color "
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

[Height]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|100|800
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
Text="#Height#px"
MeterStyle=Set.String:S | Set.Value:S

[HideVolLevel]
Meter=Shape
MeterStyle=Set.bool:S
Y=([Option21:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Button22]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option22:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\Pixel\BottomAction.inc]], '#CURRENTSECTION#', '#skin.Name#', 'color_theme')"]
[Value22]
Meter=String
Text=#BottomActionName#
MeterStyle=Set.String:S | Set.Value:S

[Button10]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option10:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\Pixel\color_theme.inc]], '#CURRENTSECTION#', '#skin.Name#', 'color_theme')"]
[Value10]
Meter=String
Text=#color_theme#
MeterStyle=Set.String:S | Set.Value:S
[Primary]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
Type=Num|1|255
[Value11]
Meter=String
Text=#PrimaryOpacity#
MeterStyle=Set.String:S | Set.Pacity:S
[SecondaryColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
[FontColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
[FontColorAccent]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option14:Y]-#Set.P#+(-30/2+8)*[Set.S])
[AccentColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option15:Y]-#Set.P#+(-30/2+8)*[Set.S])
; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
