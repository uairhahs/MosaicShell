[Variables]
@includeCacheVarsConfigurator=#@#CacheVars\Configurator.inc
Sec.W=(500*#Sec.S#)
Sec.H=(380*#Sec.S#)
AniDir=Bottom
[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;
[Button.Shape:S]
X=(#Sec.P#+150/2*#Sec.S#)r
Y=(-40/2*#sec.s#)r
Shape=Rectangle  0,0,(150*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
Group=Button
[Button.String:S]
Meter=String
X=(150/2*#Sec.s#)r
Y=(40/2*#sec.s#)r
StringAlign=CenterCenter
DynamicVariables=1
Group=Button

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[B]
Meter = Shape
Shape=Rectangle 0,0,(#Sec.W#),(#Sec.H#),#Sec.WinR# | StrokeWidth 0 | Fill Color #Set.Pri_color#
DynamicVariables=1

[HeaderString]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.P#)
W=(#Sec.W#-#Sec.P#*2)
ClipString=1
StringAlign=Center
MeterStyle=Sec.String:S
FontSize=(14*#sec.s#)
FontWeight=650
DynamicVariables=1
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
Text=You'll need ImageMagick for this.

[Description]
Meter=String
X=r
Y=(#Sec.P#/2)R
W=(#Sec.W#-#Sec.P#*2)
Clipstring=1
StringAlign=Center
MeterStyle=Sec.String:S
FontSize=(10*#sec.s#)
Text=Just a friendly reminder. You can learn how to install it below.
DynamicVariables=1

[Image]
Meter=Image
ImageName=#@#Images\ImageMagick.png
X=(#Sec.W#/2-150*#Sec.S#/2)
Y=(#Sec.H#/2-150*#Sec.S#/2)
W=(150*#Sec.S#)
H=(150*#Sec.S#)

[Button1.Shape]
Meter=Shape
X=(#Sec.W#/2-(150*#Sec.S#*2+#Sec.P#)/2)
Y=(#Sec.H#-#Sec.P#-40*#Sec.S#)
Fill=Fill Color #Set.Ter_Color#
LeftMouseUpAction=[!WriteKeyValue Variables AlreadyHasImageMagick 1 "#@#Vars.inc"][!CommandMeasure ActionTImer "Execute 2"][!Refresh "#MosaicShell\Main"]
MeterStyle=Button.Shape:S
[Button1.String]
Meter=String
Text="Already installed"
; FontColor=#Set.SubText_Color#
MeterStyle=Sec.String:S | Button.String:S
[Button2.Shape]
Meter=Shape
Fill=Fill Color #Set.Positive_color#
LeftMouseUpAction=["https://mosaicshell.gitbook.io/core/getting-started/additional-requisites#imagemagick"]
MeterStyle=Button.Shape:S
[Button2.String]
Meter=String
Text="Install guide"
FontColor=#Set.Pri_color#
FontWeight=600
MeterStyle=Sec.String:S | Button.String:S
