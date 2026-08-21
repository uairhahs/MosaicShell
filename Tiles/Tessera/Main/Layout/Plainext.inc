[Rainmeter]
SkinWidth=(600*#scale#)

[Variables]
W=(500*#scale#)
P=(20*#scale#)

mVolume.Class=[mVolume]
mBrightness.Class=Brightness

SliderRotation=h

[SliderStart]
Formula=[VolumeBar:X]
[SliderEnd]
Formula=[VolumeBar:W]


; ---------------------------------------------------------------------------- ;
;                                 Meter styles                                 ;
; ---------------------------------------------------------------------------- ;

[String:S]
X=r
Y=R
InlineSetting=Shadow | 0 | 0 | 4 | #PrimaryColor#,#PrimaryOpacity#
FontSize=(20*#scale#)
DynamicVariables=1
[Sec.Div:S]
X=r
Y=R
Text=#DividerCap#[&Func:returnDiv('#DividerCharacter#')]#DividerCap#
H=(30*#scale#)
DynamicVariables=1
FontFace=#FontFace#
FontSize=(20*#scale#)
AntiAlias=1
FontColor=#FontColor#


; ---------------------------------- Volume ---------------------------------- ;

[VolumeB]
Meter=Shape
Shape=Path This | Fill Color #BackgroundColor#,#BackgroundOpacity# | StrokeWidth 0
This=0,0 | LineTo #W#,0 | LineTo (#W#-25*#Scale#),([Standard.DIV:YH]) | LineTo 0,([Standard.DIV:YH]) | ClosePath 1
Group=Standard
DynamicVariables=1
[VolumeC]
Meter=Shape
Shape=Path This | StrokeWidth 0
This=0,0 | LineTo #W#,0 | LineTo (#W#-25*#Scale#),([Standard.DIV:YH]) | LineTo 0,([Standard.DIV:YH]) | ClosePath 1
Group=Standard
DynamicVariables=1

[VolumeString]
Meter=String
MeterStyle=String:S
X=#P#
Y=#p#
Text=[##CurrentSlider#.Class]: [&Func:volumeLevel('Muted', '%')]
DynamicVariables=1
Container=VolumeC
Group=Dynamic | Standard
[VolumeBar]
Meter=String
MeterStyle=String:S
FontSize=(18*#Scale#)
X=r
Y=R
Text=[&Func:returnDiv('#BarCharacter#')]
inlinepattern2=[&Func:checkEscape('#BarCharacter#')']
InlineSetting2=Color | #FontCOlor2#
InlinePattern3=^[&Func:checkEscape('#BarCharacter#')']{[&Func:returnBar('#CurrentSlider#')]}
InlineSetting3=Color | #AccentColor#
DynamicVariables=1
LeftMouseDownAction=[!CommandMeasure VolumeMouse "Start"]
Group=Dynamic | Standard
Container=VolumeC
[Standard.DIV]
Meter=String
X=#P#
MEterStyle=Sec.Div:S
Group=Standard
Container=VolumeC

; ----------------------------------- Media ---------------------------------- ;

[MediaB]
Meter=Shape
X=(-(#W#-25*#scale#)+(#W#-25*#scale#)*#TweenNode1#)
Y=R
Shape=Path This | Fill Color #BackgroundColor#,(#BackgroundOpacity#*#TweenNode1#) | StrokeWidth 0
This=0,0 | LineTo (#W#-25*#scale#),0 | LineTo (#W#-50*#Scale#),([MediaStates:YH]-[MediaB:Y]+#P#) | LineTo 0,([MediaStates:YH]-[MediaB:Y]+#P#) | ClosePath 1
Group=Standard | Animated
DynamicVariables=1
[MediaC]
Meter=Shape
X=r
Y=r
Shape=Path This | StrokeWidth 0
This=0,0 | LineTo (#W#-25*#scale#),0 | LineTo (#W#-50*#Scale#),([MediaStates:YH]-[MediaB:Y]+#P#) | LineTo 0,([MediaStates:YH]-[MediaB:Y]+#P#) | ClosePath 1
Group=Standard
DynamicVariables=1

[MediaTrack]
Meter=String
MeterStyle=String:S
X=#P#
W=(#W#-50*#Scale#)
ClipString=1
Text=[#PlayerType#Track] [&Func:returnBool('[&state#NowPlayingMedia#:]', '2', '> Paused <', '')]
FontWeight=650
FontSize=(24*#scale#)
Container=MediaC
Group=Music | Standard
[MediaArtist]
Meter=String
MeterStyle=String:S
W=(#W#-80*#Scale#)
ClipString=1
Text=[#PlayerType#Artist]
Container=MediaC
FontSize=(20*#scale#)
Group=Music | Standard
[MediaBar]
Meter=String
Y=(10*#Scale#)R
MeterStyle=String:S
FontSize=(16*#scale#)
Text=[#PlayerType#CurrentPosition] [&Func:returnDiv('#BarCharacter#')] [#PlayerType#Length]
inlinepattern2=[&Func:checkEscape('#BarCharacter#')']
InlineSetting2=Color | #FontCOlor2#
InlinePattern3=\s[&Func:checkEscape('#BarCharacter#')']{[&Func:returnBar('#PlayerType#Progress')]}
InlineSetting3=Color | #AccentColor#
Group=Music | Standard
UpdateDivider=1
Container=MediaC
LeftMouseUpAction=[!SetOption PosCalc Formula "($MouseX$/([#CURRENTSECTION#:W]-#P#)*100)"][!UpdateMeasure PosCalc]
[PosCalc]
Measure=calc
OnUpdateAction=[!CommandMeasure state#NowPlayingMedia# "SetPosition [PosCalc]"][!UpdateMeasure #PlayerType#Progress]
Disabled=1
DynamicVariables=1
UpdateDivider=-1
Group=NUOL
Container=MediaC
[MediaStates]
Meter=String
X=r
MeterStyle=String:S
FontSize=(12*#scale#)
Container=MediaC
Text=[#PlayerType#Player] | Heart: [#PlayerType#Heart] Shuffle: [#PlayerType#Shuffle] Repeat: [#PlayerType#Repeat]
Group=Music | Standard

[MediaPlayPause]
Meter=String
[MediaHeart]
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
Shape=Path This | Fill Color #BackgroundColor#,#BackgroundOpacity# | StrokeWidth 0
This=0,0 | LineTo #W#,0 | LineTo (#W#-25*#Scale#),([Locks.DIV:YH]) | LineTo 0,([Locks.DIV:YH]) | ClosePath 1
Group=Locks | Special
DynamicVariables=1

[LocksText]
Meter=String
MeterStyle=String:S
X=#P#
Y=#p#
InlinePattern=On
InlineSetting=Color | #AccentColor#
InlinePattern2=Off
InlineSetting2=Color | #FontColor2#
Group=Locks | Special
[Locks.DIV]
Meter=String
MEterStyle=Sec.Div:S
Group=Locks | Special
