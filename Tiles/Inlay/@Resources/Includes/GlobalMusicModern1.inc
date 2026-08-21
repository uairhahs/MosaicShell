; ---------------------------------------------------------------------------- ;
;                                      Gru                                     ;
; ---------------------------------------------------------------------------- ;
[Variables]
PlayerType=Modern
NowPlayingMedia=Modern

[stateModern]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=State
OnChangeAction=[!CommandMeasure Func "checkMediaModern()"]
DynamicVariables=1
UpdateDivider=1
Group=MusicGru

; ---------------------------------------------------------------------------- ;
;                                    Minions                                   ;
; ---------------------------------------------------------------------------- ;

; ------------------------------------ modern ------------------------------------ ;

[modernPlayer]
Measure=String
String=MediaPlayerDLL
UpdateDivider=-1
Substitute="":"Media"
Group=Music | modern
DynamicVariables=1


[modernCover]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Cover
Substitute="":"#@#Images\Default.png"
UpdateDivider=1
Group=Music | modern
DynamicVariables=1


[modernArtist]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Artist
Substitute="":"-"
UpdateDivider=1
Group=Music | modern
DynamicVariables=1


[modernTrack]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Title
Substitute="":"Not playing"
OnChangeAction=[!UpdateMeterGroup Music][!Redraw]
UpdateDivider=1
Group=Music | modern
DynamicVariables=1


[modernProgress]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Progress
Substitute="NaN":"1"
UpdateDivider=1
Group=Music | modern | Prog
DynamicVariables=1


[modernCurrentPosition]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Position
UpdateDivider=1
Group=Music | modern | Prog
DynamicVariables=1


[modernLength]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Duration
UpdateDivider=1
Group=Music | modern | Prog
DynamicVariables=1

[modernRepeat]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Repeat
IfCondition=modernRepeat = 1
IfTrueAction=[!SetOptionGroup CtrlRepeat MeterStyle "Repeat1"][!UpdateMeterGroup CtrlRepeat][!Redraw]
IfCondition2=modernRepeat = 2
IfTrueAction2=[!SetOptionGroup CtrlRepeat MeterStyle "Repeat2"][!UpdateMeterGroup CtrlRepeat][!Redraw]
IfCondition3=modernRepeat = 0
IfTrueAction3=[!SetOptionGroup CtrlRepeat MeterStyle "Repeat0"][!UpdateMeterGroup CtrlRepeat][!Redraw]
UpdateDivider=1
Group=Music | modern | repeat


[modernShuffle]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Shuffle
IfCondition=modernShuffle = 0
IfTrueAction=[!SetOptionGroup CtrlShuffle MeterStyle "Shuffle0"][!UpdateMeterGroup CtrlShuffle][!Redraw]
IfFalseAction=[!SetOptionGroup CtrlShuffle MeterStyle "Shuffle1"][!UpdateMeterGroup CtrlShuffle][!Redraw]
UpdateDivider=1
Group=Music | modern | Shuffle
DynamicVariables=1

[modernHeart]
Measure=Plugin
Plugin=WindowsNowPlaying
PlayerType=Rating
IfCondition=modernHeart = 0
IfTrueAction=[!SetOptionGroup CtrlHeart MeterStyle "EmptyHeart"][!UpdateMeterGroup CtrlHeart][!Redraw]
IfFalseAction=[!SetOptionGroup CtrlHeart MeterStyle "FullHeart"][!UpdateMeterGroup CtrlHeart][!Redraw]
UpdateDivider=1
Group=Music | modern | Heart
DynamicVariables=1
