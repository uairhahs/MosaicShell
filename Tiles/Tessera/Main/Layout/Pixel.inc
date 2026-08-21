[Variables]
ColumnW=(60*#Scale#)
H=(#Height#*#scale#)
P=(10*#Scale#)

AdditionalToggleOnOptions=[!SetOption LocksB MeterStyle "LocksB.On:S"]
AdditionalToggleOffOptions=[!SetOption LocksB MeterStyle "LocksB.Off:S"]
MHC_Reset=[!SetOption MediaHoverCircle Stroke "Fill Color #FontColor#,20"][!UpdateMeter MediaHoverCircle][!Redraw]

SliderRotation=v

[SliderStart]
Formula=(#ColumnW#)
[SliderEnd]
Formula=(#H#-(#ColumnW#)*3+#P#*2)

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[mVolume]
IfCondition=mVolume < 0
iftrueaction=[!SetOption VolumeIcon Text "[\xe440]"]
IfCondition2=(mVolume >= 0)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xe405]"]
IfConditionMode=1

[mBrightness]
IfCondition=(mBrightness = 0)
IfTrueAction=[!SetOption VolumeIcon Text "[\xe1ad]"]
IfCondition2=(mBrightness > 0)
IfTrueAction2=[!SetOption VolumeIcon Text "[\xe1ac]"]
IfConditionMode=1

[MeasureAudio]
Measure=Plugin
Plugin=AudioLevel
Port=Output
[MeasureDeviceList]
Measure=Plugin
Plugin=AudioLevel
Parent=MeasureAudio
Type=DeviceList
OnUpdateAction=[!CommandMeasure Func "GenerateDeviceMenu()"]

[LocksText.CapsLock.On:S]
Text=C
FontColor=#FontColor#
[LocksText.CapsLock.Off:S]
Text=C
FontColor=#FontColorAccent#
[LocksText.ScrollLock.On:S]
Text=S
FontColor=#FontColor#
[LocksText.ScrollLock.Off:S]
Text=S
FontColor=#FontColorAccent#
[LocksText.NumLock.On:S]
Text=N
FontColor=#FontColor#
[LocksText.NumLock.Off:S]
Text=N
FontColor=#FontColorAccent#

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Sec.BottomButton:S]
X=r
Y=(#ColumnW#-#P#)r
W=(20*#scale#)
H=(20*#scale#)
Grayscale=1
ImageTint=#FontColorAccent#
DynamicVariables=1
Group=Music | Standard
Container=MediaC

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; --------------------------------- VolumeBar -------------------------------- ;

[VolumeB]
Meter=Shape
X=(20*#Scale#+#ColumnW#)
Y=0
Shape=Rectangle 0,0,#ColumnW#,(#H#-((#ColumnW#-#P#)*#HideVolLevel#)),(#ColumnW#/2) | StrokeWidth 0 | Extend This
Shape2=Rectangle 0,(#H#-((#ColumnW#-#P#)*#HideVolLevel#)+#P#),#ColumnW#,#ColumnW#,(#ColumnW#/2) | StrokeWidth 0 | Extend This
This=Fill Color #PrimaryColor#,#PrimaryOpacity#
DynamicVariables=1
Group=Standard
[VolumeC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#ColumnW#,#H#,(#ColumnW#/2) | StrokeWidth 0
Shape2=Rectangle 0,(#H#),#ColumnW#,#ColumnW#,(#ColumnW#/2) | StrokeWidth 0
DynamicVariables=1
Group=Standard

[DeviceShape]
Meter=Shape
X=(#ColumnW#/2)
Y=(#ColumnW#/2)
Shape=Ellipse 0,0,((#ColumnW#-#P#*2)/2) | StrokeWidth 0 | Fill Color #AccentColor#
Container=VolumeC
DynamicVariables=1
[DeviceIcon]
Meter=String
FontFace=Material Icons Round
Text=[\xf01f]
X=r
Y=r
FontSize=(16*#Scale#)
MeterStyle=String:S
Container=VolumeC
StringAlign=CenterCenter
LeftMouseUpAction=[!SkinCustomMenu]

[VolumeBar]
Meter=Shape
Shape=Line (#ColumnW#/2),(#ColumnW#*1.5-#P#),(#ColumnW#/2),(#H#-#ColumnW#*1.5+#P#) | StrokeWidth (4*#Scale#) | Stroke COlor #SecondaryColor# | StrokeStartCap Round | StrokeEndCap Round
Shape2=Rectangle (#P#),(#H#-#ColumnW#),(#ColumnW#-#P#*2),(-(#H#-#ColumnW#*3+#P#*2)*[#CurrentSlider#:]/100-(#ColumnW#-#P#*2)),((#ColumnW#-#P#*2)/2) | StrokeWidth 0 | Fill Color #AccentColor#
LeftMouseDownAction=[!CommandMeasure VolumeMouse "Start"]
DynamicVariables=1
Container=VolumeC
Group=Dynamic | Standard
[VolumeIcon]
Meter=String
FontFace=Material Icons Round
Text=[\xe440]
X=(#ColumnW#/2)
Y=((#H#-#ColumnW#)-(#H#-#ColumnW#*3+#P#*2)*[#CurrentSlider#:]/100-(#ColumnW#-#P#*2)/2)
FontSize=(16*#Scale#)
DynamicVariables=1
MeterStyle=String:S
Group=Dynamic | Standard
Container=VolumeC
StringAlign=CenterCenter

[VolumeString]
Meter=String
MeterStyle=String:S
X=(#ColumnW#/2)
Y=(#H#-#ColumnW#/2)
StringAlign=CenterCenter
Text=[&Func:volumeLevel()]
DynamicVariables=1
FontWeight=600
FontColor=#FontColorAccent#,(255*(1-#HideVollevel#))
Group=Dynamic | Standard
Container=VolumeC

[MixerIcon]
Meter=String
FontFace=Material Icons Round
Text=[\xe429]
X=(#ColumnW#/2)
Y=(#H#+#ColumnW#/2+#P#-((#ColumnW#-#P#)*#HideVolLevel#))
W=(#ColumnW#)
H=(#ColumnW#)
FontSize=(16*#Scale#)
MeterStyle=String:S
Container=VolumeC
FontColor=#FontColorAccent#
LeftMouseUpAction=#BottomActionBang#
ToolTipText=Launch #BottomActionName#
StringAlign=CenterCenter

; --------------------------------- Media -------------------------------- ;

[MediaB]
Meter=Shape
X=(#ColumnW#*(1-#TweenNode1#))
Y=0
Shape=Rectangle 0,0,#ColumnW#,(#ColumnW#*3-#P#*2+40*#Scale#),(#ColumnW#/2) | StrokeWidth 0 | Extend This
Shape2=Rectangle 0,(#ColumnW#*3-#P#+40*#Scale#),#ColumnW#,(#ColumnW#*3-#P#*2),(#ColumnW#/2) | StrokeWidth 0 | Extend This
This=Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#)
DynamicVariables=1
Group=Music | Standard | Animated
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#ColumnW#,(#ColumnW#*3-#P#*2+40*#Scale#),(#ColumnW#/2) | StrokeWidth 0 | Fill Color 255,255,255,(255*#TweenNode1#)
Shape2=Rectangle 0,(#ColumnW#*3-#P#+40*#Scale#),#ColumnW#,(#ColumnW#*3-#P#*2),(#ColumnW#/2) | StrokeWidth 0 | Fill Color 255,255,255,(255*#TweenNode1#)
DynamicVariables=1
Group=Music | Standard | Animated

; --------------------------------- Section 1 -------------------------------- ;

[MediaPrev]
Meter=Image
X=(#ColumnW#/2-10*#Scale#)
Y=(#ColumnW#/2-10*#Scale#)
ImageName=#@#Icons\Previous.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Previous"]
MeterStyle=Sec.BottomButton:S

[PlayPauseShape]
Meter=Shape
X=(#P#)
Y=((#ColumnW#-#P#*2)+#P#*2)
Shape=Rectangle 0,0,(#ColumnW#-#P#*2),(#ColumnW#-#P#*2+40*#Scale#),(#ColumnW#/2-#P#) | StrokeWidth 0 | Fill Color #AccentColor# | Extend Scale
Scale=Scale 1,1
MouseOverAction=[!SetOption #CURRENTSECTION# Scale "Scale 1.1,1.1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Scale "Scale 1,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=MediaC
Group=Music | Standard
DynamicVariables=1
[MediaPlayPause]
Meter=Image
X=r
Y=r
ImageTint=#FontColor#
SolidColor=0,0,0,1
Padding=((#ColumnW#-#P#*2)/2-10*#scale#),((#ColumnW#-#P#*2+40*#Scale#)/2-10*#scale#),((#ColumnW#-#P#*2)/2-10*#scale#),((#ColumnW#-#P#*2+40*#Scale#)/2-10*#scale#)
MeterStyle=Sec.BottomButton:S
[Play]
ImageName=#@#Icons\Play.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Pause"][!UpdateMeter MediaPlayPause][!CommandMeasure state#NowPlayingMedia# "PlayPause"]
[Pause]
ImageName=#@#Icons\Pause.png
LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle "Sec.BottomButton:S | Play"][!UpdateMeter MediaPlayPause][!CommandMeasure state#NowPlayingMedia# "PlayPause"]

[MediaNext]
Meter=Image
X=(#ColumnW#/2-10*#Scale#)
Y=((#ColumnW#-#P#*2)+10*#scale#+(#ColumnW#-#P#))r
ImageName=#@#Icons\Next.png
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Next"]
MeterStyle=Sec.BottomButton:S

; --------------------------------- Section 2 -------------------------------- ;

[MediaShuffle]
Meter=Image
X=(#ColumnW#/2-10*#Scale#)
Y=(#ColumnW#/2+(#ColumnW#*3-#P#)-10*#Scale#+40*#Scale#)
ImageName=#@#Icons\Shuffle.png
MeterStyle=Sec.BottomButton:S
[Shuffle0]
ImageAlpha=150
LeftMouseUpAction=[!SetOption MediaShuffle MeterStyle "Sec.BottomButton:S | Shuffle1"][!UpdateMeter MediaShuffle][!CommandMeasure state#NowPlayingMedia# "Shuffle"]
[Shuffle1]
ImageAlpha=255
LeftMouseUpAction=[!SetOption MediaShuffle MeterStyle "Sec.BottomButton:S | Shuffle0"][!UpdateMeter MediaShuffle][!CommandMeasure state#NowPlayingMedia# "Shuffle"]

[MediaHeart]
Meter=Image
MeterStyle=Sec.BottomButton:S
[FullHeart]
ImageName=#@#Icons\HeartFull.png
LeftMouseUpAction=[!SetOption #CURRENTSECTION# MeterStyle "Sec.BottomButton:S | EmptyHeart"][!UpdateMeter #CURRENTSECTION#][!CommandMeasure state#NowPlayingMedia# "ToggleThumbsDown"]
[EmptyHeart]
ImageName=#@#Icons\Heart.png
LeftMouseUpAction=[!SetOption #CURRENTSECTION# MeterStyle "Sec.BottomButton:S | Fullheart"][!UpdateMeter #CURRENTSECTION#][!CommandMeasure state#NowPlayingMedia# "ToggleThumbsUp"]

[MediaRepeat]
Meter=Image
MeterStyle=Sec.BottomButton:S
LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Repeat"]
[Repeat0]
ImageAlpha=150
ImageName=#@#Icons\Repeat.png
[Repeat1]
ImageName=#@#Icons\Repeat.png
[Repeat2]
ImageName=#@#Icons\RepeatOne.png

; ; ---------------------------------------------------------------------------- ;
; ;                                    Special                                   ;
; ; ---------------------------------------------------------------------------- ;

; ; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=(20*#Scale#+#ColumnW#)
Y=0
Shape=Rectangle 0,0,#ColumnW#,#ColumnW#,(#ColumnW#/2) | StrokeWidth 0 | Fill Color #PrimaryColor#,#PrimaryOpacity#
Shape2=Rectangle #p#,#p#,(#ColumnW#-#P#*2),(#ColumnW#-#p#*2),(#ColumnW#/2-#P#) | StrokeWidth 0 | Extend This
Group=Locks | Special
MeterStyle=LocksB.Off:S
[LocksB.On:S]
This=Fill Color #AccentColor#
[LocksB.Off:S]
This=Fill Color 0,0,0,1
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#ColumnW#,#ColumnW#,(#ColumnW#/2) | StrokeWidth 0
Group=Locks | Special

[LocksIcon]
Meter=String
MeterStyle=String:S
X=(#ColumnW#/2)
Y=(#ColumnW#/2+2)
Group=Locks | Special
FontFace=Material Icons Round
StringAlign=CenterCenter
Container=LocksC
FontSize=(16*#Scale#)
FontWeight=600
DynamicVariables=1
Hidden=1
[LocksIcon.On:S]
Text=[\xe876]
FontColor=#PrimaryColor#
[LocksIcon.Off:S]
Text=[\xe5cd]
FontColor=#FontColorAccent#
[FlightIcon.On:S]
FontColor=#PrimaryColor#
[FlightIcon.Off:S]
FontColor=#FontColorAccent#
