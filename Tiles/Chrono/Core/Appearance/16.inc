; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;


[DefaultStartActions]
Custom1=[!SetOption #Style_Render# MeterStyle "Render:S | Render:Selected"][!UpdateMeter #Style_Render#][!Redraw]

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
[Render:S]
X=(15*[Set.S])R
Y=r
W=(([ContentContainer:W]-55*[Set.S])/2)
DynamicVariables=1
Container=ContentContainer
ImageAlpha=50
ImageName="#SKINSPATH#ModularCLocks\@Resources\Images\Preview\Render\16#CURRENTSECTION#.png"
[Render:Selected]
ImageAlpha=255

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Option02]
Meter=String
Text=Clock font
MeterStyle=Set.String:S | Set.OptionName:S
[Divider19wd]
Meter=Shape
MeterStyle=Set.Div:S

[Item01.Shape]
Meter=Shape
Leftmouseupaction=["https://docs.mosaicshell.io/core/getting-started/additional-requisites#imagemagick"]
MeterStyle=Item.Shape:S
[Item01.StringIcon]
Meter=String
Text=[\xe7ba]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item01.String]
Meter=String
Text=The blur style requires ImageMagick! Click here to learn how to install it.
MeterStyle=Set.String:S | Item.String:S
[Item01.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Blur]
Meter=Image
X=(30*[Set.S])
Y=((30+50/2)*[Set.S])r
Leftmouseupaction=[!CommandMeasure Func "PopupImageMagick('#CURRENTSECTION#', 'BangToRun')"]
BangToRun=[!WriteKeyValue Variables Style_Render #CURRENTSECTION# "#Sec.SaveLocation#"][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Render:S
[Color]
Meter=Image
Leftmouseupaction=[!WriteKeyValue Variables Style_Render #CURRENTSECTION# "#Sec.SaveLocation#"][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Render:S

[AdditionalOptions]
Meter=String
Text=Render style settings
MeterStyle=Set.String:S | Set.OptionCat:S

[Item02.Shape]
Meter=Shape
Leftmouseupaction=[!WriteKeyValue Variables Cache.Wallpaper "" "#SKINSPATH##Skin.Name#\@Resources\Vars.inc"][!Refresh "#Skin.Name#\Main"]
MeterStyle=Item.Shape:S
[Item02.StringIcon]
Meter=String
Text=[\xe72c]
MeterStyle=Set.String:S | Item.StringIcon:S
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
[Item02.String]
Meter=String
Text=Click here to regenerate blur
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
MeterStyle=Set.String:S | Item.String:S
[Item02.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])

[Divider132sdsd8]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text=Blur modulation (Brightness, Saturation, Hue)
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
MeterStyle=Set.String:S | Set.OptionName:S
[Divider132sdasd8]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Blur sigma
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
MeterStyle=Set.String:S | Set.OptionName:S
[Divider192weewe03s8]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Blur radius
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
MeterStyle=Set.String:S | Set.OptionName:S
[Divider193sds8]
Meter=Shape
MeterStyle=Set.Div:S
[Option14]
Meter=String
Text=White overlay opacity on date
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
MeterStyle=Set.String:S | Set.OptionName:S
[Divider193sds58]
Meter=Shape
MeterStyle=Set.Div:S
[Option15]
Meter=String
Text=White overlay opacity on clock
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Blur', '1', '0')])
MeterStyle=Set.String:S | Set.OptionName:S
[Divider193s1ds8]
Meter=Shape
MeterStyle=Set.Div:S
[Option16]
Meter=String
Text=Fill color
FontColor=#Set.Text_Color#,(155+150*[&Func:returnBool('Style_Render', 'Color', '1', '0')])
MeterStyle=Set.String:S | Set.OptionName:S

[ShadowOptions]
Meter=String
Text=Shadow settings
MeterStyle=Set.String:S | Set.OptionCat:S
[Option21]
Meter=String
Text=Shadow strength
MeterStyle=Set.String:S | Set.OptionName:S
; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;
[FontFace]
Meter=Shape
MeterStyle=Set.Dropdown:S
Y=([Option02:Y]-#Set.P#+(-30/2+8)*[Set.S])
Leftmouseupaction=[!CommandMeasure Func "startDrop([[#SKINSPATH##Skin.Name#\Core\Dropdown\16Font.inc]], '#CURRENTSECTION#', '#skin.Name#', 'FontFace')"]
[Value02]
Meter=String
Text=#FontFace#
MeterStyle=Set.String:S | Set.Value:S

[BlurModulate]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]
[Value11]
Meter=String
Text=#BlurModulate#
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]
MeterStyle=Set.String:S | Set.Value:S

[BlurSigma]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]
[Value12]
Meter=String
Text=#BlurSigma#
MeterStyle=Set.String:S | Set.Value:S
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]

[BlurRadius]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]
[Value13]
Meter=String
Text=#BlurRadius#
MeterStyle=Set.String:S | Set.Value:S
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]

[Whitening2]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option14:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]
[Value14]
Meter=String
Text=#Whitening2#
MeterStyle=Set.String:S | Set.Value:S
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]

[Whitening]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option15:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]
[Value15]
Meter=String
Text=#Whitening#
MeterStyle=Set.String:S | Set.Value:S
Hidden=[&Func:returnBool('Style_Render', 'Blur', '0', '1')]

[FontColor]
Meter=Shape
MeterStyle=Set.Color:S
Y=([Option16:Y]-#Set.P#+(-30/2+8)*[Set.S])
Hidden=[&Func:returnBool('Style_Render', 'Color', '0', '1')]

[ShadowStrength]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option21:Y]-#Set.P#+(-30/2+8)*[Set.S])
Type=Num|0|255
[Value21]
Meter=String
Text=#ShadowStrength#
MeterStyle=Set.String:S | Set.Value:S

; ---------------------------------------------------------------------------- ;
;                                    Anchor                                    ;
; ---------------------------------------------------------------------------- ;


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
