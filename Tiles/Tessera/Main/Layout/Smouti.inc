[Variables]
H=100
MediaW=(500*#Scale#)
LocksW=(#H#)
P=(20*#Scale#)
CornerSize=25

SliderM=[VolumeBar:X]
SliderW=[Volumebar:W]

; -------------------------- Volume control section -------------------------- ;

[VolumeB]
Meter=Shape
X=#P#
Y=#P#
Shape=Path Sci | StrokeWidth (4*#scale#) | Fill LinearGradient Backlight | Stroke Color #AccentColor# | Scale #Scale#,#Scale#,0,0
; Shape2=Rectangle ((8+#CornerSize#)*#Scale#),(8*#scale#),((#H#-8*2-#CornerSize#)*#scale#),(16*#scale#) | Extend Common
; Shape3=Rectangle (8*#Scale#),((#H#-8)*#scale#),((30)*#scale#),(-8*#scale#) | Extend Common
Shape2=Path SciLine | StrokeWidth (4*#scale#) | Stroke LinearGradient SciGrad | StrokeEndCap Triangle
Common=StrokeWidth 0 | Fill Color #BrightColor#,150
Backlight=270 | 0,0,0,1 ; 0 | #AccentColor#,100 ; 1
SciGrad=180 | #AccentColor# ; 0 | #BrightColor# ; 1
Sci=#CornerSize#,0 | LineTo #H#,0 | LineTo #H#,(#H#-#CornerSize#) | LineTo (#H#-#CornerSize#),#H# | LineTo 0,#H# | LineTo 0,(#CornerSize#) | ClosePath 1
SciLine=(#H#*#Scale#),((#H#-10-#CornerSize#)*#Scale#) | LineTo (125*#Scale#),((#H#-10-#CornerSize#)*#Scale#) | LineTo (135*#Scale#),((#H#-#CornerSize#)*#Scale#) | LineTo (335*#Scale#),((#H#-#CornerSize#)*#Scale#) | LineTo (365*#Scale#),((#H#-30-#CornerSize#)*#Scale#)

[VolumeC]
Meter=String
X=r
Y=r
W=(365*#scale#)
H=(#H#)
Solidcolor=255,255,255

[VolumeString]
Meter=String
MeterStyle=String:S
X=(52*#Scale#)
Y=(52*#Scale#)
FontSize=(24*#Scale#)
FontWeight=300
Text=[&Func:volumeLevel()]
FontFace=Elianto
StringAlign=CenterCenter
DynamicVariables=1
FontColor=#BrightColor#
InlineSetting=Shadow | 0 | 0 | 8 | #AccentColor2#
Group=Dynamic | Standard
Container=VolumeC

; [VolumeBar]
; Meter=Shape
; X=(#VolumeW#/2)
; Y=([SliderStart])
; Shape=Line 0,[SliderEnd],0,0 | StrokeWidth (3*#scale#) | Fill Color 0,0,0,1 | Stroke Color #SecondaryColor# | StrokeStartCap #Cap# | StrokeEndCap #Cap#
; Shape2=Line 0,[SliderEnd],0,([SliderEnd]*(1-[mVolume:]/100)) | StrokeWidth (3*#Scale#) | Fill Color 0,0,0,1 | Stroke LinearGradient This | StrokeStartCap #Cap# | StrokeEndCap #Cap#
; Shape3=Ellipse 0,([SliderEnd]*(1-[mVolume:]/100)),(6*#Scale#) | StrokeWidth 0 | Fill Color #AccentColor#
; This=270 | #AccentColor# ; 0.0 | #PrimaryColor# ; 1.0
; LeftMouseDownAction=[!CommandMeasure VolumeMouse "Start"]
; DynamicVariables=1
; Group=Dynamic | Standard
; Container=VolumeC

[DeviceString]
Meter=String
MeterStyle=String:S
X=(130*#Scale#)
FontSize=(22*#Scale#)
FontWeight=300
MeasureName=mVolume
FontColor=#AccentColor2#
InlineSetting=Shadow | 0 | 0 | 2 | #AccentColor2#
Group=Dynamic | Standard
Container=VolumeC

[VolumeBar]
Meter=String
MeterStyle=String:S
X=r
Y=R
FontSize=(12*#Scale#)
MeasureName=mVolume
InlineSetting=Shadow | 0 | 0 | 2 | #BrightColor#
Group=Dynamic | Standard
Container=VolumeC

Text=[&Func:returnDiv('#BarCharacter#')]
inlinepattern2=[&Func:checkEscape('#BarCharacter#')']
InlineSetting2=Color | #AccentColor#,50
InlinePattern3=^[&Func:checkEscape('#BarCharacter#')']{[&Func:returnBar('mVolume')]}
InlineSetting3=Color | #AccentColor2#
DynamicVariables=1

LeftMouseDownAction=[!CommandMeasure VolumeMouse "Start"]


; --------------------------------- Media -------------------------------- ;


; [MediaB]
; Meter=Shape
; X=(#VolumeW#/2 + (#VolumeW#/2+#P#/2)*#TweenNode1#)
; Y=r
; DynamicVariables=1
; Shape=Rectangle 0,0,#MediaW#,#H#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,(#PrimaryOpacity#*#TweenNode1#)
; Group=Music | Standard | Animated
; [MediaC]
; Meter=Shape
; X=r
; Y=r
; DynamicVariables=1
; Shape=Rectangle 0,0,#MediaW#,#H#,#R# | StrokeWidth 0 | Fill Color 255,255,255,(255*#TweenNode1#)
; Group=Music | Standard | Animated

; [MediaTrack]
; Meter=String
; MeterStyle=String:S
; X=#P#
; Y=(20*#scale#)r
; W=(#MediaW#-150*#scale#)
; FontSize=(20*#scale#)
; ClipString=1
; Text=[#PlayerType#Track]
; DynamicVariables=1
; FontFace=#FontFace2#
; Group=Music | Standard
; Container=MediaC
; [MediaArtist]
; Meter=String
; MeterStyle=String:S
; X=r
; Y=R
; W=(#MediaW#-150*#scale#)
; FontSize=(10*#scale#)
; ClipString=1
; Text=[#PlayerType#Artist]
; FontWeight=600
; DynamicVariables=1
; Group=Music | Standard
; Container=MediaC
; [MediaProgress]
; Meter=String
; MeterStyle=String:S
; X=r
; Y=R
; W=(#MediaW#-#P#*2)
; FontSize=(10*#scale#)
; ClipString=1
; Text=[#PlayerType#CurrentPosition] / [#PlayerType#Length]
; FontColor=#FontColor2#
; FontWeight=600
; DynamicVariables=1
; UpdateDivider=1
; Group=Music | Standard
; Container=MediaC

; [MediaPlayPauseBackground]
; Meter=Shape
; X=([MediaPlayPause:X]+10*#Scale#-[MediaB:X])
; Y=([MediaPlayPause:Y]+10*#Scale#)
; DynamicVariables=1
; Shape=Ellipse 0,0,25 | StrokeWidth 0 | Fill Color #AccentColor1#,50 | Scale #Scale#,#Scale#
; Shape2=Ellipse 0,0,20 | StrokeWidth 0 | Fill LinearGradient This | Scale #Scale#,#Scale#
; This=0 | #AccentColor1# ; 0.0 | #AccentColor2# ; 1.0
; Group=Music | Standard
; Container=MediaC


; [Previous]
; Meter=Image
; ImageName=#@#Icons\Previous.png
; X=(#MediaW#-#P#-140*#scale#)
; Y=(#H#/2-10*#scale#)
; W=(20*#scale#)
; H=(20*#scale#)
; Grayscale=1
; ImageTint=#FontColor#
; DynamicVariables=1
; LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Previous"]
; Group=Music | Standard
; Container=MediaC


; [MediaPlayPause]
; Meter=Image
; X=(40*#Scale#)R
; Y=r
; W=(20*#scale#)
; H=(20*#scale#)
; Grayscale=1
; ImageTint=#FontColor#
; DynamicVariables=1
; Group=Music | Standard
; Container=MediaC
; [Play]
; ImageName=#@#Icons\Play.png
; LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle Pause][!UpdateMeter MediaPlayPause][!Redraw][!CommandMeasure state#NowPlayingMedia# "PlayPause"]
; [Pause]
; ImageName=#@#Icons\Pause.png
; LeftMouseUpAction=[!SetOption MediaPlayPause MeterStyle Play][!UpdateMeter MediaPlayPause][!Redraw][!CommandMeasure state#NowPlayingMedia# "PlayPause"]

; [Next]
; Meter=Image
; ImageName=#@#Icons\Next.png
; X=(40*#Scale#)R
; Y=r
; W=(20*#scale#)
; H=(20*#scale#)
; Grayscale=1
; ImageTint=#FontColor#
; DynamicVariables=1
; LeftMouseUpAction=[!CommandMeasure state#NowPlayingMedia# "Next"]
; Group=Music | Standard
; Container=MediaC

; [ProgBar]
; Meter=Shape
; X=#P#
; Y=(#H#-#P#)
; Shape=Line 0,0,(#MediaW#-#P#*2),0 | StrokeWidth 2 | Fill Color 0,0,0,1 | Stroke Color #FontColor#,50 | StrokeStartCap #Cap# | StrokeEndCap #Cap#
; Shape2=Line 0,0,((#MediaW#-#P#*2)*[#PlayerType#Progress]/100),0 | StrokeWidth 4 | Fill Color 0,0,0,1 | Stroke LinearGradient This | StrokeStartCap #Cap# | StrokeEndCap #Cap#
; This=0 | #AccentCOlor1# ; 0.0 | #AccentColor2# ; 1.0
; LeftMouseUpAction=[!SetOption PosCalc Formula "($MouseX$/(#MediaW#-#P#*2)*100)"][!UpdateMeasure PosCalc]
; DynamicVariables=1
; UpdateDivider=1
; Group=Music | Standard
; Container=MediaC
; [PosCalc]
; Measure=calc
; OnUpdateAction=[!CommandMeasure state#NowPlayingMedia# "SetPosition [PosCalc]"][!UpdateMeasure #PlayerType#Progress]
; Disabled=1
; DynamicVariables=1
; UpdateDivider=-1
; Group=NUOL

; [MediaHeart]
; Meter=String
; [MediaShuffle]
; Meter=String

; ---------------------------------------------------------------------------- ;
;                                    Special                                   ;
; ---------------------------------------------------------------------------- ;

; ----------------------------------- Caps ----------------------------------- ;

[LocksB]
Meter=Shape
X=1
Y=1
Shape=Rectangle 0,0,#LocksW#,#H#,#R# | StrokeWidth #Stroke# | Fill Color #PrimaryColor#,#PrimaryOpacity# | Stroke Color #SecondaryColor#,#SecondaryOpacity#
Group=Locks | Special
Hidden=1
[LocksC]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#LocksW#,#H#,#R# | StrokeWidth 0
Group=Locks | Special
Hidden=1



[LocksIcon]
Meter=String
MeterStyle=String:S
X=(#Locksw#/2)
Y=(#Locksw#/2)

FontFace=Material Icons Round
StringAlign=CenterCenter
FontSize=(32*#scale#)
Group=Locks | Special
InlineSetting=Shadow | 0 | 0 | 10 | 0,0,0,150
Container=LocksC
Hidden=1
[LocksText]
Meter=String
MeterStyle=String:S
X=r
Y=(#P#*2)r
W=(#LocksW#)
StringAlign=Center
ClipString=2
FontSize=(8*#scale#)
Fontcolor=#FontColor2#
Group=Locks | Special
Container=LocksC
Hidden=1
