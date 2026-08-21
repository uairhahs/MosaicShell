[Variables]
H=(#Height#*#scale#)
R=(14*#Scale#)
VolumeW=(28*#Scale#)
MediaW=(200*#Scale#)
Locksw=(50*#Scale#)
P=(50*#Scale#)

AdditionalToggleOnOptions=[!SetOption LocksB MeterStyle "LocksB.On:S"]
AdditionalToggleOffOptions=[!SetOption LocksB MeterStyle "LocksB.Off:S"]
MHC_Reset=[!SetOption MediaHoverCircle Stroke "Fill Color #FontColor#,20"][!UpdateMeter MediaHoverCircle][!Redraw]

SliderRotation=v

[SliderStart]
Formula=0
[SliderEnd]
Formula=#H#

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;
[Sec.BottomButton:S]
X=(#P#/2)R
Y=r
W=(20*#scale#)
H=(20*#scale#)
Grayscale=1
ImageTint=#FontColor#
DynamicVariables=1
MouseOverAction=[!SetOption MediaHoverCircle X "([#CURRENTSECTION#:X])"][!SetOption MediaHoverCircle Y "([#CURRENTSECTION#:Y])"][!ShowMeter MediaHoverCircle][!UpdateMeter MediaHoverCircle][!Redraw]
MouseLeaveAction=[!HideMeter MediaHoverCircle][!Redraw]
LeftMouseDownAction=[!SetOption MediaHoverCircle Stroke "Fill Color #FontColor#,150"][!UpdateMeter MediaHoverCircle][!Redraw]
Group=Music | Standard
Container=MediaC

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; --------------------------------- VolumeBar -------------------------------- ;

[VolumeB]
Meter=Shape
X=(#Stroke#/2+20*#Scale#+#MediaW#)
Y=#Stroke#/2
Shape=Rectangle 0,0,#VolumeW#,#H#,#R# | StrokeWidth #Stroke# | Stroke Color #BorderColor#,#BorderOpacity# | Extend This
This=Fill Color #PrimaryColor#,#PrimaryOpacity#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #SecondaryColor#,#PrimaryOpacity#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #PrimaryColor#,#PrimaryOpacity#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Group=Standard
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#VolumeW#,#H#,#R# | StrokeWidth 0
Group=Standard


;
[VolumeBar]
Meter=Shape
Shape=Rectangle (#Stroke#/2),(#H#-#Stroke#/2),(#VolumeW#-#Stroke#),(-(#H#-#Stroke#)*[#CurrentSlider#:]/100),((#VolumeW#-#Stroke#)/2) | StrokeWidth 0 | Fill LinearGradient This
This=90 | #AccentColor1# ; 0 | #AccentColor2# ; 1
; Shape=Path That | StrokeWidth (0) | Fill LinearGradient This
; That=0,#H# | LineTo (#VolumeW#),#H# | LineTo #VolumeW#,((#H#)*(1-[#CurrentSlider#:]/100)-10*#Scale#) | LineTo 0,((#H#)*(1-[#CurrentSlider#:]/100)) | ClosePath 1
LeftMouseDownAction=[!CommandMeasure VolumeMouse "Start"]
DynamicVariables=1
Container=VolumeC
Group=Dynamic | Standard

; ------------------------------ MediaPlayerBar ------------------------------ ;

[MediaBAnchor]
Meter=String
X=(#Stroke#/2)
Y=(#Stroke#/2)
[MediaB]
Meter=Shape
X=(#Stroke#/2+(20*#Scale#)*(1-#TweenNode1#))
Y=r
Shape=Rectangle 0,0,#MediaW#,#H#,#R# | StrokeWidth #Stroke# | Extend This | Stroke Color #BorderColor#,(#BorderOpacity#*#TweenNode1#) | Extend This
This=Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#)
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #SecondaryColor#,#PrimaryOpacity#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #PrimaryColor#,(#PrimaryOpacity#*#*TweenNode1*#)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Group=Music | Standard | Animated
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#MediaW#,#H#,#R# | StrokeWidth 0 | Fill Color 255,255,255,(255*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1



[MediaBar]
Meter=Shape
X=(#P#/2)
Y=(#P#/2)
Shape=Rectangle 0,0,(#MediaW#-#P#),(#MediaW#-#P#),#R# | StrokeWidth 0 | Fill Color #AccentColor1#,50
This=90 | #AccentColor1# ; 0 | #AccentColor2# ; 1
Shape2=Rectangle 0,0,(#MediaW#-#P#),(#MediaW#-#P#),#R# | StrokeWidth 0 | Fill LinearGradient This
Shape3=Path Path | StrokeWidth 0
Path=0,0 | LineTo ((#MediaW#-#P#)*[#PlayerType#Progress]/100),0 | LineTo ((#MediaW#-#P#)*[#PlayerType#Progress]/100+0*#Scale#),(#MediaW#-#P#) | LineTo 0,(#MediaW#-#P#) | ClosePath 1
Shape4=Combine Shape2 | Intersect Shape3
DynamicVariables=1
LeftMouseUpAction=[!SetOption PosCalc Formula "($MouseX$/(#MediaW#-#P#)*100)"][!UpdateMeasure PosCalc]
Group=Music | Standard
Container=MediaC
UpdateDivider=1
[PosCalc]
Measure=calc
OnUpdateAction=[!CommandMeasure state#NowPlayingMedia# "SetPosition [PosCalc]"][!UpdateMeasure #PlayerType#Progress]
Disabled=1
DynamicVariables=1
UpdateDivider=-1
Group=NUOL

[MediaImage]
Meter=Image
X=#P#
Y=#P#
W=(#MediaW#-2*#P#)
H=(#MediaW#-2*#P#)
PreserveAspectRatio=2
MaskImageName=#@#Images\Mask.png
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | Standard | MediaImage
Container=MediaC

[MediaTrack]
Meter=String
MeterStyle=String:S
X=((#MediaW#-2*#P#)/2)r
Y=(#P#/2+10*#scale#)R
W=(#MediaW#-#P#)
ClipString=1
Text=[#PlayerType#Track]
DynamicVariables=1
FontWeight=650
StringAlign=Center
FontSize=(12*#Scale#)
Group=Music | Standard
Container=MediaC

[MediaArtist]
Meter=String
MeterStyle=String:S
X=r
Y=(2*#scale#)R
ClipString=1
StringAlign=Center
Text=[#PlayerType#Artist]
FontColor=#FontColor2#
DynamicVariables=1
Group=Music | Standard
Container=MediaC

[MediaPrev]
Meter=Image
X=(#MediaW#/2-(20*3*#scale#+#P#)/2)
Y=(#H#-#P#/2-20*#scale#)
ImageName=#@#Icons\Previous.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Previous"]#MHC_Reset#
MeterStyle=Sec.BottomButton:S

[MediaPlayPause]
Meter=Image
MeterStyle=Sec.BottomButton:S
[Play]
ImageName=#@#Icons\Play.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Pause"][!UpdateMeter MediaPlayPause]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "PlayPause"]
[Pause]
ImageName=#@#Icons\Pause.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Play"][!UpdateMeter MediaPlayPause]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "PlayPause"]

[MediaNext]
Meter=Image
ImageName=#@#Icons\Next.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Next"]#MHC_Reset#
MeterStyle=Sec.BottomButton:S

[MediaHoverCircle]
Meter=Shape
Shape=Ellipse (10*#scale#),(10*#scale#),(16*#Scale#) | StrokeWidth 0 | Extend Stroke
Stroke=Fill Color #FontColor#,20
Container=MediaC
Hidden=1

[MediaHeart]
Meter=String
X=-10
Y=-10
[MediaShuffle]
Meter=String
X=-10
Y=-10
[MediaRepeat]
Meter=String
X=-10
Y=-10

; ; ---------------------------------------------------------------------------- ;
; ;                                    Special                                   ;
; ; ---------------------------------------------------------------------------- ;

; ; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=(#Stroke#/2+20*#Scale#+#MediaW#)
Y=#Stroke#/2
Shape=Rectangle 0,0,#LocksW#,#H#,#R# | StrokeWidth #Stroke# | Extend This | Stroke Color #BorderColor#,#BorderOpacity#
Group=Locks | Special
MeterStyle=LocksB.Off:S
[LocksB.On:S]
This=Fill Color #SecondaryColor#,#PrimaryOpacity#
[LocksB.Off:S]
This=Fill Color #PrimaryColor#,#PrimaryOpacity#
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#LocksW#,#H#,#R# | StrokeWidth 0
Group=Locks | Special

[LocksIcon]
Meter=String
MeterStyle=String:S
X=(#LocksW#/2)
Y=(26*#scale#)
FontFace=Material Icons Round
StringAlign=Center
FontSize=(16*#scale#)
Group=Locks | Special
Container=LocksC
Hidden=1
[LocksText]
Meter=String
MeterStyle=String:S
X=([LocksIcon:W]/2)r
Y=(#P#/2)R
Angle=(Rad(90))
Group=Locks | Special
Container=LocksC
DynamicVariables=1
Hidden=1
