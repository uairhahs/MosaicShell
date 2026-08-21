[Variables]
W=(#Width#*#scale#)
VolumeH=(50*#Scale#)
MediaH=(190*#Scale#)
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
MouseOverAction=[!SetOption MediaHoverCircle X "([#CURRENTSECTION#:X])"][!SetOption MediaHoverCircle Y "([#CURRENTSECTION#:Y]-#VolumeH#-20*#Scale#)"][!ShowMeter MediaHoverCircle][!UpdateMeter MediaHoverCircle][!Redraw]
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
Shape=Rectangle 0,0,#W#,#VolumeH#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #SecondaryColor#,#SecondaryOpacity#
Group=Standard
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#VolumeH#,#R# | StrokeWidth 0
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
X=(60*#Scale#)
Y=(#VolumeH#/2)
Shape=Line 0,0,([SliderEnd]),0 | StrokeWidth (2*#Scale#) | StrokeStartCap #cap# | StrokeEndCap #cap# | Extend BackStroke
Shape2=Line 0,0,([SliderEnd]*[#CurrentSlider#:]/100),0 | StrokeWidth (2*#Scale#) | StrokeStartCap #cap# | StrokeEndCap #cap# | Extend Stroke
Shape3=Ellipse ([SliderEnd]*[#CurrentSlider#:]/100),0,(#DotSize#/2*#scale#) | StrokeWidth 0 | Fill Color #AccentColor#
Shape4=Line 0,0,([SliderEnd]),0 | StrokeWidth (20*#scale#) | Stroke Color 0,0,0,1
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
Stroke=Stroke Color #AccentColor#,100

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
X=r
Y=(20*#Scale#)R
Shape=Rectangle 0,0,#W#,#MediaH#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#) | Stroke Color #SecondaryColor#,(#SecondaryOpacity#*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,(#MediaH#*#TweenNode1#),#R# | StrokeWidth 0 | Fill color 255,255,255,(255*#TweenNode1#)
Group=Music | Standard | Animated
DynamicVariables=1


[MediaImageFade]
Meter=Image
W=(#W#)
H=(#MediaH#)
PreserveAspectRatio=2
MaskImageName=#@#Images\FadeMask.png
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | Standard | MediaImage
Container=MediaC

[MusicIcon]
Meter=String
MeterStyle=String:S
X=(#P#)
Y=(#P#)
Text=[\xe3a1]
FontFace=Material Icons Round
FontSize=(12*#Scale#)
Group=Music | Standard
Container=MediaC

[MediaName]
Meter=String
MeterStyle=String:S
X=(#P#*3)
Y=r
W=(#W#-(#MediaH#+2*#P#))
ClipString=1
Text=[#PlayerType#Player]
DynamicVariables=1
FontSize=(9*#Scale#)
Group=Music | Standard
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

[ProgBar]
Meter=Shape
X=#P#
Y=(110*#scale#)
Shape=Line 0,0,(#W#-#P#*2),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | Extend BackStroke | StrokeStartCap Round | StrokeEndCap Round
Shape2=Line 0,0,((#W#-#P#*2)*[#PlayerType#Progress]/100),0 | StrokeWidth (2*#scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend Stroke
Shape3=Ellipse ((#W#-#P#*2)*[#PlayerType#Progress]/100),0,(#DotSize#/4) | strokeWidth 0 | Fill Color #AccentColor# | Scale #Scale#,#Scale#
Shape4=Line 0,0,(#W#-#P#*2),0 | StrokeWidth (20*#Scale#) | Fill Color 0,0,0,1 | Stroke Color 0,0,0,1
LeftMouseDownAction=[!SetOption ProgBar MeterStyle "ProgBar:1"][!UpdateMeter ProgBar][!Redraw]
LeftMouseUpAction=[!SetOption ProgBar MeterStyle "ProgBar:0"][!UpdateMeter ProgBar][!Redraw][!SetOption PosCalc Formula "($MouseX$/(#W#-#P#*2)*100)"][!UpdateMeasure PosCalc]
DynamicVariables=1
UpdateDivider=1
Group=Music | Standard
Container=MediaC
MeterStyle=ProgBar:0
[ProgBar:0]
BackStroke=Stroke Color #FontColor2#
Stroke=Stroke Color #AccentColor#
[ProgBar:1]
BackStroke=Stroke Color #FontColor2#,150
Stroke=Stroke Color #AccentColor#,100
[PosCalc]
Measure=calc
OnUpdateAction=[!CommandMeasure state#NowPlayingMedia# "SetPosition [PosCalc]"][!UpdateMeasure #PlayerType#Progress]
Disabled=1
DynamicVariables=1
UpdateDivider=-1
Group=NUOL
[MediaCurrent]
Meter=String
MeterStyle=String:S
X=r
Y=R
FontColor=#FontColor2#
DynamicVariables=1
UpdateDivider=1
FontSize=(8*#scale#)
MeasureName=#PlayerType#CurrentPosition
Group=Music | Standard
Container=MediaC
[MediaLength]
Meter=String
MeterStyle=String:S
X=(#W#-#P#*2)r
Y=r
FontColor=#FontColor2#
StringAlign=Right
DynamicVariables=1
FontSize=(8*#scale#)
MeasureName=#PlayerType#Length
Group=Music | Standard
Container=MediaC

[MediaHeart]
Meter=Image
X=(#W#/2-(20*5*#scale#+#P#*2*4)/2)
Y=(20*#scale#)R
MeterStyle=Sec.BottomButton:S
[FullHeart]
ImageName=#@#Icons\HeartFull.png
LeftMouseUpAction=[!SetOption #CURRENTSECTION# MeterStyle "Sec.BottomButton:S | EmptyHeart"][!UpdateMeter #CURRENTSECTION#]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "ToggleThumbsDown"]
[EmptyHeart]
ImageName=#@#Icons\Heart.png
LeftMouseUpAction=[!SetOption #CURRENTSECTION# MeterStyle "Sec.BottomButton:S | Fullheart"][!UpdateMeter #CURRENTSECTION#]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "ToggleThumbsUp"]

[MediaPrev]
Meter=Image
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

[MediaShuffle]
Meter=Image
ImageName=#@#Icons\Shuffle.png
MeterStyle=Sec.BottomButton:S
[Shuffle0]
ImageAlpha=150
LeftMouseUpAction=[!SetOption MediaShuffle MeterStyle "Sec.BottomButton:S | Shuffle1"][!UpdateMeter MediaShuffle]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "Shuffle"]
[Shuffle1]
ImageAlpha=255
LeftMouseUpAction=[!SetOption MediaShuffle MeterStyle "Sec.BottomButton:S | Shuffle0"][!UpdateMeter MediaShuffle]#MHC_Reset#[!CommandMeasure state#NowPlayingMedia# "Shuffle"]


[MediaImage]
Meter=Image
X=(#W#-(80*#Scale#)-#P#)
Y=#P#
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
