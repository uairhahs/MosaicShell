; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[Pos_Button.Shape:S]
X=(170*[S])r
Y=(-30*[S])r
Shape=Rectangle 0,0,(300*[S]),(60*[S]),(#Set.R#) | StrokeWidth (2*[S]) | Extend Stroke |
Stroke=Stroke Color #Set.Positive_Color#,150 | Fill Color "0,0,0,1"
This=180 | #Set.Positive_Color#,50 ; 0 | #Set.Positive_Color#,25 ; 1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Positive_Color# | Fill LinearGradient This"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Positive_Color#,150 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
Dynamicvariables=1
[Button.String:S]
X=(150*[S])r
Y=(30*[S])r
W=(250*[S])
ClipString=2
FontWeight=650
FontSize=(12*[S])
StringAlign=CenterCenter

[Neu_Button.Shape:S]
X=(170*[S])r
Y=(-30*[S])r
Shape=Rectangle 0,0,(300*[S]),(60*[S]),(#Set.R#) | StrokeWidth (2*[S]) | Extend Stroke |
Stroke=Stroke Color #Set.Text_Color#,150 | Fill Color "0,0,0,1"
This=180 | #Set.Text_Color#,50 ; 0 | #Set.Text_Color#,25 ; 1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Text_Color# | Fill LinearGradient This"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Text_Color#,150 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
Dynamicvariables=1

[Neg_Button.Shape:S]
X=(170*[S])r
Y=(-30*[S])r
Shape=Rectangle 0,0,(300*[S]),(60*[S]),(#Set.R#) | StrokeWidth (2*[S]) | Extend Stroke |
Stroke=Stroke Color #Set.Accent_Color#,150 | Fill Color "0,0,0,1"
This=180 | #Set.Accent_Color#,50 ; 0 | #Set.Accent_Color#,25 ; 1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Accent_Color# | Fill LinearGradient This"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Accent_Color#,150 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
Dynamicvariables=1

[Dis_Button.Shape:S]
X=(170*[S])r
Y=(-30*[S])r
Shape=Rectangle 0,0,(300*[S]),(60*[S]),(#Set.R#) | StrokeWidth (2*[S]) | Extend Stroke |
Stroke=Stroke Color 89, 100, 242,150 | Fill Color "0,0,0,1"
This=180 | 89, 100, 242,50 ; 0 | 89, 100, 242,25 ; 1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color 89, 100, 242 | Fill LinearGradient This"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color 89, 100, 242,150 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
Dynamicvariables=1



[Box.Shape:S]
X=(170*[S])r
Y=(-30*[S])r
Shape=Rectangle 0,0,(300*[S]),(200*[S]),(#Set.R#) | StrokeWidth (2*[S]) | Extend Stroke |
Stroke=Stroke Color #Set.Text_Color#,150 | Fill Color "0,0,0,1"
This=180 | #Set.Text_Color#,50 ; 0 | #Set.Text_Color#,25 ; 1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Text_Color# | Fill LinearGradient This"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Text_Color#,150 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
Dynamicvariables=1
[Pos_Box.Shape:S]
X=(170*[S])r
Y=(-30*[S])r
Shape=Rectangle 0,0,(300*[S]),(200*[S]),(#Set.R#) | StrokeWidth (2*[S]) | Extend Stroke |
Stroke=Stroke Color #Set.Positive_Color#,150 | Fill Color "0,0,0,1"
This=180 | #Set.Positive_Color#,50 ; 0 | #Set.Positive_Color#,25 ; 1
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Positive_Color# | Fill LinearGradient This"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Positive_Color#,150 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
Dynamicvariables=1
[Box.String:S]
X=(150*[S])r
Y=(30*[S])r
W=(250*[S])
ClipString=2
FontWeight=650
FontSize=(18*[S])
StringAlign=Center
Inlinepattern=#CRLF#.*
INlineSetting=weight | 400
Inlinepattern2=#CRLF#.*
INlineSetting2=size | (12*[S])

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[B]
Shape=Rectangle 0,0, #W#, #H#, #WR# | StrokeWidth #B# | Fill Color [#Set.Pri_Color] | Stroke Color [#Set.Ter_Color]

[Header.Image]
Meter=Image
X=(#W#/2-300*[S]/2)
Y=(100*[S])
W=(300*[S])
H=(300*[S])
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "1" "#CURRENTPATH#Main.ini"][!Refresh]
DynamicVariables=1
Container=ContentContainer
ImageName=#SKINSPATH#Pulse\@Resources\Images\Pulse.png

[Header.String]
Meter=String
Text=Pulse Setup
X=(300*[S]/2)r
Y=R
FontSize=(32*[S])
FontWeight=650
StringAlign=Center
MeterStyle=Sec.String:S

[Description.String]
Meter=String
FontSize=(16*[S])
X=r
Y=R
W=(#W#-#Set.P#*2)
ClipString=2
StringAlign=Center
InlinePattern=^$
InlineSetting=Color | #Set.Positive_Color#
MeterStyle=Sec.String:S


@includeContent=#SKINSPATH#Pulse\Core\Window\Setup\#Sec.Arg2#.inc
