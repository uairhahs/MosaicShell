﻿[Variables]
BoxH=125

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Sec.Section.Text:S]
X=(5*[Set.S])R
Y=r
W=(#Set.W#-#Set.P#*2-300*[Set.S])
ClipString=2
FontSize=(15*[Set.S])
StringAlign=LeftCenter
InlineSetting=Weight | 650
[Sec.Section.Icon:S]
X=(#Set.P#)r
Y=(#BoxH#/2*[Set.S])r
FontSize=(20*[set.s])
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
Text=MosaicShell documentation
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
Text=This is the detailed documentation for MosaicShell, its structure and how to build a module for MosaicShell along with handy tricks used while coding modules for MosaicShell (or any Rainmeter skin).



[Section1]
Meter=Shape
X=#Set.P#
Y=(250*[Set.S])
MeterStyle=Sec.Section.Shape:S
LeftMouseUpAction=["https://mosaicshell.gitbook.io/core/using-mosaicshell/user-interface"]
[Section1.Icon]
Meter=String
Text=[\xe7fd]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Section1.Text]
Meter=String
Text=I am a MosaicShell user
InlinePattern=MosaicShell user
MeterStyle=Set.String:S | Sec.Section.Text:S

[Section2]
Meter=Shape
MeterStyle=Sec.Section.Shape:S
LeftMouseUpAction=["https://mosaicshell.gitbook.io/core/coding-a-module/code-formatting"]
[Section2.Icon]
Meter=String
Text=[\xe54b]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Section2.Text]
Meter=String
Text=I am a skin developer looking for tips and tricks
InlinePattern=tips and tricks
MeterStyle=Set.String:S | Sec.Section.Text:S

[Section3]
Meter=Shape
MeterStyle=Sec.Section.Shape:S
Leftmouseupaction=["https://mosaicshell.gitbook.io/core/module-structure/root-outline"]
[Section3.Icon]
Meter=String
Text=[\xef54]
MeterStyle=Set.String:S | Set.Icon:S | Sec.Section.Icon:S
[Section3.Text]
Meter=String
Text=I am here to learn how to use Core to make modules / widgets
InlinePattern=use Core.*
MeterStyle=Set.String:S | Sec.Section.Text:S
