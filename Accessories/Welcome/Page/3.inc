[Variables]
width=(256*#Sec.S#)
imgwidth=(75*#sec.s#)
TweenNode3=0
TweenNode4=0

; ---------------------------------------------------------------------------- ;
;                                  MeterStyleS                                 ;
; ---------------------------------------------------------------------------- ;

[Support:S]
DynamicVariables=1
Container=a.Container
[SupportImage:S]
X=((#width#/2-#imgwidth#/2))r
Y=((#width#/2-#imgwidth#/2))r
W=(#imgwidth#)
H=(#imgwidth#)
DynamicVariables=1
Container=a.Container
[SupportText:S]
FontSize=(10*#Sec.S#)
Inlinepattern=#CRLF#.*
InlineSetting=Weight | 650
Inlinepattern2=#CRLF#.*
InlineSetting2=Size | (20*#Sec.S#)
Inlinepattern3=#CRLF#.*
StringAlign=CenterCenter
FontColor=#Set.Subtext_Color#
DynamicVariables=1
Container=a.Container

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Text]
Meter=String
Text=Join our growing community!#CRLF#MosaicShell is in active development!
X=(#Sec.W#/2)r
Y=#Sec.WindowPaddingY#
FontSize=(24*#Sec.S#)
InlinePattern=.*#CRLF#
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 1.0 | #Set.Accent_Color_2# ; 0.0
INlinePattern2=#CRLF#.*
InlineSetting2=Size | (10*#Sec.S#)
INlinePattern3=#CRLF#.*
InlineSetting3=Color | 160,155,145
INlinePattern4=#CRLF#.*
InlineSetting4=Weight | 400
StringAlign=center
MeterStyle=Sec.String:S
FontWeight=650

; --------------------------------- Section1 --------------------------------- ;

[Section1.Shape]
Meter=Shape
X=#Sec.WindowPaddingX#
Y=(15*#Sec.S#)R
Shape=Rectangle 0,0,(#Sec.W#-#Sec.WindowPaddingX#*2),(130*#Sec.S#),(#Sec.R#/2) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=a.Container
LeftMouseUpAction=["https://discord.gg/JmgehPSDD6"]

[Section1.Image]
Meter=Image
X=(#Sec.P#)r
Y=(130*#Sec.S#/2-50*#Sec.S#/2)r
W=(50*#Sec.S#)
H=(50*#Sec.S#)
ImageName=#@#Icons\Discord.png
Container=a.Container

[Section1.String]
Meter=String
X=(75*#Sec.S#)r
Y=(50*#Sec.S#/2)r
W=(#Sec.W#-#Sec.WindowPaddingX#*2-#Sec.P#*2-#Sec.S#*75)
Text=Join CoreCommunity's Discord server!#CRLf#This is MosaicShell's homebase. Get to know everything about MosaicShell, from the past to the future.
StringAlign=LeftCenter
FontWeight=650
FontSize=(12*#Sec.S#)
InlinePattern=#CRLF#.*
InlineSetting=Color | #Set.SubText_Color#
InlinePattern2=#CRLF#.*
InlineSetting2=Weight | 400
InlinePattern3=#CRLF#.*
InlineSetting3=Size | (10*#Sec.S#)
MeterStyle=Sec.String:S

; --------------------------------- Section2 --------------------------------- ;

[Section2.Shape]
Meter=Shape
X=#Sec.WindowPaddingX#
Y=(15*#Sec.S#)R
Shape=Rectangle 0,0,(#Sec.W#-#Sec.WindowPaddingX#*2),(130*#Sec.S#),(#Sec.R#/2) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=a.Container
LeftMouseUpAction=["https://www.deviantart.com/jaxoriginals"]

[Section2.Image]
Meter=Image
X=(#Sec.P#)r
Y=(130*#Sec.S#/2-50*#Sec.S#/2)r
W=(50*#Sec.S#)
H=(50*#Sec.S#)
ImageName=#@#Images\DA.png
Container=a.Container

[Section2.String]
Meter=String
X=(75*#Sec.S#)r
Y=(50*#Sec.S#/2)r
W=(#Sec.W#-#Sec.WindowPaddingX#*2-#Sec.P#*2-#Sec.S#*75)
Text=Watch me on DeviantArt!#CRLf#This is where I mainly release new modules and widgets!
StringAlign=LeftCenter
FontWeight=650
FontSize=(12*#Sec.S#)
InlinePattern=#CRLF#.*
InlineSetting=Color | #Set.SubText_Color#
InlinePattern2=#CRLF#.*
InlineSetting2=Weight | 400
InlinePattern3=#CRLF#.*
InlineSetting3=Size | (10*#Sec.S#)
MeterStyle=Sec.String:S



[SubtleText]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.H#-#Sec.P#)
FontColor=#Set.Subtext_Color#
StringAlign=CenterBottom
Text=Continue
MouseOverAction=[!SetOption #CURRENTSECTION# InlineSetting "Underline"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# InlineSetting ""][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure p.ActionTimer "Execute 2"]
MeterStyle=Sec.String:S
