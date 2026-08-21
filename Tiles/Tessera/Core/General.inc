; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysTitle]
Meter=String
Text=Activation
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Timeout duration
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:322]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Activate flyouts with a hotkey
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:22144]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Activation hotkey
FontColor=#Set.Text_Color#,(150+105*#OptionalKey#)
MeterStyle=Set.String:S | Set.OptionName:S


[HotkeysTitle3]
Meter=String
Text=Flyout components
MeterStyle=Set.String:S | Set.OptionCat:S

[Option31]
Meter=String
Text=Enable CapsLock / NumLock / ScrollLock Flyouts
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S132]
Meter=Shape
MeterStyle=Set.Div:S
[Option32]
Meter=String
Text=Enable Media Flyouts
MeterStyle=Set.String:S | Set.OptionName:S


[HotkeysTitle2]
Meter=String
Text=Compatability
MeterStyle=Set.String:S | Set.OptionCat:S

[Option21]
Meter=String
Text=Use legacy volume key hooks
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:3322]
Meter=Shape
MeterStyle=Set.Div:S
[Option22]
Meter=String
Text=Volume tick change by legacy key hook
Fontcolor=#Set.Text_Color#,(150+105*#LegacyVol#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:2322]
Meter=Shape
MeterStyle=Set.Div:S
[Option23]
Meter=String
Text=Use locks key hooks
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:222]
Meter=Shape
MeterStyle=Set.Div:S
[Option24]
Meter=String
Text=Use legacy force brightness changer (Non built-in keys)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S41]
Meter=Shape
Hidden=(1-#LegacyBright#)
MeterStyle=Set.Div:S
[Option42]
Meter=String
Text=Brightness flyouts mode
Hidden=(1-#LegacyBright#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S431]
Meter=Shape
Hidden=(1-#LegacyBright#)
MeterStyle=Set.Div:S
[Option43]
Meter=String
Text=Hotkey to increase brightness
Hidden=(1-#LegacyBright#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:2231213]
Meter=Shape
Hidden=(1-#LegacyBright#)
MeterStyle=Set.Div:S
[Option44]
Meter=String
Text=Hotkey to decrease brightness
Hidden=(1-#LegacyBright#)
MeterStyle=Set.String:S | Set.OptionName:S


; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;


[Timeout]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|1|20
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
Text=#Timeout#s
MeterStyle=Set.String:S | Set.Value:S
[OptionalKey]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option02:Y]-#Set.P#+(-20/2+8)*[Set.S])
; ------------------------------- Optional key ------------------------------- ;
[Button03]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey')"]
Hidden=(1-#OptionalKey#)
[Value03]
Meter=String
Text=#KeyInString#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(1-#OptionalKey#)

[Locks]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option31:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Media]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option32:Y]-#Set.P#+(-20/2+8)*[Set.S])

[LegacyVol]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option21:Y]-#Set.P#+(-20/2+8)*[Set.S])
[LegacyVolChange]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|1|20
Hidden=(1-#LegacyVol#)
Y=([Option22:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value22]
Meter=String
Text=#LegacyVolChange#
Hidden=(1-#LegacyVol#)
MeterStyle=Set.String:S | Set.Value:S
[OverrideLocks]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option23:Y]-#Set.P#+(-20/2+8)*[Set.S])
[LegacyBright]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option24:Y]-#Set.P#+(-20/2+8)*[Set.S])
[BrightnessMode:]
Meter=Shape
Shape2=Rectangle ([&Func:returnBool('BrightnessMode', '0')]=0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables BrightnessMode "-1" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables BrightnessMode "0" "#Sec.SaveLocation#]
MeterStyle=Set.Pick:S
Y=([Option42:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(1-#LegacyBright#)
[BrightnessMode:0]
Meter=String
Text=Device
FontColor=[&Func:returnBool('BrightnessMode', '0', '#Set.Text_Color#', '#Set.Pri_color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
Hidden=(1-#LegacyBright#)
[BrightnessMode:1]
Meter=String
Text=Backlight
FontColor=[&Func:returnBool('BrightnessMode', '0', '#Set.Pri_color#', '#Set.Text_Color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S
Hidden=(1-#LegacyBright#)
; --------------------------------- bright up -------------------------------- ;
[Button43]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option43:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey', '3')"]
Hidden=(1-#LegacyBright#)
[Value43]
Meter=String
Text=#Key3InString#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(1-#LegacyBright#)
; -------------------------------- bright down ------------------------------- ;
[Button44]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option44:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey', '2')"]
Hidden=(1-#LegacyBright#)
[Value44]
Meter=String
Text=#Key2InString#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(1-#LegacyBright#)

@includeKeys=#SKINSPATH##Skin.Name#\@Resources\Actions\HotKeys.ini
