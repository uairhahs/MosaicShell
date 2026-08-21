﻿[Variables]
@includeModulevars=#SKINSPATH##Skin.Name#\Main\Modules\Vars\#Page.SubpageModule#.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\Main\Modules\Vars\#Page.SubpageModule#.inc

[Item1.Shape]
Meter=
[Item1.StringIcon]
Meter=
[Item1.String]
Meter=

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysTitle]
Meter=String
Text=#Page.SubpageModule# settings
MeterStyle=Set.String:S | Set.OptionCat:S


[Option10]
Meter=String
Text=Time format
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:2238]
Meter=Shape
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text=Show seconds
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:23123]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Custom format
FontColor=#Set.Text_Color#,(105+150*#CustomFormat#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:323123]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Show start orb icon
MeterStyle=Set.String:S | Set.OptionName:S
[Divider14]
Meter=Shape
MeterStyle=Set.Div:S
[Option14]
Meter=String
Text=Start orb icon image
FontColor=#Set.Text_Color#,(#StartIconBool# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider15]
Meter=Shape
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text=Show start text
MeterStyle=Set.String:S | Set.OptionName:S
[Divider16]
Meter=Shape
MeterStyle=Set.Div:S
[Option16]
Meter=String
Text=Start text
FontColor=#Set.Text_Color#,(#StartTextBool# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[CustomFormat:]
Meter=Shape
Shape2=Rectangle ([&Func:returnBool('CustomFormat', '1')] = 0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables CustomFormat "0" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables CustomFormat "1" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick:S
Y=([Option10:Y]-#Set.P#+(-30/2+8)*[Set.S])
[CustomFormat:0]
Meter=String
Text=Time
FontColor=[&Func:returnBool('CustomFormat', '1', '#Set.Text_Color#', '#Set.Pri_color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
[CustomFormat:1]
Meter=String
Text=Custom
FontColor=[&Func:returnBool('CustomFormat', '1', '#Set.Pri_color#', '#Set.Text_Color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S
[ShowSec]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option11:Y]-#Set.P#+(-20/2+8)*[Set.S])
[CustomTimeFormat]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Txt
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(1-#CustomFOrmat#)
[Value12]
Meter=String
Text=#CustomTimeFormat#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(1-#CustomFOrmat#)

[StartIconBool]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option13:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Button14]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option14:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_Color#,150
LeaveColor=#Set.Accent_Color#,50
Act=[!CommandMEasure Func "interactionBox([[#SKINSPATH##Skin.Name#\Core\InteractionBox\ValliOrb.inc]], '#IconName#')"]
Hidden=(1-#StartIconBool#)
[Icon14]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=((14*[Set.S])*#FontSizeModifier#)
Hidden=(1-#StartIconBool#)
MeterStyle=Set.String:S | Set.Value:S
[Value14]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=#IconName#
Hidden=(1-#StartIconBool#)
MeterStyle=Set.String:S | Set.Value:S
[StartTextBool]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option15:Y]-#Set.P#+(-20/2+8)*[Set.S])
[StartText]
Meter=Shape
MeterStyle=Set.Textbox:S
Hidden=(1-#StartTextBool#)
Y=([Option16:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value16]
Meter=String
Text=#StartText#
Hidden=(1-#StartTextBool#)
MeterStyle=Set.String:S | Set.Value:S
