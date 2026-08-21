; ---------------------------------------------------------------------------- ;
;                                     Music                                    ;
; ---------------------------------------------------------------------------- ;

[Variables]
@includeModuleVariable=Vars\Music.inc
MusicHeight=145
MusicModuleCheckerVariable=1

NowPlayingMedia=CAD
PlayerType=WNP
PlayerProperties=0|0|1|1|0
; Shuffle|Repeat|Heart|Prog|ShowWhenNotPLaying

[MeasureMouse]
Measure=Plugin

[MusicImg.Container]
Meter=Shape
Y=r
Shape=Rectangle 0,0,#W#,(145*#scale#) | StrokeWidth 0 | Fill LinearGradient This
This=0 | 00000000 ; 0.02 | 000000 ; 0.3 | 000000 ; 0.7 | 00000000 ; 0.98
Group=Music
[MusicImg]
Meter=Image
W=#W#
H=(145*#Scale#)
ImageAlpha=125
PreserveAspectRatio=2
Container=MusicImg.container
ImageName=[#PlayerType#Cover]
DynamicVariables=1
Group=Music | MusicImg

[MediaName]
Meter=String
X=(25*#Scale#)
Y=(25*#Scale#)r
W=[MediaSliderW]
DynamicVariables=1
Clipstring=1
MeterStyle=SemiBoldText
MeasureName=#PlayerType#Track
Group=Music

[MediaArtist]
Meter=String
X=r
Y=(10*#Scale#)R
FontSize=((10*#Scale#)*#FontSizeModifier#)
MeterStyle=RegularText
MeasureName=#PlayerType#Artist
Group=Music

[ProgBar]
Meter=Shape
X=(25*#Scale#)
Y=(40*#scale#)r
Shape=Line 0,0,[MediaSliderW],0 | StrokeWidth 2 | Fill Color 0,0,0,1 | Stroke Color #Font_Color#,150 | StrokeStartCap Round | StrokeEndCap Round
Shape2=Line 0,0,[MediaSliderW],0 | StrokeWidth (10*#Scale#) | Fill Color 0,0,0,1 | Stroke Color 0,0,0,1
Shape3=Line 0,0,([MediaSliderW]*[MusicSliderPercent]/100),0 | StrokeWidth 2 | Fill Color 0,0,0,1 | Stroke Color #Font_Color# | StrokeStartCap Round | StrokeEndCap Round
Shape4=Ellipse ([MediaSliderW]*[MusicSliderPercent]/100),0,4,4 | strokeWidth 0 | Fill Color #Font_Color#
DynamicVariables=1
UpdateDivider=1
Group=Music | MusicSlider
LeftMouseDownAction=[!CommandMeasure Func "InitMultiSlider('#CURRENTSECTION#')"]
SliderProperties=X|MediaSliderW|1|1|#PlayerType#Progress|MusicSliderPercent|Music|[!SetOption MusicCurrentTime Formula "($DragPercentage$/100*#PlayerType#Length)"]|[!CommandMeasure State#NowPlayingMedia# "SetPosition $DragPercentage$"][!SetOption MusicCurrentTime Formula "#*PlayerType*#CurrentPosition"]
[MediaSliderW]
Measure=Calc
Formula=(#W#-50*#Scale#)
[MusicSliderPercent]
Measure=Calc
Formula=#PlayerType#Progress
DynamicVariables=1
UpdateDivider=1
Group=Music | MusicSlider
[MusicCurrentTime]
Measure=Calc
Formula=#PlayerType#CurrentPosition
DynamicVariables=1
UpdateDivider=1
Group=Music | MusicSlider


[CurrentTime]
Meter=String
MeterStyle=RegularText
X=r
Y=R
FontColor=#Font_Color#
DynamicVariables=1
FontSize=((8*#scale#)*#FontSizeModifier#)
Text=[&Func:FormatSeconds('MusicCurrentTime')]
UpdateDivider=1
Group=Music
[MediaLength]
Meter=String
MeterStyle=RegularText
X=[MediaSliderW]r
Y=r
FontColor=#Font_Color#
StringAlign=Right
DynamicVariables=1
FontSize=((8*#scale#)*#FontSizeModifier#)
MeasureName=#PlayerType#Length
Group=Music




[Heart]
Meter=Image
X=(#W#-200*#Scale#)
Y=(-50*#scale#)r
W=(20*#scale#)
H=(20*#scale#)
Padding=(5*#scale#),(5*#scale#),(5*#scale#),(5*#scale#)
Grayscale=1
ImageTint=#Font_Color#
DynamicVariables=1
Group=Music | CtrlHeart
[FullHeart]
ImageName=#@#Icons\HeartFull.png
LeftMouseUpAction=[!SetOptionGroup CtrlHeart MeterStyle EmptyHeart][!UpdateMeterGroup CtrlHeart][!Redraw][!CommandMeasure State#NowPlayingMedia# "ToggleThumbsDown"]
[EmptyHeart]
ImageName=#@#Icons\Heart.png
LeftMouseUpAction=[!SetOptionGroup CtrlHeart MeterStyle Fullheart][!UpdateMeterGroup CtrlHeart][!Redraw][!CommandMeasure State#NowPlayingMedia# "ToggleThumbsUp"]


[Previous]
Meter=Image
ImageName=#@#Icons\Previous.png
X=(20*#Scale#)R
Y=r
W=(20*#scale#)
H=(20*#scale#)
Padding=(5*#scale#),(5*#scale#),(5*#scale#),(5*#scale#)
Grayscale=1
ImageTint=#Font_Color#
DynamicVariables=1
Group=Music
LeftMouseUpAction=[!CommandMeasure State#NowPlayingMedia# "Previous"]


[PlayPause]
Meter=Image
MeterStyle=
X=(20*#Scale#)R
Y=r
W=(20*#scale#)
H=(20*#scale#)
Padding=(5*#scale#),(5*#scale#),(5*#scale#),(5*#scale#)
Grayscale=1
ImageTint=#Font_Color#
DynamicVariables=1
Group=Music | CtrlPlayPause
[Play]
ImageName=#@#Icons\Play.png
LeftMouseUpAction=[!SetOptionGroup CtrlPlayPause MeterStyle Pause][!UpdateMeterGroup CtrlPlayPause][!Redraw][!CommandMeasure State#NowPlayingMedia# "PlayPause"]
[Pause]
ImageName=#@#Icons\Pause.png
LeftMouseUpAction=[!SetOptionGroup CtrlPlayPause MeterStyle Play][!UpdateMeterGroup CtrlPlayPause][!Redraw][!CommandMeasure State#NowPlayingMedia# "PlayPause"]

[Next]
Meter=Image
ImageName=#@#Icons\Next.png
X=(20*#Scale#)R
Y=r
W=(20*#scale#)
H=(20*#scale#)
Padding=(5*#scale#),(5*#scale#),(5*#scale#),(5*#scale#)
Grayscale=1
ImageTint=#Font_Color#
DynamicVariables=1
Group=Music
LeftMouseUpAction=[!CommandMeasure State#NowPlayingMedia# "Next"]

[MusicAnchor]
Meter=String
Y=([MusicImg:Y]+[MusicImg:H]+#P#*#Divider#)
DynamicVariables=1
; Group=Music
