﻿[Variables]
BoxH=50

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Sec.Section.Text:S]
X=(5*[Set.S])R
Y=r
W=(#Set.W#-#Set.P#*2-300*[Set.S])
ClipString=2
FontSize=(12*[Set.S])
StringAlign=LeftCenter
InlineSetting=Weight | 650
[Sec.Section.Icon:S]
X=(#Set.P#)r
Y=(#BoxH#/2*[Set.S])r
FontSize=(20*[set.s])
StringAlign=LeftCenter
FontFace=Material Icons Round
Container=#Sec.Container#
[Sec.Section.Shape:S]
X=#Set.P#
Y=(#BoxH#/2*[Set.S]+#Set.P#/2)r
Shape=Rectangle 0,0,(#Set.W#-#Set.P#*2),(#BoxH#*[Set.S]),(#Set.R#/4) | Extend Fill | Stroke Color #Set.Accent_Color#
Fill=Fill Color #Set.Sec_Color#,100 | StrokeWidth 0
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#,200 | StrokeWidth 2"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#,100 | StrokeWidth 0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=#Sec.Container#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[BarClip]
Fill=Fill Color #Set.Pri_Color#,(#Scroll# > 0 ? 255 : 0)
Group=Scroll

[Image]
Meter=Image
W=(#Set.W#)
ImageAlpha=25
PreserveAspectRatio=2
DynamicVariables=1
ImageName=#@#Images\Cards\IconMinimalistWallpaper.png
MaskImageName=#@#Images\FadeMask.png
Container=BC

[title.Text1]
Meter=String
MeterStyle=Set.String:S
Text=Developer tools
X=(#Set.W#/2)
Y=((110*[Set.S]))
FontSize=(40*[Set.S])
FontFace=#Set.Font#
FontWeight=(650)
StringAlign=Center
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0

[SpotDes]
Meter=String
X=r
y=R
W=(#Set.W#-#Set.P#*4)
ClipString=2
FontSize=(12*[Set.S])
FontFace=#Set.Font#
Fontcolor=#Set.SubText_Color#
StringAlign=Center
MeterStyle=Set.String:S
Text=You can find the tools you need as a developer for Core.


; [Section1]
; Meter=Shape
; MeterStyle=Sec.Section.Shape:S
; LeftMouseUpAction=[!commandMeasure Func "startPopup('CoreNews', 'Left')"]
; [Section1.Icon]
; Meter=String
; Text=[\xf009]
; MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
; [Section1.Text]
; Meter=String
; Text="news"
; MeterStyle=Set.String:S | Sec.Section.Text:S

[Section2]
Meter=Shape
X=#Set.P#
Y=(250*[Set.S])
MeterStyle=Sec.Section.Shape:S
LeftMouseUpAction=[!WriteKeyValue Variables process.page 0 "#ROOTCONFIGPATH#Accessories\Welcome\Main.ini"][!ActivateConfig "#MosaicShell\Accessories\Welcome"]
[Section2.Icon]
Meter=String
Text=[\xe57b]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Section2.Text]
Meter=String
Text="activateConfig #MosaicShell\Accessories\Welcome"
MeterStyle=Set.String:S | Sec.Section.Text:S

; [Section3]
; Meter=Shape
; MeterStyle=Sec.Section.Shape:S
; LeftMouseUpAction=[!CommandMeasure Func "interactionBox('GenerateCorePage')"]
; [Section3.Icon]
; Meter=String
; Text=[\xeff0]
; MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
; [Section3.Text]
; Meter=String
; Text="interactionBox('GenerateCorePage')"
; MeterStyle=Set.String:S | Sec.Section.Text:S

[Section4]
Meter=Shape
MeterStyle=Sec.Section.Shape:S
LeftMouseUpAction=[!ActivateConfig "#MosaicShell\Main\Installation"]
[Section4.Icon]
Meter=String
Text=[\xe57b]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Section4.Text]
Meter=String
Text="core_install"
MeterStyle=Set.String:S | Sec.Section.Text:S

[Section5]
Meter=Shape
MeterStyle=Sec.Section.Shape:S
LeftMouseUpAction=[!CommandMeasure Func "interactionBox('BatchInstall')"][!DeactivateConfig]
[Section5.Icon]
Meter=String
Text=[\xe57b]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Section5.Text]
Meter=String
Text="core_stuff_batch_installer"
MeterStyle=Set.String:S | Sec.Section.Text:S

[Sectom6]
Meter=Shape
MeterStyle=Sec.Section.Shape:S
LeftMouseUpAction=[!activateConfig "#MosaicShell\Main" "S-Hub.ini"]
[Sectom6.Icon]
Meter=String
Text=[\xead5]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Sectom6.Text]
Meter=String
Text="mosaicshell_s-hub_app"
MeterStyle=Set.String:S | Sec.Section.Text:S
