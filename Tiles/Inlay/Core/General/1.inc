﻿[Variables]
@includeVars=#SKINSPATH##Skin.Name#\Main\Accessories\Unload\Vars.inc
@includeWeatherVars=#SKINSPATH##Skin.Name#\Main\Modules\Vars\Weather.inc
Sec.SaveLocation2=#SKINSPATH##Skin.Name#\Main\Accessories\Unload\Vars.inc

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;
[Item.Shape:S]
X=(30*[Set.S])
Y=(35*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyvalue Variables page.page 2 "#SKINSPATH##Skin.Name#\Core\General.inc"][!Refresh]
DynamicVariables=1
Container=ContentContainer
[Item.StringIcon:S]
X=(50*[Set.S]/2)r
Y=(50*[Set.S]/2)r
StringAlign=leftCenter
FontFace=Segoe Fluent Icons
Container=ContentContainer
[Item.String:S]
X=(#Set.P#/2)R
Y=r
StringAlign=LeftCenter
Container=ContentContainer
[Item.Arrow.String:S]
X=([ContentContainer:W]-#Set.P#)
Y=r
StringAlign=RightCenter
Text=>
Container=ContentContainer



; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysTitle]
Meter=String
Text=Behavior
MeterStyle=Set.String:S | Set.OptionCat:S

[Item1.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xe8fc]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Activation settings for Inlay
MeterStyle=Set.String:S | Item.String:S
[Item1.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Item2.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xe8fb]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Text=Make Inlay replace the default start button
MeterStyle=Set.String:S | Item.String:S
[Item2.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S


[Set.Div:3121238]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Type to search when menu is active
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:21238]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Key to run after key input is captured
FontColor=#Set.Text_Color#,(105+150*#TypeToSearch#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:212348]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=Show taskbar when start menu is shown
MeterStyle=Set.String:S | Set.OptionName:S

;================================================================================================================================================================================

[HotkeysTitle3]
Meter=String
Text=Animation
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
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S1342]
Meter=Shape
MeterStyle=Set.Div:S
[Option33]
Meter=String
Text=Animation direction
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S21342]
Meter=Shape
MeterStyle=Set.Div:S
[Option34]
Meter=String
Text=Animation steps
FontColor=#Set.Text_Color#,(#Animated# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S

;================================================================================================================================================================================

[S12313]
Meter=String
Text=Background modification
MeterStyle=Set.String:S | Set.OptionCat:S

[Option40]
Meter=String
Text=Modify launched background
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:8]
Meter=Shape
MeterStyle=Set.Div:S
[Option41]
Meter=String
Text=Background blur
FontColor=#Set.Text_Color#,(#BackgroundMod# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:2131238]
Meter=Shape
MeterStyle=Set.Div:S
[Option42]
Meter=String
Text=Background color
FontColor=#Set.Text_Color#,(#BackgroundMod# = 1 ? 255 : 150)
MeterStyle=Set.String:S | Set.OptionName:S

;================================================================================================================================================================================


[TypeToSearch]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option02:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Button02]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey', '2', '', 1)"]
Hidden=(1-#TypeToSearch#)
[Value02]
Meter=String
Text=#Key2InString#
Hidden=(1-#TypeToSearch#)
MeterStyle=Set.String:S | Set.Value:S
[ShowTaskbar]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option04:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Animated]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option31:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Button32]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option32:Y]-#Set.P#+(-30/2+8)*[Set.S])
OverColor=#Set.Accent_color#,150
LeaveColor=#Set.Accent_color#,50
Act=[!CommandMeasure Func "startSide('Ease')"]
Hidden=(1-#Animated#)
[Icon32]
Meter=String
Text=[\xe895]
FontFace=Material Icons Round
FontSize=(14*[Set.S])
Hidden=(1-#Animated#)
MeterStyle=Set.String:S | Set.Value:S
[Value32]
Meter=String
X=(-25*[Set.S])r
Y=r
Text=#Easetype#
Hidden=(1-#Animated#)
MeterStyle=Set.String:S | Set.Value:S
[Button33]
Meter=Shape
MeterStyle=Set.DropDown:S
Y=([Option33:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop('AniDir', '#CURRENTSECTION#', 'MosaicShell', 'AniDir')"]
Hidden=(1-#Animated#)
[Value33]
Meter=String
Text=From #AniDir#
Hidden=(1-#Animated#)
MeterStyle=Set.String:S | Set.Value:S
[AniSteps]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Int|10|40
Y=([Option34:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(#Animated# = 1 ? 0 : 1)
[Value34]
Meter=String
Text=#AniSteps#
MeterStyle=Set.String:S | Set.Value:S
Hidden=(#Animated# = 1 ? 0 : 1)

[BackgroundMod]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option40:Y]-#Set.P#+(-20/2+8)*[Set.S])

[UnloadBlur:]
Meter=Shape
0Act=[!WriteKeyValue Variables UnloadBlur "None" "#Sec.SaveLocation2#"]
1Act=[!WriteKeyValue Variables UnloadBlur "Acrylic" "#Sec.SaveLocation2#"]
2Act=[!WriteKeyValue Variables UnloadBlur "Blur" "#Sec.SaveLocation2#"]
MeterStyle=Set.Pick3:S
Shape2=Rectangle (100*[Set.S]*([&Func:returnBool('UnloadBlur', 'Acrylic')]+[&Func:returnBool('UnloadBlur', 'Blur')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0
Y=([Option41:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=(1-#BackgroundMod#)
[UnloadBlur:0]
Meter=String
Text=None
FontColor=[&Func:returnBool('UnloadBlur', 'None', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_0:S
Hidden=(1-#BackgroundMod#)
[UnloadBlur:1]
Meter=String
Text=Acrylic
FontColor=[&Func:returnBool('UnloadBlur', 'Acrylic', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_1:S
Hidden=(1-#BackgroundMod#)
[UnloadBlur:2]
Meter=String
Text=Standard
FontColor=[&Func:returnBool('UnloadBlur', 'Blur', '#Set.Pri_color#', '#Set.Text_Color#')]
MeterStyle=Set.String:S | Set.PickOption3_2:S
Hidden=(1-#BackgroundMod#)
[Solid]
Meter=Shape
MeterStyle=Set.ColorPacity:S
LeftMouseUpAction=["#@#Addons\RainRGB4.exe" "VarName=#CURRENTSECTION#Color" "FileName=#Sec.SaveLocation2#" "RefreshConfig=#CURRENTCONFIG# | #Skin.Name#\Main"]
SaveLocation=2
Y=([Option42:Y]-#Set.P#+(-30/2+8)*[Set.S])
Type=Num|1|255
Hidden=(1-#BackgroundMod#)
[Value41]
Meter=String
Text=#SolidOpacity#
MeterStyle=Set.String:S | Set.Pacity:S
Hidden=(1-#BackgroundMod#)

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[Input2:M]
Measure=Plugin
Plugin=InputText
SolidColor=#Set.Pri_Color#
FontColor=#Set.Text_Color#
FontFace=Poppins
FontSize=((10*[Set.S])*#FontSizeModifier#)
StringAlign=LeftCenter
W=((150-10)*[Set.S])
H=((30-10)*[Set.S])
DynamicVariables=1
DefaultValue="[##Editing#]"
Command1=[!WriteKeyValue Variables "#Editing#" """$UserInput$""" "#Sec.SaveLocation2#"][!Refresh]

[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R

@includeKeys=#SKINSPATH##Skin.Name#\@Resources\Actions\HotKeys.ini
