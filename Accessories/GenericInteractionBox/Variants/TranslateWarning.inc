[Variables]
Sec.W=(500*#Sec.S#)
Sec.H=(300*#Sec.S#)
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
;                                  MeterStyleS                                 ;
; ---------------------------------------------------------------------------- ;


; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[ImageContainer]
Meter=Shape
Shape=Rectangle 0,0,(#Sec.W#),[Image:H],#Set.R# | StrokeWidth 0 | Fill LinearGradient This
This=90 | 0,0,0,0 ; 0 | 0,0,0,255 ; 1
DynamicVariables=1

[Image]
Meter=Image
Container=Imagecontainer
ImageName=#@#Images\TourPreview.png
W=#Sec.W#
H=(200*#sec.S#)
PreserveAspectRatio=2
ImageAlpha=175

[Img]
Meter=Image
X=(#Sec.W#/2-[Img:W]/2)
Y=#Sec.P#
W=(100*#Sec.S#)
H=(100*#Sec.S#)
DynamicVariables=1
ImageName=#@#Images\CoreAssets\#Set.IconStyle#Logo256.png

[Text]
Meter=String
Text=Warning
X=(#Sec.W#/2)
Y=(15*#Sec.s#)R
W=(#Sec.W#-#Sec.P#*2)
FontSize=(20*#Sec.S#)
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 1.0 | #Set.Accent_Color_2# ; 0.0
StringAlign=center
MeterStyle=Sec.String:S
FontWeight=650
[Text2]
Meter=String
Text=This feature is still in BETA, and might cause functionality issues. Proceed?
X=r
Y=R
W=(#Sec.W#-#Sec.P#*2)
FontColor=160,155,145
StringAlign=center
MeterStyle=Sec.String:S

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;





[BackButton]
Meter=Shape
X=(#Sec.W#/2-120*#sec.s#-#Sec.p#/2)
Y=(#Sec.H#-#sec.P#-40*#sec.s#)
Shape=Rectangle  0,0,(120*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Fill=Fill Color #Set.Ter_Color#
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables Set.Lang """English""" "#@#Vars.inc"][!CommandMeasure fUnc "interactionBox('tour4')"][!Refresh]
[BackText]
Meter=String
X=(60*#Sec.s#)r
Y=(#Sec.H#-#Sec.P#-20*#sec.s#)
StringAlign=CenterCenter
Text=Cancel
FontWeight=600
MeterStyle=Sec.String:S
[SubtleButton]
Meter=Shape
X=(#Sec.W#/2+#Sec.p#/2)
Y=(#Sec.H#-#sec.P#-40*#sec.s#)
Shape=Rectangle  0,0,(120*#sec.S#),(40*#Sec.S#),(#set.R#/2) | StrokeWidth 0 | Extend Fill | Extend scale
Fill=Fill Color #Set.Positive_color#
Scale=Scale 1,1
MouseOverAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1.1,1.1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" Scale "Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
LeftMouseUpAction=[!CommandMeasure fUnc "interactionBox('tour4')"][!Refresh]
[SubtleText]
Meter=String
X=(60*#Sec.s#)r
Y=(#Sec.H#-#Sec.P#-20*#sec.s#)
FontColor=#Set.Pri_color#
StringAlign=CenterCenter
Text=Continue
FontWeight=600
MeterStyle=Sec.String:S

[Esc]
Meter=
