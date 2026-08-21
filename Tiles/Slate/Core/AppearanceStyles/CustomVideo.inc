
[Choose:M]
Measure=Plugin
Plugin=FileChoose
UseNewStyle=0
GetTarget=0
AllowNonstorageFolder=1
GetIcon=0
ReturnValue=WholePath
FileInitialDirectory=
DynamicVariables=1
Command1=[!WriteKeyvalue Variables VideoPath "$Path$" "#Sec.SaveLocatioN#"][!WriteKeyvalue Variables Videoname "$Name$" "#Sec.SaveLocatioN#"][!Refresh]

[CheckffplayPowershell]
MEasure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile="#SKINSPATH##Skin.name#\Core\Checkffplay.ps1"

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

[ffplay]
Meter=String
Text=Requirements
MeterStyle=Set.String:S | Set.OptionCat:S

[Item1.Shape]
Meter=Shape
LeftMouseUpAction=["https://docs.mosaicshell.io/core/modules/idlestyle#video-backgrounds"]
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xf61c]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=An ffplay file is required for this style to work. Click here for an elaborate guide
MeterStyle=Set.String:S | Item.String:S
[Item1.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Item2.Shape]
Meter=Shape
MeterStyle=Item.Shape:S
[Item2.StringIcon]
Meter=String
Text=[\xf16a]
Group=Ava
MeterStyle=Set.String:S | Item.StringIcon:S
[Item2.String]
Meter=String
Text=Loading...
Group=Ava
MeterStyle=Set.String:S | Item.String:S

[HotkeysT2itle]
Meter=String
Text=Customization
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Video path
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S1913s138]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Play video once only
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S1913s12331138]
Meter=Shape
MeterStyle=Set.Div:S
[Option03]
Meter=String
Text=Video sound
MeterStyle=Set.String:S | Set.OptionName:S
[Set.Div:S1913123123s138]
Meter=Shape
MeterStyle=Set.Div:S
[Option04]
Meter=String
Text=Extended background color
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
[Button01]
Meter=Shape
MeterStyle=Set.Button:S
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
Act=[!CommandMeasure Choose:M "ChooseFile 1"]
OverColor=#Set.Accent_Color#,150
LeaveColor=#Set.Accent_Color#,50
[Value01]
Meter=String
Text=#VideoName#
MeterStyle=Set.String:S | Set.Value:S

[Loop]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option02:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Sound]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option03:Y]-#Set.P#+(-20/2+8)*[Set.S])

[Background]
Meter=Shape
MeterStyle=Set.ColorPacity:S
Y=([Option04:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value04]
Meter=String
Text=#BackgroundOpacity#
MeterStyle=Set.String:S | Set.Pacity:S
