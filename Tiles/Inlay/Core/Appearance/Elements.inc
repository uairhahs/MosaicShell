
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
[Item.Arrow.String:S]
X=([ContentContainer:W]-#Set.P#)
Y=r
StringAlign=RightCenter
Text=>
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header08]
Meter=String
Text=Elements
MeterStyle=Set.String:S | Set.OptionCat:S

[Item1.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables page.subpagemodule "Shortcuts" "#SKINSPATH##Skin.Name#\Core\Modules.inc"][!WriteKeyvalue Variables page.page 2 "#SKINSPATH##Skin.Name#\Core\Modules.inc"][!WriteKeyvalue Variables "Skin.Set_Page" "Modules" "#@#CacheVars\Configurator.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xe89f]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Trying to change how shortcut looks? Click here!
MeterStyle=Set.String:S | Item.String:S
[Item1.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Divider09]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option09]
Meter=String
Text=Font face
MeterStyle=Set.String:S | Set.OptionName:S
[Divider10]
Meter=Shape
MeterStyle=Set.Div:S
[Option10]
Meter=String
Text=Element corner roundness

MeterStyle=Set.String:S | Set.OptionName:S
[Divider11]
Meter=Shape
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text=Element edge strokes

MeterStyle=Set.String:S | Set.OptionName:S
[Divider12]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Draw divider lines between modules

MeterStyle=Set.String:S | Set.OptionName:S
[Divider13]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Font size multiplier

MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;
[Font]
Meter=Shape
MeterStyle=Set.Textbox:S



Y=([Option09:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value09]
Meter=String
Text=#Font#

MeterStyle=Set.String:S | Set.Value:S
[R]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0|100


Y=([Option10:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value10]
Meter=String
Text=#R#

MeterStyle=Set.String:S | Set.Value:S
[Stroke]
Meter=Shape
MeterStyle=Set.Bool:S


Y=([Option11:Y]-#Set.P#+(-20/2+8)*[Set.S])
[Divider]
Meter=Shape
MeterStyle=Set.Bool:S


Y=([Option12:Y]-#Set.P#+(-20/2+8)*[Set.S])
[FontSizeModifier]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0.2|1.5


Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value13]
Meter=String
Text=#FontSizeModifier#

MeterStyle=Set.String:S | Set.Value:S

[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
