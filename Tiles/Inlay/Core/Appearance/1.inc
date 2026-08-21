; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Item.Shape:S]
X=(([ContentContainer:W]-#Set.P#-20*[Set.S])/(3*2)+10*[Set.S])r
Y=(-([ContentContainer:W]-#Set.P#-20*[Set.S])/3/4*3)r
Shape=Rectangle 0,0,(([ContentContainer:W]-#Set.P#-20*[Set.S])/3),(([ContentContainer:W]-#Set.P#-20*[Set.S])/3),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Item.Icon.String:S]
X=(([ContentContainer:W]-#Set.P#-20*[Set.S])/3/2)r
Y=(([ContentContainer:W]-#Set.P#-20*[Set.S])/3/2)r
DynamicVariables=1
StringAlign=CenterCenter
FontFace=Segoe Fluent Icons
FontSize=(64*[Set.S])

Container=ContentContainer
[Item.String:S]
X=r
Y=(([ContentContainer:W]-#Set.P#-20*[Set.S])/3/2/2)r
StringAlign=CenterCenter
Text=N/A

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header01]
Meter=String
Text=What do you want to customize?
MeterStyle=Set.String:S | Set.OptionCat:S

[Item1.Shape]
Meter=Shape
X=(20*[Set.S])
Y=(40*[SEt.S])r
LeftMouseUpAction=[!WriteKeyvalue Variables page.page Dimensions "#SKINSPATH##Skin.Name#\Core\Appearance.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item1.Icon.String]
Meter=String
Text=[\xf575]
MeterStyle=Set.String:S | Item.Icon.String:S
[Item1.String]
Meter=String
Text=Dimensions
MeterStyle=Set.String:S | Item.String:S

[Item2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables page.page Elements "#SKINSPATH##Skin.Name#\Core\Appearance.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item2.Icon.String]
Meter=String
Text=[\xf57d]
MeterStyle=Set.String:S | Item.Icon.String:S
[Item2.String]
Meter=String
Text=Elements
MeterStyle=Set.String:S | Item.String:S

[Item3.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables page.page Fills "#SKINSPATH##Skin.Name#\Core\Appearance.inc"][!Refresh]
MeterStyle=Item.Shape:S
[Item3.Icon.String]
Meter=String
Text=[\xe790]
MeterStyle=Set.String:S | Item.Icon.String:S
[Item3.String]
Meter=String
Text=Background and fill
MeterStyle=Set.String:S | Item.String:S

[AnchorSuppli]
Meter=String
Container=ContentContainer
