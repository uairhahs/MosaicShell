[Variables]
W=(#Width#*#scale#)
VolumeH=(70*#Scale#)
MediaH=(70*#Scale#)
LocksH=(70*#Scale#)
XPad=(40*#scale#)
P=(15*#Scale#)

MHC_Reset=[!SetOption MediaHoverCircle Stroke "Stroke Color #FontColor#,20"][!UpdateMeter MediaHoverCircle][!Redraw]

SliderRotation=h
SliderEndAction=[!SetOption VolumeBar MeterStyle "VolumeBar:0"][!UpdateMeter Volumebar][!Redraw]

[SliderStart]
Formula=[VolumeBar:X]
[SliderEnd]
Formula=(#W#-#XPAD#-(#XPAD#+(#MediaH#-#P#*2)+#P#))

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Sec.BottomButton:S]
X=(#P#)R
Y=r
W=(20*#scale#)
H=(20*#scale#)
Grayscale=1
ImageTint=#FontColor#
DynamicVariables=1
Group=Music | Standard
Container=MediaC

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[mVolume]
IfCondition=(mVolume >= 0) && (mVolume < 20)
IfTrueAction=[!SetOption VolumeIcon Text "[\xE992]"]
IfCondition2=(mVolume >= 20) && (mVolume < 50)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xE993]"]
IfCondition3=(mVolume >= 50) && (mVolume < 90)
IfTrueAction3=[!SetOption VolumeIcon Text "[\xE994]"]
IfCondition4=(mVolume >= 90)
IfTrueAction4=[!SetOption VolumeIcon Text "[\xE995]"]
IfCondition5=(mVolume = -1)
IfTrueAction5=[!SetOption VolumeIcon Text "[\xE74F]"]
IfConditionMode=1
[mBrightness]
IfCondition=(mBrightness >= 0) && (mBrightness < 50)
IfTrueAction=[!SetOption VolumeIcon Text "[\xEC8A]"]
IfCondition2=(mBrightness >= 50) && (mBrightness <= 100)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xE706]"]
IfConditionMode=1

; --------------------------------- VolumeBar -------------------------------- ;

[VolumeB]
Meter=Shape
X=0
Y=(#MediaH#+#P#)
Shape=Rectangle 0,0,#W#,#VolumeH#,(#VolumeH#/2) | StrokeWidth 0 | Fill Color #PrimaryColor#,#PrimaryOpacity#
Group=Standard | Animated
DynamicVariables=1
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#VolumeH# | StrokeWidth 0 | Fill Color 255,255,255,(255*#TweenNode1#)
Group=Standard | Animated
DynamicVariables=1



[VolumeIcon]
Meter=String
MeterStyle=String:S
X=(#XPAD#)
Y=(#VolumeH#/2)
FontFace=Segoe MDL2 Assets
StringAlign=LeftCenter
FontSize=(18*#scale#)
Group=Dynamic | Standard
Container=VolumeC
[VolumeBar]
Meter=Shape
X=(#XPAD#+(#MediaH#-#P#*2)+#P#)
Y=(#VolumeH#/2)
Shape=Line 0,-1,([SliderEnd]),-1 | StrokeWidth (8*#Scale#) | StrokeStartCap Round | StrokeEndCap Round | Extend BackStroke
Shape2=Line 0,-1,([SliderEnd]*[#CurrentSlider#:]/100),-1 | StrokeWidth (8*#Scale#) | StrokeStartCap Round | StrokeEndCap Round | Extend Stroke
Shape3=Line 0,-1,([SliderEnd]),-1 | StrokeWidth (20*#scale#) | Stroke Color 0,0,0,1
LeftMouseDownAction=[!SetOption VolumeBar MeterStyle "VolumeBar:1"][!UpdateMeter Volumebar][!Redraw][!CommandMeasure VolumeMouse "Start"]
DynamicVariables=1
Container=VolumeC
Group=Dynamic | Standard
MeterStyle=VolumeBar:0
[VolumeBar:0]
BackStroke=Stroke Color #FontColor2#
Stroke=Stroke Color #AccentColor#
[VolumeBar:1]
BackStroke=Stroke Color #FontColor2#,150
Stroke=Stroke Color #AccentColor#

; ------------------------------ MediaPlayerBar ------------------------------ ;

[MediaB]
Meter=Shape
Shape=Rectangle 0,0,(#W#),(#MediaH#),(#VolumeH#/2) | StrokeWidth 0 | Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#) | Scale (0.5+0.5*#TweenNode1#),(0.5+0.5*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,(#W#),(#MediaH#),(#VolumeH#/2) | StrokeWidth 0 | Fill Color #PrimaryColor#,(255*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1

[MediaImage]
Meter=Image
X=(#XPAD#)
Y=(#P#)
W=(#MediaH#-#P#*2)
H=(#MediaH#-#P#*2)
MaskImageName=#@#Images\CircleMask.png
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | Standard | MediaImage
Container=MediaC

[MediaTrack]
Meter=String
MeterStyle=String:S
X=(#P#)R
Y=(#P#)
W=(#W#-20*#Scale#*2-#P#-#XPAD#-(#XPAD#+(#MediaH#-#P#*2)+#P#))
ClipString=1
Text=[#PlayerType#Track]
DynamicVariables=1
FontSize=(12*#Scale#)
FontWeight=650
Group=Music | Standard
Container=MediaC

[MediaArtist]
Meter=String
MeterStyle=String:S
X=r
Y=(5*#Scale#)R
W=(#W#-20*#Scale#*2-#P#-#XPAD#-(#XPAD#+(#MediaH#-#P#*2)+#P#))
ClipString=1
Text=[#PlayerType#Artist]
DynamicVariables=1
Group=Music | Standard
Container=MediaC

[MediaPlayPause]
Meter=Image
X=(#W#-20*#Scale#*2-#P#-#XPAD#)
Y=(#MediaH#/2-10*#scale#)
MeterStyle=Sec.BottomButton:S
[Play]
ImageName=#@#Icons\Play.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Pause"][!UpdateMeter MediaPlayPause][!CommandMeasure state#NowPlayingMedia# "PlayPause"]
[Pause]
ImageName=#@#Icons\Pause.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Play"][!UpdateMeter MediaPlayPause][!CommandMeasure state#NowPlayingMedia# "PlayPause"]

[MediaNext]
Meter=Image
ImageName=#@#Icons\Next.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Next"]
MeterStyle=Sec.BottomButton:S

[MediaRepeat]
Meter=String

; ---------------------------------------------------------------------------- ;
;                                    Special                                   ;
; ---------------------------------------------------------------------------- ;

; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=0
Y=(#MediaH#+#P#)
Shape=Rectangle 0,0,#W#,#LocksH#,(#VolumeH#/2) | StrokeWidth 0 | Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#) | Scale (0.5+0.5*#TweenNode1#),(0.5+0.5*#TweenNode1#)
Group=Locks | Special | Animated
DynamicVariables=1
Hidden=1
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#LocksH#,(#VolumeH#/2) | StrokeWidth 0 | Fill Color #PrimaryColor#,(255*#TweenNode1#)
Group=Locks | Special | Animated
DynamicVariables=1
Hidden=1



[LocksText]
Meter=String
MeterStyle=String:S
X=(#W#/2)
Y=(#LocksH#/2)
FontSize=(12*#Scale#)
StringAlign=CenterCenter
Group=Locks | Special
Container=LocksC
Hidden=1
