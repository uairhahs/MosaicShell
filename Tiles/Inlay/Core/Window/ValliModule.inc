[Variables]
SEc.StartAction=[!SetOption NavBar.Button#Sec.arg2#.StringIcon MeterStyle "Sec.String:S | NavBar.Button.StringIcon:S | NavBar.ButtonSelected.StringIcon:S"][!UpdateMeter NavBar.Button#Sec.arg2#.StringIcon][!Redraw]
; Sec.Box.W=((#W#-#Sec.P#*4)/3)
Sec.Box.H=(250*[S])
Sec.Box.P=(70*[S])
Sec.Box.DLC.H=(200*[S])
Sec.AnchorName=AnchorSuppli
@includeSecVars=#@#CacheVars\Configurator.inc
@includeSkinVars=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
; @includeSkinvars=#SKINSPATH##Skin.Name#\@Resources\Vars.inc

[LogicLua]
Measure=Script
ScriptFile=#SKINSPATH##Skin.name#\Core\Module\Logic.lua
; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;
[Item.Shape:S]
X=(20*[S])
Y=(40*[S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[S]),(2*[S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.StringIcon:S]
X=(50*[S]/2)r
Y=(50*[S]/2)r
StringAlign=leftCenter
FontFace=Segoe Fluent Icons
Container=ContentContainer
[Item.String:S]
X=(#Set.P#/2)R
Y=r
StringAlign=LeftCenter
Container=ContentContainer
[Item.Button.Shape:S]
X=([ContentContainer:W]-#Set.P#-30*[S])
Y=(-15*[S])r
Shape=Rectangle 0,0,(30*[S]),(30*[S]),(5*[S]) | StrokeWidth 0 | Extend Stroke
Stroke=Fill Color #Set.Positive_color#,100
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color #Set.Positive_color#,200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color #Set.Positive_color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.Button.StringIcon:S]
X=(30*[S]/2)r
Y=(30*[S]/2)r
StringAlign=CenterCEnter
Text=[\xe5ce]
FontFace=Material Icons Round
Container=ContentContainer

[Module.Shape:S]
X=(#Sec.P#*3+20*[S])R
Y=(-#Sec.Box.H#+#Sec.P#+40*[S])r
Shape=Rectangle 0,0,(((#W#-#Sec.P#*4)/3)),(#Sec.Box.H#),(#Sec.R#/4) | StrokeWidth 0 | Extend Fill
Shape2=Line (#Set.P#*0.5),(#Sec.Box.H#-#Sec.Box.P#),(((#W#-#Sec.P#*4)/3)-#Sec.P#),(#Sec.Box.H#-#Sec.Box.P#) | STrokeWidth 2 | Extend Stroke
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure LogicLua """ChangeModuleTo('[&Func:trim('#CURRENTSECTIOn#','.Shape','')]')"""]
DynamicVariables=1
Container=ContentContainer
[Module.Image:S]
X=(10*[S])r
Y=(10*[S])r
W=(((#W#-#Sec.P#*4)/3)-20*[S])
H=(#Sec.Box.H#-#Sec.Box.P#-#Sec.P#)
ImageName=#SKINSPATH##Skin.Name#\Core\Module\[&Func:trim('#CURRENTSECTIOn#','.Image','')].png
DynamicVariables=1
MaskImageName=#SKINSPATH##Skin.Name#\Core\Module\#ModuleMask.png
Container=ContentContainer
[Module.String:S]
X=(#Sec.P#/2)r
Y=(-#Sec.P#/2)R
Text=[&Func:trim('#CURRENTSECTIOn#','.String','')]
StringAlign=LeftBottom
FontSize=((20*[S])*#FontSizeModifier#)
FontWeight=650
DynamicVariables=1
[Module.Description.String:S]
X=r
Y=(#Sec.P#*1.5)r
W=(((#W#-#Sec.P#*4)/3)-#Sec.P#*2-40*[S])
H=(#Sec.Box.P#-#Sec.P#)
FontSize=((12*[S])*#FontSizeModifier#)
ClipString=2

[DLC.Shape:S]
X=((((#W#-#Sec.P#*4)/3))-10*[S]+#sec.P#/2)r
Y=(-#Sec.Box.DLC.H#+#Sec.P#/2+10*[S])r
Shape=Rectangle 0,0,(((#W#-#Sec.P#*4)/3)),(#Sec.Box.DLC.H#),(#Sec.R#/4) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure LogicLua """ChangeModuleTo('[&Func:trim('#CURRENTSECTIOn#','.Shape','')]')"""]
DynamicVariables=1
Container=ContentContainer
[DLC.Image:S]
X=(10*[S])r
Y=(10*[S])r
W=(((#W#-#Sec.P#*4)/3)-20*[S])
H=(#Sec.Box.DLC.H#-20*[S])
ImageName=#SKINSPATH##Skin.Name#\Core\Module\[&Func:trim('#CURRENTSECTIOn#','.Image','')].png
DynamicVariables=1
MaskImageName=#SKINSPATH##Skin.Name#\Core\Module\#ModuleMask.png
Container=ContentContainer
[DLC.String:S]
X=(#Sec.P#/2)r
Y=(-#Sec.P#/2)R
Text=[&Func:trim('#CURRENTSECTIOn#','.String','')]
StringAlign=LeftBottom
FontSize=((20*[S])*#FontSizeModifier#)
FontWeight=650
DynamicVariables=1

[NavBar.Button.StringIcon:S]
FontFace=Material Icons Round
x=(60*[S])r
Y=r
FontSize=((20*[S])*#FontSizeModifier#)
FontColor=#Set.Subtext_Color#
MouseOverAction=[!SetOption #CURRENTSECTIon# Font_Color #Set.Text_Color#][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTIon# Font_Color #Set.Subtext_Color#][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "[&Func:trim('#CURRENTSECTIOn#','[^0-9]','')]" "#CURRENTPATH#Main.ini"][!Refresh]
StringAlign=centerCEnter
[NavBar.ButtonSelected.StringIcon:S]
FontColor=#Set.Text_Color#
MouseLeaveAction=
MouseOverAction=

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Navbar.Shape]
Meter=Shape
X=(#W#/2-300*[S]/2)
Y=(#Sec.P#+#Scroll#)
Shape=Rectangle 0,0,300,40,#Sec.R# | StrokeWidth 0 | Fill Color #Set.Sec_Color# | Scale [S],[S],0,0
DynamicVariables=1
Container=ContentContainer
[NavBar.Button1.StringIcon]
Meter=String
X=(300*[S]/2-60*[S]*3/2)r
Y=(40*[S]/2)r
Text=[\xe88a]
MeterStyle=Sec.String:S | NavBar.Button.StringIcon:S
[NavBar.Button2.StringIcon]
Meter=String
Text=[\xf060]
MeterStyle=Sec.String:S | NavBar.Button.StringIcon:S
[NavBar.Button3.StringIcon]
Meter=String
Text=[\xe88e]
MeterStyle=Sec.String:S | NavBar.Button.StringIcon:S
[NavBar.Button4.StringIcon]
Meter=String
Text=[\xe8cc]
MeterStyle=Sec.String:S | NavBar.Button.StringIcon:S


@includeSubpage=ValliModule\#Sec.arg2#.inc
