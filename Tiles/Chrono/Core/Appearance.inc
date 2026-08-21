[Variables]
Sec.SaveLocation=#SKINSPATH##Skin.Name#\Main\Styles\Vars\#Style#.inc
Sec.AnchorName=AnchorSuppli


[Input2:M]
Measure=Plugin
Plugin=InputText
SolidColor=#Set.Pri_Color#
FontColor=#Set.Text_Color#
FontFace=Poppins
FontSize=(10*[Set.S])
StringAlign=LeftCenter
W=((150-10)*[Set.S])
H=((30-10)*[Set.S])
DynamicVariables=1
DefaultValue="[##Editing#]"
Command1=[!WriteKeyValue Variables "#Editing#" """$UserInput$""" "#Sec.SaveLocation2#"][!Refresh]


; ---------------------------------------------------------------------------- ;
;                                 Style options                                ;
; ---------------------------------------------------------------------------- ;

[SmartOptions]
Meter=String
Text=Options for "#Style#" style
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Widget size
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                 Global input                                 ;
; ---------------------------------------------------------------------------- ;

[Scale]
Meter=Shape
MeterStyle=Set.Textbox:S
Type=Num|0.5|3
Y=([Option01:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value01]
Meter=String
Text=#Scale#x
MeterStyle=Set.String:S | Set.Value:S

[Divider1328]
Meter=Shape
X=(30*[SEt.S])
Y=(30*[Set.S])r
MeterStyle=Set.Div:S

@includeCore=#SKINSPATH##Skin.Name#\Core\Appearance\#Style#.inc

@includeStyleVars=#SKINSPATH##Skin.Name#\Main\Styles\Vars\#Style#.inc
