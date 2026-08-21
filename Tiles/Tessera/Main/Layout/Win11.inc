[Variables]
W=(#Width#*#scale#)
VolumeH=(50*#Scale#)
MediaH=(175*#Scale#)
LocksH=(50*#Scale#)
P=(15*#Scale#)

MHC_Reset=[!SetOption MediaHoverCircle Stroke "Stroke Color #FontColor#,20"][!UpdateMeter MediaHoverCircle][!Redraw]

SliderRotation=h
SliderEndAction=[!SetOption VolumeBar MeterStyle "VolumeBar:0"][!UpdateMeter Volumebar][!Redraw]

[SliderStart]
Formula=[VolumeBar:X]
[SliderEnd]
Formula=((#Width#-120)*#scale#)

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Sec.BottomButton:S]
X=(#P#*2)R
Y=r
W=(20*#scale#)
H=(20*#scale#)
Grayscale=1
ImageTint=#FontColor#
DynamicVariables=1
MouseOverAction=[!SetOption MediaHoverCircle X "([#CURRENTSECTION#:X]-1)"][!SetOption MediaHoverCircle Y "([#CURRENTSECTION#:Y]-#VolumeH#-1)"][!ShowMeter MediaHoverCircle][!UpdateMeter MediaHoverCircle][!Redraw]
MouseLeaveAction=[!HideMeter MediaHoverCircle][!Redraw]
LeftMouseDownAction=[!SetOption MediaHoverCircle Stroke "Stroke Color #FontColor#,150"][!UpdateMeter MediaHoverCircle][!Redraw]
Group=Music | Standard
Container=MediaC

[SysColor]
Measure=Plugin
Plugin=SysColor
ColorType=Aero
OnUpdateAction=[!SetVariable AccentColor "[SysColor]"]
DynamicVariables=1
DisplayType=RGB
Disabled=(1-#SysAccent#)

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

[FrostedGlass]
Measure=Plugin
Plugin=FrostedGlass
Type=#Blur#
Corner=#BlurCorner#
Border=All

[SysColor]
Measure=Plugin
Plugin=SysColor
ColorType=Aero
OnUpdateAction=[!SetVariable AccentColor "[SysColor]"]
DynamicVariables=1
DisplayType=RGB
Disabled=(1-#SysAccent#)

; --------------------------------- VolumeBar -------------------------------- ;

[StrokeB]
Meter=Shape
X=#Border#
Y=#Border#
Shape=Rectangle 0,0,(#W#),(#VolumeH#+#MediaH#*#MusicVisible#*#TweenNode1#),#StaticWin11Corner# | Stroke Color #FontColor#,50 | StrokeWidth (#Border#*#Scale#) | Fill Color 0,0,0,0
DynamicVariables=1
Group=Standard | Music | Animated
Hidden=(1-#Border#)

[VolumeB]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#VolumeH#,#StaticWin11Corner# | StrokeWidth 0 | Fill Color #PrimaryColor#,#PrimaryOpacity#
Group=Standard
DynamicVariables=1
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#VolumeH# | StrokeWidth 0
Group=Standard



[VolumeIcon]
Meter=String
MeterStyle=String:S
X=(30*#scale#)
Y=(#VolumeH#/2)
FontFace=Segoe Fluent Icons
StringAlign=CenterCenter
FontSize=(16*#scale#)
Group=Dynamic | Standard
Container=VolumeC
[VolumeBar]
Meter=Shape
X=(60*#Scale#)
Y=(#VolumeH#/2)
Shape=Line 0,0,([SliderEnd]),0 | StrokeWidth (4*#Scale#) | StrokeStartCap #cap# | StrokeEndCap #cap# | Extend BackStroke
Shape2=Line 0,0,([SliderEnd]*[#CurrentSlider#:]/100),0 | StrokeWidth (4*#Scale#) | StrokeStartCap #cap# | StrokeEndCap #cap# | Extend Stroke
Shape3=Line 0,0,([SliderEnd]),0 | StrokeWidth (20*#scale#) | Stroke Color 0,0,0,1
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

[VolumeString]
Meter=String
MeterStyle=String:S
X=(#W#-30*#scale#)
Y=(#VolumeH#/2)
StringAlign=CenterCenter
FontSize=(12*#Scale#)
Text=[&Func:volumeLevel()]
DynamicVariables=1
Group=Dynamic | Standard
Container=VolumeC

; ------------------------------ MediaPlayerBar ------------------------------ ;

[MediaB]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,(#W#),(#VolumeH#),(#StaticWin11Corner#+1) | StrokeWidth 0 | Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#)
Shape2=Rectangle 0,0,#W#,((#VolumeH#+#MediaH#)*#TweenNode1#),(#StaticWin11Corner#) | StrokeWidth 0
Shape3=Combine Shape | XOR Shape2
Group=Music | Standard | Animated
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=(#VolumeH#)r
Shape=Rectangle 0,0,#W#,(#MediaH#*#TweenNode1#),#StaticWin11Corner# | StrokeWidth 0 | Fill color 255,255,255,(255*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1

[MediaDivider]
Meter=Shape
X=(#P#)
Y=0
Shape=Line 0,0,(#W#-#P#*2),0 | StrokeWidth (1*#Scale#) | Stroke Color #FontColor#,50 | StrokeStartCap #Cap# | StrokeEndCap #Cap#
Container=MediaC

[MediaTrack]
Meter=String
MeterStyle=String:S
X=(#P#)
Y=(#P#)R
W=(#W#-(80*#Scale#)-#P#*3)
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
W=(#W#-(80*#Scale#)-#P#*3)
ClipString=1
Text=[#PlayerType#Artist]
DynamicVariables=1
Group=Music | Standard
Container=MediaC

[MediaPrev]
Meter=Image
X=(#W#/2-(20*3*#scale#+#P#*2*2)/2)
Y=(60*#scale#)R
ImageName=#@#Icons\FluentPrevious.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Previous"]#MHC_Reset#
MeterStyle=Sec.BottomButton:S

[MediaPlayPause]
Meter=Image
MeterStyle=Sec.BottomButton:S
[Play]
ImageName=#@#Icons\FluentPlay.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Pause"][!UpdateMeter MediaPlayPause]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "PlayPause"]
[Pause]
ImageName=#@#Icons\FluentPause.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Play"][!UpdateMeter MediaPlayPause]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "PlayPause"]

[MediaNext]
Meter=Image
ImageName=#@#Icons\FluentNext.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Next"]#MHC_Reset#
MeterStyle=Sec.BottomButton:S

[MediaImage]
Meter=Image
X=(#W#-(80*#Scale#)-#P#)
Y=(#P#)
W=(80*#Scale#)
H=(80*#Scale#)
PreserveAspectRatio=2
MaskImageName=#@#Images\Mask.png
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | Standard | MediaImage
Container=MediaC

[MediaRepeat]
Meter=String

[MediaHoverCircle]
Meter=Shape
Shape=Ellipse (10*#scale#),(10*#scale#),(20*#Scale#) | StrokeWidth 1 | Fill Color 0,0,0,0 | Extend Stroke
Stroke=Stroke Color #FontColor#,20
Container=MediaC
Hidden=1

; ---------------------------------------------------------------------------- ;
;                                    Special                                   ;
; ---------------------------------------------------------------------------- ;

; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=#Border#
Y=#Border#
Shape=Rectangle 0,0,#W#,#LocksH#,#StaticWin11Corner# | StrokeWidth (#Border#*#Scale#) | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #FontColor#,50
Group=Locks | Special
Hidden=1
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#LocksH#,#StaticWin11Corner# | StrokeWidth 0
Group=Locks | Special
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
