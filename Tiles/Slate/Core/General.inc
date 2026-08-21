﻿
; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;


[Item.Shape:S]
X=(30*[Set.S])
Y=(35*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.StringIcon:S]
X=(50*[Set.S]/2)r
Y=(50*[Set.S]/2)r
StringAlign=leftCenter
FontFace=Segoe Fluent Icons
Container=ContentContainer
[Item.String:S]
X=(#Set.P#/2)R
Y=r
StringAlign=LeftCenter
Container=ContentContainer
[Item.Arrow.String:S]
X=([ContentContainer:W]-#Set.P#)
Y=r
StringAlign=RightCenter
Text=>
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[IdleTiming]
Meter=String
Text=Activation
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Hotkey for pausing Slate
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:382]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Hotkey for activating Slate forcefully
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:38]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Ignore idle duration and manually activate Slate forcefully
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:44438]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=Time till activation
FontColor=#Set.Text_Color#,(255-105*#IgnoreIdleDuration#)
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:444]
Meter=Shape
MeterStyle=Set.Div:S
[Option05]
Meter=String
Text=Show Slate background activeness notification
MeterStyle=Set.String:S | Set.OptionName:S

[Time]
Meter=String
Text=Time & weather
MeterStyle=Set.String:S |  Set.OptionCat:S

[Item01.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables Skin.Name """#MosaicShell""" "#@#SecVar.inc"][!WriteKeyvalue Variables Skin.Set_Page GlobalVars "#@#SecVar.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item01.StringIcon]
Meter=String
Text=[\xe775]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item01.String]
Meter=String
Text=Change time format in MosaicShell global settings.
MeterStyle=Set.String:S | Item.String:S
[Item01.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Item02.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables Skin.Name """#MosaicShell""" "#@#SecVar.inc"][!WriteKeyvalue Variables Skin.Set_Page GlobalVars "#@#SecVar.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item02.StringIcon]
Meter=String
Text=[\xe774]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item02.String]
Meter=String
Text=Change weather location & units in MosaicShell global settings.
MeterStyle=Set.String:S | Item.String:S
[Item02.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Checks]
Meter=String
Text=Idle checks
MeterStyle=Set.String:S |  Set.OptionCat:S

[Item03.Shape]
Meter=Shape
LeftMouseUpAction=["http://www.google.com/search?q=WebNowPlaying Companion"]
MeterStyle=Item.Shape:S
[Item03.StringIcon]
Meter=String
Text=[\xe72d]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item03.String]
Meter=String
Text=You'll need to install WebNowPlaying Companion extension on your browser for Youtube checks.
MeterStyle=Set.String:S | Item.String:S
[Item03.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Set.Div:31813123]
Meter=Shape
X=(30*[SEt.S])
MeterStyle=Set.Div:S
[Option31]
Meter=String
Text=Fullscreen check
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:318]
Meter=Shape
MeterStyle=Set.Div:S
[Option32]
Meter=String
Text=Youtube player check
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:1S1913s138]
Meter=Shape
Hidden=(1-#YoutubeCheck#)
MeterStyle=Set.Div:S
[Option33]
Meter=String
Text=Youtube state check
Hidden=(1-#YoutubeCheck#)
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[Button01]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey')"]
[Value01]
Meter=String
Text=#KeyInString#
MeterStyle=Set.String:S | Set.Value:S
[Button02]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Func "startSide('Hotkey', '2')"]
[Value02]
Meter=String
Text=#Key2InString#
MeterStyle=Set.String:S | Set.Value:S

[IgnoreIdleDuration]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option03:Y]-#Set.P#+(-20/2+8)*[Set.S])

[TimeOut]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Time
Hidden=#IgnoreIdleDuration#
TooltipTitle=Time till Slate shows
Tooltiptext=Enter in time format (x)h(x)m(x)s for the time needed for you to be idle to activate Slate.
Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value04]
Meter=String
Text=#TimeOut#
Hidden=#IgnoreIdleDuration#
MeterStyle=Set.String:S | Set.Value:S

[LoadedNotif]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option05:Y]-#Set.P#+(-20/2+8)*[Set.S])

[FullScreenCheck]
Meter=Shape
MeterStyle=Set.Bool:S
ToolTipTitle=#CURRENTSECTION#
ToolTipText=Checks if there are any full screen applications to prevent Slate from showing when, for example you're gaming
Y=([Option31:Y]-#Set.P#+(-20/2+8)*[Set.S])
[YoutubeCheck]
Meter=Shape
MeterStyle=Set.Bool:S
ToolTipTitle=#CURRENTSECTION#
ToolTipText=Checks if you are on the youtube website. This requires the WebNowPlaying addon to be installed on your browser
Y=([Option32:Y]-#Set.P#+(-20/2+8)*[Set.S])
[YoutubeState]
Meter=Shape
MeterStyle=Set.Bool:S
Hidden=(1-#YoutubeCheck#)
ToolTipTitle=#CURRENTSECTION#
ToolTipText=Checks if youtube is playing media. This requires the previous option to be turned on.
Y=([Option33:Y]-#Set.P#+(-20/2+8)*[Set.S])


@includeKeys=#SKINSPATH##Skin.Name#\@Resources\Actions\HotKeys.ini
