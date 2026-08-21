[Variables]
W=(#Width#*#scale#)
VolumeH=(50*#Scale#)
MediaH=(150*#Scale#)
LocksH=(50*#Scale#)
P=(15*#Scale#)

SliderRotation=h

[SliderStart]
Formula=[VolumeBar:X]
[SliderEnd]
Formula=((#Width#-120)*#scale#)

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
[mBrightness]
IfCondition=(mBrightness >= 0) && (mBrightness < 50)
IfTrueAction=[!SetOption VolumeIcon Text "[\xEC8A]"]
IfCondition2=(mBrightness >= 50) && (mBrightness <= 100)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xE706]"]
IfConditionMode=1

; --------------------------------- VolumeBar -------------------------------- ;

[VolumeB]
Meter=Shape
X=1
Y=1
Shape=Rectangle 0,0,(#W#),#VolumeH#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #SecondaryColor#,#SecondaryOpacity#
Group=Standard
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,(#W#),#VolumeH#,#R# | StrokeWidth 0
Group=Standard



[VolumeIcon]
Meter=String
MeterStyle=String:S
X=#P#
Y=(#VolumeH#/2)
FontFace=Segoe MDL2 Assets
StringAlign=LeftCenter
FontSize=(16*#scale#)
Group=Dynamic | Standard
Container=VolumeC
[VolumeBar]
Meter=Shape
X=#P#R
Y=(#VolumeH#/2)
Shape=Line 0,0,([SliderEnd]),0 | StrokeWidth (2*#scale#*#TweenNode1#) | Stroke Color #FontColor#,150 | StrokeStartCap #cap# | StrokeEndCap #cap#
Shape2=Line 0,0,([SliderEnd]*[#CurrentSlider#:]/100),0 | StrokeWidth (2*#scale#*#TweenNode1#) | Stroke Color #AccentColor# | StrokeStartCap #cap# | StrokeEndCap #cap#
Shape3=Line 0,0,([SliderEnd]),0 | StrokeWidth (20*#scale#) | Stroke Color 0,0,0,1
Shape4=Ellipse ([SliderEnd]*[#CurrentSlider#:]/100),0,(#DotSize#/2*#scale#*#TweenNode1#) | StrokeWidth 0 | Fill Color #AccentColor#
LeftMouseDownAction=[!CommandMeasure VolumeMouse "Start"]
DynamicVariables=1
Container=VolumeC
Group=Dynamic | Standard | Animated
[VolumeString]
Meter=String
MeterStyle=String:S
X=(#W#-30*#scale#)
Y=(#VolumeH#/2)
StringAlign=CenterCenter
Text=[&Func:volumeLevel()]
DynamicVariables=1
Group=Dynamic | Standard
Container=VolumeC

; ------------------------------ MediaPlayerBar ------------------------------ ;

[MediaB]
Meter=Shape
X=1
Y=(20*#Scale#)R
Shape=Rectangle (#W#),0,(-#W#*#TweenNode1#),#MediaH#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#) | Stroke Color #SecondaryColor#,(#SecondaryOpacity#*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,(#W#*#TweenNode1#),#MediaH#,#R# | StrokeWidth 0 | Fill Color 255,255,255,(255*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1



[MediaImage]
Meter=Image
X=#P#
Y=#P#
W=(#MediaH#-2*#P#)
H=(#MediaH#-2*#P#)
PreserveAspectRatio=2
MaskImageName=#@#Images\Mask.png
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | Standard | MediaImage
Container=MediaC

[MediaTrack]
Meter=String
MeterStyle=String:S
X=(#P#)R
Y=r
W=(#W#-(#MediaH#+2*#P#))
ClipString=1
Text=[#PlayerType#Track]
DynamicVariables=1
FontSize=(12*#Scale#)
Group=Music | Standard
Container=MediaC

[MediaArtist]
Meter=String
MeterStyle=String:S
X=r
Y=(10*#scale#)R
W=(#W#-(#MediaH#+2*#P#))
ClipString=1
Text=[#PlayerType#Artist]
FontColor=#FontColor2#
DynamicVariables=1
Group=Music | Standard
Container=MediaC

[MediaProgress]
Meter=String
MeterStyle=String:S
X=r
Y=R
W=(#W#-(#MediaH#+2*#P#))
ClipString=1
Text=[#PlayerType#CurrentPosition] / [#PlayerType#Length]
FontColor=#FontColor2#
FontSize=(8*#Scale#)
DynamicVariables=1
UpdateDivider=1
Group=Music | Standard
Container=MediaC

[MediaHeart]
Meter=Image
X=r
Y=(10*#scale#)R
W=(20*#scale#)
H=(20*#scale#)
Padding=(5*#scale#),(5*#scale#),(5*#scale#),(5*#scale#)
Grayscale=1
ImageTint=#FontColor#
DynamicVariables=1
Group=Music | Standard
Container=MediaC
[FullHeart]
ImageName=#@#Icons\HeartFull.png
LeftMouseUpAction=[!SetOption #CURRENTSECTION# MeterStyle EmptyHeart][!UpdateMeter #CURRENTSECTION#][!Redraw][!CommandMeasure state#NowPlayingMedia# "ToggleThumbsDown"]
[EmptyHeart]
ImageName=#@#Icons\Heart.png
LeftMouseUpAction=[!SetOption #CURRENTSECTION# MeterStyle Fullheart][!UpdateMeter #CURRENTSECTION#][!Redraw][!CommandMeasure state#NowPlayingMedia# "ToggleThumbsUp"]

[MediaPlayPause]
Meter=String
[MediaShuffle]
Meter=String
[MediaRepeat]
Meter=String

; ---------------------------------------------------------------------------- ;
;                                    Special                                   ;
; ---------------------------------------------------------------------------- ;

; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=1
Y=1
Shape=Rectangle 0,0,#W#,#LocksH#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #SecondaryColor#,#SecondaryOpacity#
Group=Locks | Special
Hidden=1
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#LocksH#,#R# | StrokeWidth 0
Group=Locks | Special
Hidden=1

[LocksIcon]
Meter=String
MeterStyle=String:S
X=#P#
Y=(26*#scale#)
FontFace=Material Icons Round
StringAlign=LeftCenter
FontSize=(16*#scale#)
Group=Locks | Special
Container=LocksC
Hidden=1
[LocksText]
Meter=String
MeterStyle=String:S
X=(#P#)R
Y=r
StringAlign=LeftCenter
Group=Locks | Special
Container=LocksC
Hidden=1
