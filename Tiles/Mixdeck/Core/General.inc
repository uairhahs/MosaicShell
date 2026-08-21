﻿[Variables]
@includeHotkeys=#SKINSPATH##Skin.Name#\@Resources\Actions\HotKeys.ini
@include19Custom=#SKINSPATH##Skin.Name#\Main\Elements\Unload\Vars.inc

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; --------------------------------- Activate --------------------------------- ;
[Header0]
Meter=String
Text=Activation
MeterStyle=Set.String:S | Set.OptionCat:S
[Option01]
Meter=String
Text=Use hotkey to activate Mixdeck
FontColor=#Set.Text_Color#,(#StayOnDesktop# = 0 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider01]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Activation hotkey
FontColor=#Set.Text_Color#,(#Hotkey# = 0 || #StayOnDesktop# = 1 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider02]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Use module as widget
MeterStyle=Set.String:S | Set.OptionName:S

; ------------------------------------ App ----------------------------------- ;
[Header1]
Meter=String
Text=Applications and operation
MeterStyle=Set.String:S | Set.OptionCat:S
[Option11]
Meter=String
Text=When app has multiple volume channels
MeterStyle=Set.String:S | Set.OptionName:S
[Divider11]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Excluded applications
MeterStyle=Set.String:S | Set.OptionName:S
[Divider12]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Level to change when scrolling
MeterStyle=Set.String:S | Set.OptionName:S

; ----------------------------------- Tray ----------------------------------- ;
[Header2]
Meter=String
Text=System tray icon
MeterStyle=Set.String:S | Set.OptionCat:S
[Option21]
Meter=String
Text=Create system volume tray icon
FontColor=#Set.Text_Color#,(#StayOnDesktop# = 0 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider21]
Meter=Shape
MeterStyle=Set.Div:S
[Option22]
Meter=String
Text=Scroll icon to change volume (creates mouse hook)
FontColor=#Set.Text_Color#,(#Tray# = 0 || #StayOnDesktop# = 1 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider22]
Meter=Shape
MeterStyle=Set.Div:S
[Option23]
Meter=String
Text=Volume tray icon theme
FontColor=#Set.Text_Color#,(#Tray# = 0 || #StayOnDesktop# = 1 ? 150 : 255)
MeterStyle=Set.String:S | Set.OptionName:S

; --------------------------------- Animation -------------------------------- ;
[Header3]
Meter=String
Text=Animation
MeterStyle=Set.String:S | Set.OptionCat:S
[Option31]
Meter=String
Text=Animate volume peaks next to slider
MeterStyle=Set.String:S | Set.OptionName:S
[Divider31]
Meter=Shape
Hidden=#StayOnDesktop#
MeterStyle=Set.Div:S
[Option32]
Meter=String
Text=Slide animations
Hidden=#StayOnDesktop#
MeterStyle=Set.String:S | Set.OptionName:S
[Divider32]
Meter=Shape
Hidden=#StayOnDesktop#
MeterStyle=Set.Div:S
[Option33]
Meter=String
Text=Animation easing type
Hidden=#StayOnDesktop#
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider33]
Meter=Shape
Hidden=#StayOnDesktop#
MeterStyle=Set.Div:S
[Option34]
Meter=String
Text=Animation direction
Hidden=#StayOnDesktop#
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider34]
Meter=Shape
Hidden=#StayOnDesktop#
MeterStyle=Set.Div:S
[Option35]
Meter=String
Text=Animation steps
Hidden=#StayOnDesktop#
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider35]
Meter=Shape
Hidden=#StayOnDesktop#
MeterStyle=Set.Div:S
[Option36]
Meter=String
Text=Animation displacement
Hidden=#StayOnDesktop#
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S

; [Header20]
; Meter=String
; Text=Background modification
; MeterStyle=Set.String:S | Set.OptionCat:S
; [Option21]
; Meter=String
; Text=Modify launched background
; MeterStyle=Set.String:S | Set.OptionName:S
; [Divider22]
; Meter=Shape
; MeterStyle=Set.Div:S
; [Option22]
; Meter=String
; Text=Background blur
; FontColor=#Set.Text_Color#,(#BackgroundMod# = 1 ? 255 : 150)
; MeterStyle=Set.String:S | Set.OptionName:S
; [Divider23]
; Meter=Shape
; MeterStyle=Set.Div:S
; [Option23]
; Meter=String
; Text=Background color
; FontColor=#Set.Text_Color#,(#BackgroundMod# = 1 ? 255 : 150)
; MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

