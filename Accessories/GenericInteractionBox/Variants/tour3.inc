[Variables]
Sec.W=(500*#Sec.S#)
Sec.H=(700*#Sec.S#)
AniDir=Bottom
BackgroundCheckStatus=0
GlobalCheckStatus=0
tweennode3=0
tweennode4=0

width=((#Sec.W#-#Sec.P#*3)/2)
imgwidth=(75*#sec.s#)
[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
; ---------------------------------------------------------------------------- ;
;                                  MeterStyleS                                 ;
; ---------------------------------------------------------------------------- ;

[Support:S]
DynamicVariables=1
[SupportImage:S]
X=((#width#/2-#imgwidth#/2))r
Y=((#width#/2-#imgwidth#/2))r
W=(#imgwidth#)
H=(#imgwidth#)
DynamicVariables=1
[SupportText:S]
FontSize=(10*#Sec.S#)
Inlinepattern=#CRLF#.*
InlineSetting=Weight | 650
Inlinepattern2=#CRLF#.*
InlineSetting2=Size | (20*#Sec.S#)
Inlinepattern3=#CRLF#.*
StringAlign=CenterCenter
FontColor=#Set.Subtext_Color#
DynamicVariables=1

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
Text=Join our growing community!#CRLF#JaxCore is in active development!
X=(#Sec.W#/2)
Y=(15*#Sec.s#)R
FontSize=(20*#Sec.S#)
InlinePattern=.*#CRLF#
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 1.0 | #Set.Accent_Color_2# ; 0.0
INlinePattern2=#CRLF#.*
InlineSetting2=Size | (10*#Sec.S#)
INlinePattern3=#CRLF#.*
InlineSetting3=Color | 160,155,145
INlinePattern4=#CRLF#.*
InlineSetting4=Weight | 400
StringAlign=center
MeterStyle=Sec.String:S
FontWeight=650

[Support.1]
Meter=Shape
X=#Sec.P#
Y=(30*#Sec.S#)R
This=0,0 | 4,255,83,(100*#TweenNode3#) ; 0.0 | 0,0,0,0 ; 1.0
That=180 | 04ff58 ; 0.0 | 04cc47 ; 1.0
Shape=Rectangle 0,0,#width#,#width#,5 | StrokeWidth ((2+4*#TweenNode3#)*#Sec.S#) | Fill RadialGradient This | Stroke LinearGradient That
MeterStyle=Support:S
MouseOverAction=[!SEtVariable tweennode3 1][!Updatemeter *][!Redraw]
MouseLeaveAction=[!SEtVariable tweennode3 0][!Updatemeter *][!Redraw]
LeftMouseUpAction=["https://www.deviantart.com/jaxoriginals"]
[SupportImage.1]
Meter=Image
ImageName=#ROOTCONFIGPATH#Accessories\Tour\Images\DA.png
MeterStyle=SupportImage:S
[SupportText.1]
MEter=String
Text=Watch me on#CRLF#DeviantArt
InlineSetting3=GradientColor | 180 | 04ff58 ; 0.0 | 04cc47 ; 1.0
MeterStyle=Sec.String:S | SupportText:S
X=([Support.1:X]+(#width#/2))
Y=([Support.1:Y]+#Width#+#Sec.P#*2)
W=(#width#)

[Support.3]
Meter=Shape
X=(#Sec.W#/2+#Sec.P#/2)
Y=[Support.1:Y]
This=0,0 | 88, 101, 242,(100*#TweenNode4#) ; 0.0 | 0,0,0,0 ; 1.0
That=180 | 5865f2 ; 0.0 | 112, 125, 255 ; 1.0
Shape=Rectangle 0,0,#width#,#width#,5 | StrokeWidth ((2+4*#TweenNode4#)*#Sec.S#) | Fill RadialGradient This | Stroke LinearGradient That
MouseOverAction=[!SEtVariable tweennode4 1][!Updatemeter *][!Redraw]
MouseLeaveAction=[!SEtVariable tweennode4 0][!Updatemeter *][!Redraw]
MeterStyle=Support:S
LeftMouseUpAction=["https://discord.gg/JmgehPSDD6"]
[SupportImage.3]
Meter=Image
ImageName=#@#Icons\Discord.png
MeterStyle=SupportImage:S
ImageTint=5865f2
Greyscale=1
[SupportText.3]
MEter=String
Text=Join our#CRLF#Discord server
InlineSetting3=GradientColor | 180 | 5865f2 ; 0.0 | 112, 125, 255 ; 1.0
MeterStyle=Sec.String:S | SupportText:S
X=([Support.3:X]+(#width#/2))
Y=([Support.3:Y]+#Width#+#Sec.P#*2)
W=(#width#)


[SubtleText]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.H#-#Sec.P#)
FontColor=#Set.Subtext_Color#
StringAlign=CenterBottom
Text=Continue
MouseOverAction=[!SetOption #CURRENTSECTION# InlineSetting "Underline"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# InlineSetting ""][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMEasure fUnc "interactionBox('tour4')"][!Refresh]
MeterStyle=Sec.String:S

[Esc]
Meter=