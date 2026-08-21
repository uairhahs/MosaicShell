; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[Input:M]
Measure=Plugin
Plugin=InputText
SolidColor=#Set.Sec_Color#
FontColor=#Set.Text_Color#
; FontFace=#Set.Font#
; Poppins not installed in Windows
FontSize=(10*[Set.S])
StringAlign=Right
W=((200-20)*[Set.S])
H=((30-10)*[Set.S])
DynamicVariables=1
FocusDismiss=0
DefaultValue="[##Editing#]"
Command1=[!CommandMeasure Func "processInput('1', '', [[$UserInput$]], '')"]

; ---------------------------------------------------------------------------- ;
;                                 Meter styles                                 ;
; ---------------------------------------------------------------------------- ;

[Set.OptionCat:S]
X=(30*[Set.S])
Y=(30*[Set.S])R
FontSize=(14*[Set.S])
FontWeight=500
LeftMouseUpAction=[!SetClip "MosaicShell -> #Skin.Name# -> #Skin.Set_Page# -> Category: [&Func:LocalVar('#CURRENTSECTION#', 'Text')]"]
Container=ContentContainer
[Set.OptionCat.Description:S]
X=r
Y=R
FontSize=(10*[Set.S])
FontColor=#Set.SubText_Color#
Container=ContentContainer
[Set.OptionName:S]
X=r
Y=(20*[Set.S])R
FontSize=(10*[Set.S])
LeftMouseUpAction=[!SetClip "MosaicShell -> #Skin.Name# -> #Skin.Set_Page# -> Setting: [&Func:LocalVar('#CURRENTSECTION#', 'Text')]"]
Container=ContentContainer

; ---------------------------------- Generic --------------------------------- ;