; -------------------------------- Activation -------------------------------- ;
[Hotkey]
Meter=Shape
MeterStyle=Set.Bool:S
Hidden=#StayOnDesktop#
Y=([Option01:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Button04]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey', '')"]
Hidden=(#Hotkey# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
[Value04]
Meter=String
Text=#KeyInString#
Hidden=(#Hotkey# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[StayOnDesktop]
Meter=Shape
MeterStyle=Set.Bool:S | StayOnDesktop:#StayOnDesktop#
Y=([Option03:Y]-#Set.P#+(-20/2+8)*[Set.S])
[StayOnDesktop:0]
LeftMouseUpAction=[!WriteKeyValue "Mixdeck\Main\Elements\ControlScreen" "SavePosition" "1" "#SETTINGSPATH#Rainmeter.ini"][!WriteKeyValue "Mixdeck\Main\Elements\ControlScreen" "AlphaValue" "255" "#SETTINGSPATH#Rainmeter.ini"][!SetVariable StayOnDesktop 1][!WriteKeyValue Variables StayOnDesktop "1" "#Sec.SaveLocation#"][!UpdateMeasure Auto_Refresh:M][!UpdateMeter *][!Redraw]
[StayOnDesktop:1]
LeftMouseUpAction=[!WriteKeyValue "Mixdeck\Main\Elements\ControlScreen" "SavePosition" "0" "#SETTINGSPATH#Rainmeter.ini"][!WriteKeyValue "Mixdeck\Main\Elements\ControlScreen" "AlphaValue" "1" "#SETTINGSPATH#Rainmeter.ini"][!WriteKeyValue "Mixdeck\Main\Elements\ControlScreen" "WindowX" "" "#SETTINGSPATH#Rainmeter.ini"][!WriteKeyValue "Mixdeck\Main\Elements\ControlScreen" "WindowY" "" "#SETTINGSPATH#Rainmeter.ini"][!SetVariable StayOnDesktop 0][!WriteKeyValue Variables StayOnDesktop "0" "#Sec.SaveLocation#"][!UpdateMeasure Auto_Refresh:M][!UpdateMeter *][!Redraw]


; ------------------------ Applications and operation ------------------------ ;
[Button11]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\CollpaseIcons.inc]], '#CURRENTSECTION#', '#skin.Name#', 'CollapseIcons')"]
Hidden=(1-#Animated#)
[Value11]
Meter=String
Text=#CollapseIcons#
Hidden=(1-#Animated#)
MeterStyle=Set.String:S | Set.Value:S
[ExcludeApp]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
; ----------Tooltip
ToolTipTitle=#CURRENTSECTION#
ToolTipText=Apps to exclude from showing up in Mixdeck
[Value12]
Meter=String
Text=#ExcludeApp#
MeterStyle=Set.String:S | Set.Value:S
[VolumeChangeLevel]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|1|20
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value13]
Meter=String
Text=#VolumeChangeLevel#
MeterStyle=Set.String:S | Set.Value:S

; ----------------------------------- Tray ----------------------------------- ;
[Tray]
Meter=Shape
MeterStyle=Set.Bool:S
Hidden=#StayOnDesktop#
Y=([Option21:Y]-#Set.P#+(-20/2+8)*[Set.S])
[TrayScroll]
Meter=Shape
MeterStyle=Set.Bool:S
Hidden=(#Tray# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
Y=([Option22:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Button23]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option23:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_Color#,150
LeaveColor=#Set.Accent_Color#,50
Act=[!CommandMeasure Func "interactionBox([[#SKINSPATH##Skin.Name#\Core\InteractionBox\MixerIconTheme.inc]], '#IconTheme#')"]
Hidden=(#Tray# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
[Icon23]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=(14*[Set.S])
Hidden=(#Tray# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[Value23]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=#IconTheme#
Hidden=(#Tray# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S

; --------------------------------- Animation -------------------------------- ;
[VolumePeaks]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option31:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Animated]
Meter=Shape
MeterStyle=Set.Bool:S
Hidden=#StayOnDesktop#
Y=([Option32:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Button33]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option33:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_Color#,150
LeaveColor=#Set.Accent_Color#,50
Act=[!CommandMeasure Func "startSide('Ease')"]
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
[Icon33]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=(14*[Set.S])
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[Value33]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=#Easetype#
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[Button34]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option34:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop('AniDir', '#CURRENTSECTION#', 'MosaicShell', 'AniDir')"]
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
[Value34]
Meter=String
Text=From #AniDir#
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[AniSteps]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|10|100
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
Y=([Option35:Y]-#Set.P#+(-30/2+8)*[Set.S])
; ----------Tooltip
ToolTipTitle=#CURRENTSECTION#
ToolTipText=How much frames the animation has in total
[Value35]
Meter=String
Text=#AniSteps#
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S
[AnimationDisplacement]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|5|300
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
Y=([Option36:Y]-#Set.P#+(-30/2+8)*[Set.S])
; ----------Tooltip
ToolTipTitle=#CURRENTSECTION#
ToolTipText=How much distance Mixdeck travels during the animation
[Value36]
Meter=String
Text=#AnimationDisplacement#
Hidden=(#Animated# = 0 || #StayOnDesktop# = 1 ? 1 : 0)
MeterStyle=Set.String:S | Set.Value:S

; [BackgroundMod]
; Meter=Shape
; MeterStyle=Set.Bool:S
; Y=([Option21:Y]-#Set.P#+(-20/2+8)*[Set.S])
; [Blur:]
; Meter=Shape
; 0Act=[!WriteKeyValue Variables Blur "None" "#SKINSPATH##Skin.Name#\Main\Elements\Unload\Vars.inc"]
; 1Act=[!WriteKeyValue Variables Blur "Acrylic" "#SKINSPATH##Skin.Name#\Main\Elements\Unload\Vars.inc"]
; 2Act=[!WriteKeyValue Variables Blur "Blur" "#SKINSPATH##Skin.Name#\Main\Elements\Unload\Vars.inc"]
; MeterStyle=Set.Pick3:S
; Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('Blur', 'Acrylic')]+[&Func:returnBool('Blur', 'Blur')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
; Y=([Option22:Y]-#Set.P#+(-30/2+8)*[Set.S])
; Hidden=(1-#BackgroundMod#)
; [Blur:0]
; Meter=String
; Text=None
; FontColor=[&Func:returnBool('Blur', 'None', '#Set.Pri_color#', '#Set.Text_Color#')]
; MeterStyle=Set.String:S | Set.PickOption3_0:S
; Hidden=(1-#BackgroundMod#)
; [Blur:1]
; Meter=String
; Text=Acrylic
; FontColor=[&Func:returnBool('Blur', 'Acrylic', '#Set.Pri_color#', '#Set.Text_Color#')]
; MeterStyle=Set.String:S | Set.PickOption3_1:S
; Hidden=(1-#BackgroundMod#)
; [Blur:2]
; Meter=String
; Text=Blur
; FontColor=[&Func:returnBool('Blur', 'Blur', '#Set.Pri_color#', '#Set.Text_Color#')]
; MeterStyle=Set.String:S | Set.PickOption3_2:S
; Hidden=(1-#BackgroundMod#)

; [Solid]
; Meter=Shape
; MeterStyle=Set.ColorPacity:S
; LeftMouseUpAction=["#@#Addons\RainRGB4.exe" "VarName=#CURRENTSECTION#Color" "FileName=#SKINSPATH##Skin.Name#\Main\Elements\Unload\Vars.inc" "RefreshConfig=#CURRENTCONFIG# | #Skin.Name#\Main"]
; Y=([Option23:Y]-#Set.P#+(-30/2+8)*[Set.S])
; Type=Num|1|255
; Hidden=(1-#BackgroundMod#)
; [Value23]
; Meter=String
; Text=#SolidOpacity#
; MeterStyle=Set.String:S | Set.Pacity:S
; Hidden=(1-#BackgroundMod#)
