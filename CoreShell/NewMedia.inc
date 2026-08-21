[Variables]
BarHandler=5.5

[Header]
Meter=String
X=(20*[Set.S])
Y=(20*[Set.S]+#Scroll#)
DynamicVariables=1
Text=Media
FontSize=(25*[Set.S])
FontWeight=600
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0
MeterStyle=Set.String:S
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
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
FontSize=(10*[Set.S])
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

[Item1.Shape]
Meter=Shape
X=(30*[Set.S])
Y=(70*[Set.S])r
LeftMouseUpAction=["https://mosaicshell.gitbook.io/core/getting-started/faqs/media-player-faqs#setting-up-media-players"]
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xea1f]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Learn how to setup your media player here
MeterStyle=Set.String:S | Item.String:S
[Item1.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S


; --------------------------------- Section2 --------------------------------- ;

[Section1]
Meter=Shape
X=(30*[Set.S])
Y=(35*[Set.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(100*[Set.S]),(#Set.R#/4) | StrokeWidth 0 | Fill Color #Set.Sec_Color#
Container=ContentContainer
DynamicVariables=1

[Section1.Icon]
Meter=String
Text=[\xec4f]
X=(#Set.P#)r
Y=(100/2*[Set.S])r
FontSize=(20*[set.s])
MeterStyle=Set.String:S | Set.Icon:S | Section1.Icon:#MediaType#
[Section1.Icon:Modern]
FontColor=#Set.Accent_Color#
InlineSetting=Shadow | 0 | 0 | 2 | #Set.Accent_Color#
[Section1.Icon:Auto]
FontColor=#Set.Accent_Color_2#
InlineSetting=Shadow | 0 | 0 | 2 | #Set.Accent_Color_2#

[Section1.Text]
Meter=String
X=(5*[Set.S])R
Y=r
W=([ContentContainer:W]-#Set.P#*4-300*[Set.S])
ClipString=2
FontSize=(10*[Set.S])
Container=ContentContainer
StringAlign=LeftCenter
InlinePattern=#CRLF#.*
InlineSetting=Color | #Set.SubText_Color#
MeterStyle=Set.String:S | Section1.Text:#MediaType#
[Section1.Text:Modern]
Text=Method: #MediaType##CRLF#Gets media status from all media players.
[Section1.Text:Auto]
Text=Method: #MediaType##CRLF#Switches between nowplaying and webnowplaying plugins.

[MediaType:]
Meter=Shape
Y=(-30/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-300*[Set.S])
Shape2=Rectangle ([&Func:returnBool('MediaType', 'Modern')] = 0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables MediaType "Auto" "#Sec.SaveLocation#"]
1Act=[!WriteKeyValue Variables MediaType "Modern" "#Sec.SaveLocation#"]
MeterStyle=Set.Pick:S
[MediaType:0]
Meter=String
Text=Auto
FontColor=[&Func:returnBool('MediaType', 'Modern', '#Set.Text_Color#', '#Set.Pri_Color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
[MediaType:1]
Meter=String
Text=Modern
FontColor=[&Func:returnBool('MediaType', 'Modern', '#Set.Pri_color#', '#Set.Text_Color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S

; --------------------------------- Section2 --------------------------------- ;

[Section2]
Meter=Shape
X=(30*[Set.S])
Y=(60*[Set.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(100*[Set.S]),(#Set.R#/4) | StrokeWidth 0 | Fill Color #Set.Sec_Color#
Container=ContentContainer
DynamicVariables=1

[Section2.Icon]
Meter=String
Text=[\xe7b3]
X=(#Set.P#)r
Y=(100/2*[Set.S])r
FontSize=(20*[set.s])
MeterStyle=Set.String:S | Set.Icon:S | Section2.Icon:#FetchImage#
[Section2.Icon:0]
FontColor=#Set.Accent_Color#
InlineSetting=Shadow | 0 | 0 | 2 | #Set.Accent_Color#
[Section2.Icon:1]
FontColor=#Set.Positive_Color#
InlineSetting=Shadow | 0 | 0 | 2 | #Set.Positive_Color#

[Section2.Text]
Meter=String
X=(5*[Set.S])R
Y=r
W=([ContentContainer:W]-#Set.P#*4-60*[Set.S])
ClipString=2
FontSize=(10*[Set.S])
Container=ContentContainer
StringAlign=LeftCenter
InlinePattern=#CRLF#.*
InlineSetting=Color | #Set.SubText_Color#
MeterStyle=Set.String:S | Section2.Text:#FetchImage#
[Section2.Text:0]
Text=Skip media art#CRLF#The skin will not fetch and display now playing album art
[Section2.Text:1]
Text=Download media art#CRLF#The skin will fetch and display now playing album art

[FetchImage]
Meter=Shape
MeterStyle=Set.Bool:S
Y=(-20/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-60*[Set.S])
