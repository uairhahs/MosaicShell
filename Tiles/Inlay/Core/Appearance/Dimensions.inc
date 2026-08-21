
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
StringAlign=LeftCenter
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                    Conent                                    ;
; ---------------------------------------------------------------------------- ;
[Header01]
Meter=String
Text=Dimensions
MeterStyle=Set.String:S | Set.OptionCat:S

[Item1.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xe946]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Start menu too big? Decrease the scale and increase the width
MeterStyle=Set.String:S | Item.String:S

[Item2.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xe946]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Text=Font too small? Change the font multiplier in the elements appearance settings.
MeterStyle=Set.String:S | Item.String:S

[Divider02]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Scale
MeterStyle=Set.String:S | Set.OptionName:S
[Divider03]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Width

MeterStyle=Set.String:S | Set.OptionName:S
[Divider04]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=Minium height

MeterStyle=Set.String:S | Set.OptionName:S
[Divider05]
Meter=Shape
MeterStyle=Set.Div:S
[Option05]
Meter=String
Text=Border size

MeterStyle=Set.String:S | Set.OptionName:S
[Divider06]
Meter=Shape
MeterStyle=Set.Div:S
[Option06]
Meter=String
Text=Corner roundness

MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[Scale]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0.5|2


Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#Scale#

MeterStyle=Set.String:S | Set.Value:S
[FixW]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|100|#SCREENAREAWIDTH#


Y=([Option03:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value03]
Meter=String
Text=#FixW#

MeterStyle=Set.String:S | Set.Value:S
[MinH]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0|#SCREENAREAHEIGHT#


Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value04]
Meter=String
Text=#MinH#

MeterStyle=Set.String:S | Set.Value:S
[WinB]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0|50


Y=([Option05:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value05]
Meter=String
Text=#WinB#

MeterStyle=Set.String:S | Set.Value:S
[WinR]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0|100


Y=([Option06:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value06]
Meter=String
Text=#WinR#

MeterStyle=Set.String:S | Set.Value:S

[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