[Set.Div:S]
X=r
y=(20*[Set.S])R
DynamicVariables=1
Container=ContentContainer
Shape=Line 0,0,(#Set.W#-30*[Set.S]-#Set.P#*4),0 | StrokeWidth 1 | Fill Color #Set.Pri_Color#,0 | Stroke LinearGradient This
This=0 | #Set.Text_Color#,25 ; 0.0 | #Set.Text_Color#,0 ; 0.5 | #Set.Text_Color#,25 ; 1.0

[Set.TextBox:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-210*[Set.S])
Shape=Rectangle 0,0,(200*[Set.S]),(40*[Set.S]),3 | StrokeWidth 0 | Extend This | OffSet 0,(-5*[Set.S])
This=Fill Color #Set.Sec_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!SetOption Input:M X "([#CURRENTSECTION#:X]+5*[Set.S])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y]+7*[Set.S])"][!CommandMeasure Func "processInput('0', '#CURRENTSECTION#', '', '#CURRENTSECTION#')"][!CommandMeasure Input:M "ExecuteBatch 1"]
Container=ContentContainer
DynamicVariables=1
[Set.Value:S]
X=((200-15)*[Set.S])r
Y=(15*[Set.S])r
W=((200-30)*[Set.S])
ClipString=1
StringAlign=rightCenter
FontSize=(10*[Set.S])
Container=ContentContainer
DynamicVariables=1
[Set.Bool:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-60*[Set.S])
Shape=Rectangle 0,0,(50*[Set.S]),(25*[Set.S]),(12.5*[Set.S]) | StrokeWidth (16*[Set.S]) | Extend This | OffSet 0,(-2.5*[Set.S]) | Fill Color 0,0,0,0
Shape2=Rectangle 0,0,(50*[Set.S]),(25*[Set.S]),(12.5*[Set.S]) | StrokeWidth 0 | Fill Color [&Func:returnBool('#CURRENTSECTION#', '1', '#Set.Positive_Color#', '#Set.SubText_Color#')] | OffSet 0,(-2.5*[Set.S])
Shape3=Ellipse ([##CURRENTSECTION#] = 1 ? (36*[Set.S]) : (14*[Set.S])),(10*[Set.S]),(8*[Set.S]) | StrokeWidth 0 | Fill Color #Set.Text_Color#
DynamicVariables=1
This=Stroke Color 0,0,0,0
Container=ContentContainer
MouseOverAction=[!SetOption #CURRENTSECTION# This "Stroke Color #set.ter_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Stroke Color 0,0,0,0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Leftmouseupaction=[!WriteKeyvalue Variables "#CURRENTSECTION#" "([##CURRENTSECTION#] = 1 ? 0 : 1)" "[#Sec.SaveLocation[&Func:LocalVar('#CURRENTSECTION#', 'SaveLocation', '')]]"][!SetVariable "#CURRENTSECTION#" "([##CURRENTSECTION#] = 1 ? 0 : 1)"][!UpdateMeasure Auto_Refresh:M][!UpdateMeter *][!Redraw]

; [Set.FontBox:S]
; X=((#Set.W#-30*[Set.S]-#Set.P#*3)-201*[Set.S])
; ; Y=([Option[&Func:LocalVar('#CURRENTSECTION#', 'Class')]:Y]-#Set.P#+(-30/2+8)*[Set.S])
; Container=ContentContainer
; DynamicVariables=1
; Shape=Rectangle 0,0,200,30,3,3 | StrokeWidth 2 | Stroke Color 0,0,0,1 | Extend Fill | Scale [Set.S],[Set.S],0,0
; Fill=Fill Color 0,0,0,1
; MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
; MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
; LeftMouseUpAction=[!CommandMeasure Func "startSide('Font', '#CURRENTSECTION#')"]
; RightMouseUpAction=[!SetOption Input:M X "([#CURRENTSECTION#:X]+5*[Set.S])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y]+5*[Set.S])"][!CommandMeasure Func "processInput('0', '#CURRENTSECTION#', '', '#CURRENTSECTION#')"][!CommandMeasure Input:M "ExecuteBatch 1"]


; ---------------------------------- Colors ---------------------------------- ;

[Set.Color:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-50*[Set.S])
Shape=Ellipse (20*[Set.S]),(15*[Set.S]),(20*[Set.S]) | StrokeWidth 0 | Extend This
Shape2=Ellipse (20*[Set.S]),(15*[Set.S]),(10*[Set.S]) | StrokeWidth (2*[Set.S]) | Extend That | Fill Color [##CURRENTSECTION#]
This=Fill Color #Set.Sec_Color#
That=Stroke Color #Set.Ter_color#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_color#"][!SetOption #CURRENTSECTION# That "Stroke Color #Set.Subtext_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# That "Stroke Color #Set.Ter_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
LeftMouseUpAction=[!SetOption Input:M X "([#CURRENTSECTION#:X]-200*[Set.S])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y]+5*[Set.S])"][!CommandMeasure Func "processInput('0', '#CURRENTSECTION#', '', '#CURRENTSECTION#')"][!CommandMeasure Input:M "ExecuteBatch 1"]
Container=ContentContainer

; [Set.Grad0:S]
; X=((#Set.W#-30*[Set.S]-#Set.P#*3)-151*[Set.S])
; DynamicVariables=1
; Shape=Rectangle 0,0,75,30,3 | StrokeWidth 0 | Fill LinearGradient Grad | Scale [Set.S],[Set.S],0,0
; Grad=180 | [#[&Func:GroupVar('#CURRENTSECTION#', 'Grad0')]] ; 0.0 | [#[&Func:GroupVar('#CURRENTSECTION#', 'Grad1')]] ; 2
; LeftMouseUpAction=["#@#Addons\RainRGB4.exe" "VarName=[&Func:GroupVar('#CURRENTSECTION#', 'Grad0')]" "FileName=#Sec.SaveLocation#" "RefreshConfig=#CURRENTCONFIG# | #Skin.Name#\Main"]
; Container=ContentContainer
; [Set.Grad1:S]
; X=r
; Y=r
; DynamicVariables=1
; Shape=Rectangle (75*[Set.S]),0,(75*[Set.S]),(30*[Set.S]),3 | StrokeWidth 0 | Fill LinearGradient Grad
; Grad=180 | [#[&Func:GroupVar('#CURRENTSECTION#', 'Grad0')]] ; -1 | [#[&Func:GroupVar('#CURRENTSECTION#', 'Grad1')]] ; 1
; LeftMouseUpAction=["#@#Addons\RainRGB4.exe" "VarName=[&Func:GroupVar('#CURRENTSECTION#', 'Grad1')]" "FileName=#Sec.SaveLocation#" "RefreshConfig=#CURRENTCONFIG# | #Skin.Name#\Main"]
; Container=ContentContainer

[Set.ColorPacity:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-140*[Set.S])
Shape=Rectangle 0,0,(130*[Set.S]),(40*[Set.S]),(20*[Set.S]) | StrokeWidth 0 | Extend This | OffSet 0,(-5*[Set.S])
Shape2=Ellipse (20*[Set.S]),(15*[Set.S]),(10*[Set.S]) | StrokeWidth (2*[Set.S]) | Extend That | Fill Color [##CURRENTSECTION#Color]
This=Fill Color #Set.Sec_Color#
That=Stroke Color #Set.Ter_color#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_color#"][!SetOption #CURRENTSECTION# That "Stroke Color #Set.Subtext_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# That "Stroke Color #Set.Ter_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!SetOption Input:M X "([#CURRENTSECTION#:X]-45*[Set.S])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y]+5*[Set.S])"][!CommandMeasure Func "processInput('0', '#CURRENTSECTION#Color', '', '#CURRENTSECTION#')"][!CommandMeasure Input:M "ExecuteBatch 1"]
RightMouseUpAction=[!SetOption Input:M X "([#CURRENTSECTION#:X]-45*[Set.S])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y]+5*[Set.S])"][!CommandMeasure Func "processInput('0', '#CURRENTSECTION#Opacity', '', '#CURRENTSECTION#')"][!CommandMeasure Input:M "ExecuteBatch 1"]
ToolTipTitle=Color: #CURRENTSECTION#Color
ToolTipText=Left click to change the color#CRLF#Right click to change the alpha
DynamicVariables=1
Container=ContentContainer
[Set.Pacity:S]
X=(45*[Set.S])r
Y=(15*[Set.S])r
StringAlign=LeftCenter
FontSize=(10*[Set.S])
Container=ContentContainer
Stringeffect=Shadow
Prefix="A: "
DynamicVariables=1

; ------------------------------ Action buttons ------------------------------ ;




[Set.Button:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-210*[Set.S])
Shape=Rectangle 0,0,(200*[Set.S]),(40*[Set.S]),3 | StrokeWidth 0 | Extend Fill | OffSet 0,(-5*[Set.S])
OverColor=#Set.Subtext_color#
LeaveColor=#Set.Ter_color#
DynamicVariables=1
Fill=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'OverColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[&Func:LocalVar('#CURRENTSECTION#', 'Act')]
Container=ContentContainer

[Set.RectButton:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-26*[Set.S])
; LeftMouseUpAction=[!commandMeasure Func "startPopup('#CURRENTSECTION#', 'info')"]
Hidden=1
Container=ContentContainer
[Set.Icon:S]
FontFace=Segoe MDL2 Assets
X=(26/2*[Set.S])r
Y=(26/2*[Set.S])r
StringAlign=CenterCenter
FontSize=(10*[Set.S])
Text=[\xEA1F]
Container=ContentContainer
DynamicVariables=1

[Set.DropDown:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-210*[Set.S])
Shape=Rectangle 0,0,(200*[Set.S]),(40*[Set.S]),3 | StrokeWidth 0 | Extend This | OffSet 0,(-5*[Set.S])
Shape2=Ellipse (25*[Set.S]),(15*[Set.S]),(12*[Set.S]) | StrokeWidth 2 | Fill Color #set.Ter_color# | Stroke Color #set.Subtext_color#,150
Shape3=Path Arrow | strokeWidth (2*[Set.S]) | Stroke Color #set.Text_color# | OffSet (20*[Set.S]),((40/2-15/2-5)*[Set.S])
This=Fill Color #Set.Sec_Color#
Arrow=0,(5*[Set.S]) | LineTo (5*[Set.S]),(10*[Set.S]) | LineTo (10*[Set.S]),(5*[Set.S])
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
DynamicVariables=1

; --------------------------------- 2 picker --------------------------------- ;




[Set.Pick:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-312*[Set.S])
Shape=Rectangle 0,0,300,30,7 | StrokeWidth 2 | Extend Fill | Scale [Set.S],[Set.S],0,0 | Stroke Color #Set.Text_Color#,50
Fill=Fill Color #Set.Text_Color#,25
Shape2=Rectangle ([#[&Func:GroupVar('#CURRENTSECTION#')]] = 0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
DynamicVariables=1
Container=ContentContainer
[Set.PickOption_0:S]
X=(300/4*[Set.S])r
Y=(30/2*[Set.S])r
FontSize=(10*[Set.S])
Padding=(300/6*[Set.S]),(30/4*[Set.S]),(300/6*[Set.S]),(30/4*[Set.S])
DynamicVariables=1
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
StringAlign=CenterCenter
Container=ContentContainer
[Set.PickOption_1:S]
X=(300/2*[Set.S])r
Y=r
FontSize=(10*[Set.S])
Padding=(300/6*[Set.S]),(30/4*[Set.S]),(300/6*[Set.S]),(30/4*[Set.S])
DynamicVariables=1
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
StringAlign=CenterCenter
Container=ContentContainer








; --------------------------------- 3 picker --------------------------------- ;




[Set.Pick3:S]
X=((#Set.W#-30*[Set.S]-#Set.P#*3)-312*[Set.S])
Shape=Rectangle 0,0,300,30,7 | StrokeWidth 2 | Extend Fill | Scale [Set.S],[Set.S],0,0 | Stroke Color #Set.Text_Color#,25
Fill=Fill Color #Set.Text_Color#,25
DynamicVariables=1
Container=ContentContainer
[Set.PickOption3_0:S]
X=(300/6*[Set.S])r
Y=(30/2*[Set.S])r
FontSize=(10*[Set.S])
Padding=(300/6*[Set.S]),(30/4*[Set.S]),(300/6*[Set.S]),(30/4*[Set.S])
DynamicVariables=1
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
StringAlign=CenterCenter
Container=ContentContainer
[Set.PickOption3_1:S]
X=(300/3*[Set.S])r
Y=r
FontSize=(10*[Set.S])
Padding=(300/6*[Set.S]),(30/4*[Set.S]),(300/6*[Set.S]),(30/4*[Set.S])
DynamicVariables=1
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
StringAlign=CenterCenter
Container=ContentContainer
[Set.PickOption3_2:S]
X=(300/3*[Set.S])r
Y=r
FontSize=(10*[Set.S])
Padding=(300/6*[Set.S]),(30/4*[Set.S]),(300/6*[Set.S]),(30/4*[Set.S])
DynamicVariables=1
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '2Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
StringAlign=CenterCenter
Container=ContentContainer


; ------------------------------- Side buttons ------------------------------- ;

; ---------------- Legacy styles
[PageCat:S]
FontFace=#Set.Font#
FontWeight=(650)
X=(#Set.P#/2)
Y=((24/2*[Set.S])+#Set.P#/1.5)r
StringAlign=Left
FontSize=(8*[Set.S])
FontColor=150,150,150
InlineSetting=CharacterSpacing | 1 | 1
Container=PageList.Container
Group=PageList
[PageBox:S]
X=(#Set.P#/2-5*[Set.S])
Y=((24/2*[Set.S])+#Set.P#/1.5)r
Shape=Rectangle 0,0,(#Set.L#-#Set.P#*2),(40*[Set.S]),(#Set.R#/2) | StrokeWidth 0 | Extend This
Shape2=Rectangle (5*[Set.S]),(5*[Set.S]),(30*[Set.S]),(30*[Set.S]),(#Set.R#) | StrokeWidth 0 | Extend Fill
Fill=Fill [&Func:returnBool('Skin.Set_Page', '[&Func:trim('#CURRENTSECTION#', '.Shape', '')]', 'LinearGradient CoreGrad', 'Color 0,0,0,1')]
CoreGrad=45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
Leftmouseupaction=[!WriteKeyvalue Variables "Skin.Set_Page" "#CURRENTSECTION#" "#@#CacheVars\Configurator.inc"][!Refresh]
This=Fill Color 0,0,0,1
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
OverColor=150
LeaveColor=0
DynamicVariables=1
Container=PageList.Container
Group=PageList
[PageIcon:S]
ImageName=#@#Icons\Settings\#CURRENTSECTION#.png
X=(#Set.P#/2)
Y=(5*[Set.S])r
W=(20*[Set.S])
H=(20*[Set.S])
Padding=(5*[Set.S]),(5*[Set.S]),(5*[Set.S]),(5*[Set.S])
DynamicVariables=1
ImageTint=#Set.Text_Color#
Container=PageList.Container
Group=PageList
[PageText:S]
X=(15*[Set.S])R
Y=(15*[Set.S])r
StringAlign=LeftCenter
FontColor=[&Func:returnBool('BarHandler', '#CURRENTSECTION#', '#Set.Text_Color#', '#Set.SubText_Color#')],(255*#process.tween_node_pagelist#)
; FontColor=(#BarHandler# = #CURRENTSECTION# ? 255 : 120),(#BarHandler# = #CURRENTSECTION# ? 255 : 120),(#BarHandler# = #CURRENTSECTION# ? 255 : 120)
FontSize=(10*[Set.S])
Container=PageList.Container
Group=PageList

; ---------------- MosaicShell v45000 Styles

[PageDiv:S]
X=(#Set.P#/2)
y=(15*[Set.S])R
Container=PageList.Container
Group=PageList
Shape=Line 0,0,((#Set.L#-#Set.P#*2)*#process.tween_node_pagelist#+(30*[Set.S]*(1-#process.tween_node_pagelist#))),0 | StrokeWidth 2 | Fill Color #Set.Sec_Color#,0 | Stroke Color #Set.Text_Color#,25
DynamicVariables=1
[PageList.Category:S]
X=(#Set.P#/2)
Y=(20*[Set.S]+#Set.P#/2)r
W=((#Set.L#-#Set.P#)*#process.tween_node_pagelist#+(30*[Set.S]*(1-#process.tween_node_pagelist#)))
FontFace=#Set.Font#
FontWeight=(650)
StringAlign=Left
FontSize=(8*[Set.S])
FontColor=#Set.SubText_Color#
ClipString=1
InlineSetting=CharacterSpacing | 1 | 1
Container=PageList.Container
Group=PageList
[PageList.Item.Shape:S]
X=(#Set.P#/2-5*[Set.S])
Y=(25*[Set.S])r
Shape=Rectangle 0,0,(#Set.L#-#Set.P#*2),(40*[Set.S]),(#Set.R#/2) | StrokeWidth 0 | Extend This
Shape2=Rectangle (5*[Set.S]),(5*[Set.S]),(30*[Set.S]),(30*[Set.S]),(#Set.R#) | StrokeWidth 0 | [&Func:returnBool('Skin.Set_Page', '[&Func:trim('#CURRENTSECTION#', '.Shape', '')]', 'Fill LinearGradient CoreGrad', 'Fill Color 0,0,0,1')]
CoreGrad=45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
Leftmouseupaction=[!WriteKeyvalue Variables "Skin.Set_Page" "[&Func:trim('#CURRENTSECTION#', '.Shape', '')]" "#@#CacheVars\Configurator.inc"][!Refresh]
This=Fill Color 0,0,0,1
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
OverColor=150
LeaveColor=0
DynamicVariables=1
Group=PageList
Container=PageList.Container
[PageList.Item.Icon.String:S]
X=(20*[Set.S])r
Y=(21*[Set.S])r
DynamicVariables=1
StringAlign=CenterCenter
FontSize=(16*[Set.S])
FontFace=Material Icons Round
InlineSetting=Shadow | 0 | 0 | 5 | #Set.Pri_Color#,75
Group=PageList
Container=PageList.Container
[PageList.Item.String:S]
X=(25*[Set.S])r
Y=r
StringAlign=LeftCenter
;Text=[&Func:trim('#CURRENTSECTION#', '.String', '')]
;Above line is not used as to allow translations
FontColor=[&Func:returnBool('Skin.Set_page', '[&Func:trim('#CURRENTSECTION#', '.String', '')]', '#Set.Text_Color#', '#Set.SubText_Color#')],(255*#process.tween_node_pagelist#)
FontSize=((10*[Set.S]))
Group=PageList
Container=PageList.Container
