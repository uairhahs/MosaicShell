[Variables]
@includeModulevars=#SKINSPATH##Skin.Name#\Main\Modules\Vars\#Page.SubpageModule#.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\Main\Modules\Vars\#Page.SubpageModule#.inc

[Item1.Shape]
Meter=
[Item1.StringIcon]
Meter=
[Item1.String]
Meter=

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Shortcuts.Shape:S]
X=(20*[Set.S])
Y=(40*[SEt.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),([ContentContainer:W]/4),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=ContentContainer
[Shortcuts.Image:S]
X=(10*[Set.S])r
Y=(10*[Set.S])r
W=([ContentContainer:W]-#Set.P#-20*[Set.S])
H=([ContentContainer:W]/4-20*[SEt.S])
ImageName=#SKINSPATH##Skin.Name#\Core\Shortcuts\[&Func:trim('#CURRENTSECTIOn#','.Image','')].png
DynamicVariables=1
Container=ContentContainer
LeftMouseUpAction=[!WriteKeyValue Variables ShortcutBoxStyles [&Func:trim('#CURRENTSECTIOn#','.Image','')] "#Sec.SaveLocation#"][!UpdateMeasure Auto_Refresh:M][!Refresh]
MaskImageName=#SKINSPATH##Skin.Name#\Core\Module\#ModuleMask.png
[Shortcuts.String:S]
X=(10*[Set.s])r
Y=(-10*[SEt.s])R
Text=[&Func:trim('#CURRENTSECTIOn#','.String','')]
StringAlign=LeftBottom
FontSize=((20*[Set.S])*#FontSizeModifier#)
FontWeight=650
Container=ContentContainer

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysTitle]
Meter=String
Text=#Page.SubpageModule# settings
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Shortcut visual styles
MeterStyle=Set.String:S | Set.OptionName:S

[Tiles.Shape]
Meter=Shape
MeterStyle=Shortcuts.Shape:S
[Tiles.Image]
Meter=Image
MeterStyle=Shortcuts.Image:S
[Tiles.String]
Meter=String
MEterStyle=Set.String:S | Shortcuts.STring:S

[Win11.Shape]
Meter=Shape
MeterStyle=Shortcuts.Shape:S
[Win11.Image]
Meter=Image
MeterStyle=Shortcuts.Image:S
[Win11.String]
Meter=String
MEterStyle=Set.String:S | Shortcuts.STring:S

[Bordered.Shape]
Meter=Shape
MeterStyle=Shortcuts.Shape:S
[Bordered.Image]
Meter=Image
MeterStyle=Shortcuts.Image:S
[Bordered.String]
Meter=String
MEterStyle=Set.String:S | Shortcuts.STring:S

[Set.Div:S132]
Meter=Shape
X=(20*[Set.S])
MeterStyle=Set.Div:S

[Option02]
Meter=String
Text=Bordered style padding
MeterStyle=Set.String:S | Set.OptionName:S

[BorderedPadding]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value02]
Meter=String
Text=#BorderedPadding#
MeterStyle=Set.String:S | Set.Value:S
