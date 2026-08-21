[Variables]
H=(#Height#*#scale#)
VolumeW=(#H#)
MediaW=(#H#)
LocksW=(#H#)
P=(10*#Scale#)
SeparateMedia=1

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[FrostedGlass]
Measure=Plugin
Plugin=FrostedGlass
Type=#Blur#
Corner=#BlurCorner#
Border=All

[mVolume]
IfCondition=(mVolume >= 0) && (mVolume < 33)
IfTrueAction=[!SetOption VolumeIcon Text "[\xe04e]"]
IfCondition2=(mVolume >= 33) && (mVolume < 66)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xe04d]"]
IfCondition3=(mVolume >= 66)
IfTrueAction3=[!SetOption VolumeIcon Text "[\xe050]"]
IfCondition4=(mVolume = -1)
IfTrueAction4=[!SetOption VolumeIcon Text "[\xe04f]"]
IfConditionMode=1

[mBrightness]
IfCondition=(mBrightness >= 0) && (mBrightness < 33)
IfTrueAction=[!SetOption VolumeIcon Text "[\xe1ad]"]
IfCondition2=(mBrightness >= 33) && (mBrightness < 66)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xe3a9]"]
IfCondition3=(mBrightness >= 66)
IfTrueAction3=[!SetOption VolumeIcon Text "[\xe3ac]"]
IfConditionMode=1

; --------------------------------- VolumeBar -------------------------------- ;

[VolumeB]
Meter=Shape
X=1
Y=1
Shape=Rectangle 0,0,#VolumeW#,#H#,#StaticWin11Corner# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #BorderColor#,#BorderOpacity#
Group=Standard
MouseScrollUpAction=[!CommandMeasure "mVolume" "ChangeVolume 2"][!UpdateMeasure mVolume]
MouseScrollDownAction=[!CommandMeasure "mVolume" "ChangeVolume -2"][!UpdateMeasure mVolume]
DynamicVariables=1
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#VolumeW#,#H#,#StaticWin11Corner#  | StrokeWidth 0
Group=Standard
DynamicVariables=1

[VolumeIcon]
Meter=String
MeterStyle=String:S
X=(#VolumeW#/2)
Y=(#H#/2)
FontFace=Material Icons Round
StringAlign=CenterCenter
FontSize=(34*#scale#*#TweenNode1#)
Group=Dynamic | Standard | Animated
FontColor=#FontColor#,(255*(Clamp([#CurrentSlider#:],25,100))/100)
DynamicVariables=1
InlineSetting=Shadow | 0 | 0 | 5 | #GlowColor#,#GlowOpacity#
Container=VolumeC

[VolumeString]
Meter=String
MeterStyle=String:S
X=(#VolumeW#/2)
Y=(#p#*3)r
StringAlign=Center
FontWeight=650
FontSize=(10*#scale#*#TweenNode1#)
Text=[&Func:volumeLevel()]
DynamicVariables=1
Group=Dynamic | Standard | Animated
Container=VolumeC
InlineSetting=Shadow | 0 | 0 | 5 | #GlowColor#,#GlowOpacity#
FontFace=#FontFace#

; ---------------------------------------------------------------------------- ;
;                                  End of port                                 ;
; ---------------------------------------------------------------------------- ;

; ; --------------------------------- Media -------------------------------- ;

[MediaB]
Meter=Shape
X=1
Y=1
Shape=Rectangle 0,0,#MediaW#,#H#,#StaticWin11Corner# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #BorderColor#,#BorderOpacity#
Group=Music | Standard
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#MediaW#,#H#,#StaticWin11Corner# | StrokeWidth 0
Group=Music | Standard
DynamicVariables=1

[MediaPlayPause]
Meter=Image
W=(#MediaW#)
H=(#MediaW#)
PreserveAspectRatio=2
ImageAlpha=#MediaImageAlpha#
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | Standard | MediaImage
Container=MediaC
[Play]
Greyscale=1
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle Pause][!UpdateMeter MediaPlayPause][!Redraw][!CommandMeasure state#NowPlayingMedia# "PlayPause"]
[Pause]
Greyscale=0
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle Play][!UpdateMeter MediaPlayPause][!Redraw][!CommandMeasure state#NowPlayingMedia# "PlayPause"]

[MediaTrack]
Meter=String
MeterStyle=String:S
X=(#MediaW#/2)
Y=#P#
W=(#MediaW#-#P#*2)
FontSize=(Clamp(14*#scale#, 0, 14))
ClipString=1
StringAlign=Center
Text=[#PlayerType#Track]
DynamicVariables=1
Group=Music | Standard
Container=MediaC
[MediaArtist]
Meter=String
MeterStyle=String:S
X=r
Y=(#P#/2)R
W=(#MediaW#-#P#*2)
FontSize=(Clamp(10*#scale#, 0, 10))
ClipString=1
StringAlign=Center
Text=[#PlayerType#Artist]
FontWeight=600
DynamicVariables=1
Group=Music | Standard
Container=MediaC


[Previous]
Meter=String
[Next]
Meter=String
[MediaHeart]
Meter=String
[MediaShuffle]
Meter=String


[MediaCurrentPosition]
Meter=String
MeterStyle=String:S
X=#P#
Y=(#H#-#P#*2)
W=(#MediaW#-#P#*2)
FontSize=(8*#scale#)
ClipString=1
StringAlign=LeftBottom
Text=[#PlayerType#CurrentPosition]
DynamicVariables=1
UpdateDivider=1
FontColor=#FontColor#,100
Group=Music | Standard
Container=MediaC
[MediaLength]
Meter=String
MeterStyle=String:S
X=(#MediaW#-#P#)
Y=(#H#-#P#*2)
W=(#MediaW#-#P#*2)
FontSize=(8*#scale#)
ClipString=1
StringAlign=RightBottom
Text=[#PlayerType#Length]
DynamicVariables=1
FontColor=#FontColor#,100
Group=Music | Standard
Container=MediaC
[ProgBar]
Meter=Shape
Y=(#H#-5*#scale#)
Shape=Rectangle 0,0,(#MediaW#),(5*#scale#) | StrokeWidth 0 | Extend BackFill
Shape2=Rectangle 0,0,((#MediaW#)*[#PlayerType#Progress]/100),(5*#scale#) | StrokeWidth 0 | Extend Fill
This=0 | #AccentCOlor1# ; 0.0 | #AccentColor2# ; 1.0
LeftMouseDownAction=[!SetOption ProgBar MeterStyle "ProgBar:1"][!UpdateMeter ProgBar][!Redraw]
LeftMouseUpAction=[!SetOption ProgBar MeterStyle "ProgBar:0"][!UpdateMeter ProgBar][!Redraw][!SetOption PosCalc Formula "($MouseX$/(#MediaW#)*100)"][!UpdateMeasure PosCalc]
DynamicVariables=1
UpdateDivider=1
Group=Music | Standard
Container=MediaC
MeterStyle=ProgBar:0
[ProgBar:0]
BackFill=Fill Color #FontColor#,50
Fill=Fill Color #FontColor#
[ProgBar:1]
BackFill=Fill Color #FontColor#,25
Fill=Fill Color #FontColor#,100

[PosCalc]
Measure=calc
OnUpdateAction=[!CommandMeasure state#NowPlayingMedia# "SetPosition [PosCalc]"][!UpdateMeasure #PlayerType#Progress]
Disabled=1
DynamicVariables=1
UpdateDivider=-1
Group=NUOL

; ---------------------------------------------------------------------------- ;
;                                    Special                                   ;
; ---------------------------------------------------------------------------- ;

; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=1
Y=1
Shape=Rectangle 0,0,#LocksW#,#H#,#StaticWin11Corner# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #BorderColor#,#BorderOpacity#
Group=Locks | Special
Hidden=1
DynamicVariables=1
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#LocksW#,#H#,#StaticWin11Corner# | StrokeWidth 0
Group=Locks | Special
Hidden=1
DynamicVariables=1



[LocksIcon]
Meter=String
MeterStyle=String:S
X=(#Locksw#/2)
Y=(#Locksw#/2)

FontFace=Material Icons Round
StringAlign=CenterCenter
FontSize=(32*#scale#)
InlineSetting=Shadow | 0 | 0 | 5 | #GlowColor#,#GlowOpacity#
Group=Locks | Special
Container=LocksC
Hidden=1
[LocksText]
Meter=String
MeterStyle=String:S
X=r
Y=(#P#*3)r
W=(#LocksW#)
StringAlign=Center
ClipString=2
FontSize=(10*#scale#)
InlineSetting=Shadow | 0 | 0 | 5 | #GlowColor#,#GlowOpacity#
Group=Locks | Special
Container=LocksC
Hidden=1



[LocksIcon.On:S]
Text=[\xe837]
[LocksIcon.Off:S]
Text=[\xe836]
