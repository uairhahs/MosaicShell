
; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;


[HotkeysTitle3]
Meter=String
Text=Flyout animation settings
MeterStyle=Set.String:S | Set.OptionCat:S

[Option31]
Meter=String
Text=Slide animations
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S132]
Meter=Shape
MeterStyle=Set.Div:S
[Option32]
Meter=String
Text=Animation easing type
FontColor=#Set.Text_Color#,(#Ani# = 0 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S1343]
Meter=Shape
MeterStyle=Set.Div:S
[Option33]
Meter=String
Text=Animation direction
FontColor=#Set.Text_Color#,(#Ani# = 0 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S21343]
Meter=Shape
MeterStyle=Set.Div:S
[Option34]
Meter=String
Text=Animation steps
FontColor=#Set.Text_Color#,(#Ani# = 0 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S221343]
Meter=Shape
MeterStyle=Set.Div:S
[Option35]
Meter=String
Text=Animation distance
FontColor=#Set.Text_Color#,(#Ani# = 0 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S


; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;


[Ani:]
Meter=Shape
0Act=[!WriteKeyValue Variables Ani "0" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables Ani "1" "#Sec.SaveLocation#"]
2Act=[!WriteKeyValue Variables Ani "2" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Ani', '1')]+[&Func:returnBool('Ani', '2')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option31:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Ani:0]
Meter=String
Text=None
FontColor=[&Func:returnBool('Ani', '0', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_0:S
[Ani:1]
Meter=String
Text=Fast
FontColor=[&Func:returnBool('Ani', '1', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_1:S
[Ani:2]
Meter=String
Text=Fancy
FontColor=[&Func:returnBool('Ani', '2', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_2:S
[Easetype]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option32:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Ease')"]
Hidden=(#Ani# = 0 ? 1 : 0)
[Value32]
Meter=String
Text=#Easetype#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(#Ani# = 0 ? 1 : 0)
[Button33]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option33:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop('AniDir', '#CURRENTSECTION#', 'MosaicShell', 'AniDir')"]
Hidden=(#Ani# = 0 ? 1 : 0)
[Value33]
Meter=String
Text=From #AniDir#
Hidden=(#Ani# = 0 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[AniSteps]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|10|40
Y=([Option34:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(#Ani# = 0 ? 1 : 0)
[Value34]
Meter=String
Text=#AniSteps#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(#Ani# = 0 ? 1 : 0)
[AnimationDisplacement]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|10|200
Y=([Option35:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(#Ani# = 0 ? 1 : 0)
[Value35]
Meter=String
Text=#AnimationDisplacement#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(#Ani# = 0 ? 1 : 0)
