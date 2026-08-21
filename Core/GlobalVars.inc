[Variables]
Sec.SaveLocation2="#@#gVars\Weather.inc"
Sec.SaveLocation3="#@#gVars\WeatherAPIKey.inc"
@includegVar1=#@#gVars\Time.inc
@includegVar2=#@#gVars\Weather.inc
@includegVar3=#@#gVars\WeatherAPIKey.inc

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

[Time]
Meter=String
Text=Time
MeterStyle=Set.String:S |  Set.OptionCat:S

[Option01]
Meter=String
Text=Time format
MeterStyle=Set.String:S | Set.OptionName:S

[Weather]
Meter=String
Text=Weather
MeterStyle=Set.String:S |  Set.OptionCat:S

[Item1.Shape]
Meter=Shape
LeftMouseUpAction=["https://mosaicshell.gitbook.io/core/getting-started/changing-global-options"]
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xea1f]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Learn how to change weather location properly here
MeterStyle=Set.String:S | Item.String:S
[Item1.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

; [Item2.Shape]
; Meter=Shape
; MeterStyle=Item.Shape:S
; [Item2.StringIcon]
; Meter=String
; Text=[\xe72c]
; MeterStyle=Set.String:S | Item.StringIcon:S
; [Item2.String]
; Meter=String
; Text=After changing, refresh the module / widget by right clicking it
; MeterStyle=Set.String:S | Item.String:S

[Set.Div:25158]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text=Weather temperature unit
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:24125158]
Meter=Shape
MeterStyle=Set.Div:S
[Option12]
Meter=String
Text=Weather location
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:58]
Meter=Shape
MeterStyle=Set.Div:S
[Option13]
Meter=String
Text=Weather language
MeterStyle=Set.String:S | Set.OptionName:S

[Advanced]
Meter=String
Text=Advanced
MeterStyle=Set.String:S |  Set.OptionCat:S

[Item3.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item3.StringIcon]
Meter=String
Text=[\xedae]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item3.String]
Meter=String
Text=Do not change these options unless you know what you're doing
MeterStyle=Set.String:S | Item.String:S

[Set.Div:252158]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S

[Option21]
Meter=String
Text=Weather.com API key
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[gVar.2412:]
Meter=Shape
Shape2=Rectangle ([&Func:returnBool('gVar.2412', 'H')]=0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables gVar.2412 "I" "#@#gVars\Time.inc"]
1Act=[!WriteKeyValue Variables gVar.2412 "H" "#@#gVars\Time.inc"]
MeterStyle=Set.Pick:S
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[gVar.2412:0]
Meter=String
Text=12-Hour
FontColor=[&Func:returnBool('gVar.2412', 'H', '#Set.Text_Color#', '#Set.Pri_color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
[gVar.2412:1]
Meter=String
Text=24-hour
FontColor=[&Func:returnBool('gVar.2412', 'H', '#Set.Pri_color#', '#Set.Text_Color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S

[gVar.WeatherUnits:]
Meter=Shape
Shape2=Rectangle ([&Func:returnBool('gVar.WeatherUnits', 'Imperial')]=0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |
0Act=[!WriteKeyValue Variables gVar.WeatherUnits "Metric" "#@#gVars\Weather.inc"]
1Act=[!WriteKeyValue Variables gVar.WeatherUnits "Imperial" "#@#gVars\Weather.inc"]
MeterStyle=Set.Pick:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
[gVar.WeatherUnits:0]
Meter=String
Text=Metric
FontColor=[&Func:returnBool('gVar.WeatherUnits', 'Imperial', '#Set.Text_Color#', '#Set.Pri_color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '0Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_0:S
[gVar.WeatherUnits:1]
Meter=String
Text=Imperial
FontColor=[&Func:returnBool('gVar.WeatherUnits', 'Imperial', '#Set.Pri_color#', '#Set.Text_Color#')]
LeftMouseUpAction=[&Func:GroupVar('#CURRENTSECTION#', '1Act')][!Refresh][!UpdateMeasure Auto_Refresh:M]
MeterStyle=Set.String:S | Set.PickOption_1:S

[gVar.WeatherLocation]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Txt
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
SaveLocation=2
[Value12]
Meter=String
Text=#gVar.WeatherLocation#
MeterStyle=Set.String:S | Set.Value:S

[gVar.language]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Txt
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
SaveLocation=2
[Value13]
Meter=String
Text=#gVar.language#
MeterStyle=Set.String:S | Set.Value:S

[gVar.WeatherAPIKey]
Meter=Shape
MeterStyle=Set.Textbox:S
Y=([Option21:Y]-#Set.P#+(-30/2+8)*[Set.S])
SaveLocation=3
[Value21]
Meter=String
Text=#gVar.WeatherAPIKey#
MeterStyle=Set.String:S | Set.Value:S

; ---------------------------------------------------------------------------- ;
;                                Core overrides                                ;
; ---------------------------------------------------------------------------- ;

[Auto_Refresh:M]
Measure=

[Help:Category]
Y=(#Set.H#-100*[Set.S])

[DivCat:1293s18]
Meter=
[ActiveChecker]
Measure=
[BottomButton]
Meter=
[BottomText]
Meter=
