

[Item02.Shape]
Meter=Shape
Leftmouseupaction=[!WriteKeyValue Variables Cache.Wallpaper "" "#SKINSPATH##Skin.Name#\@Resources\Vars.inc"][!Refresh "#Skin.Name#\Main"]
MeterStyle=Item.Shape:S
[Item02.StringIcon]
Meter=String
Text=[\xe72c]
MeterStyle=Set.String:S | Item.StringIcon:S
[Item02.String]
Meter=String
Text=Click here to regenerate blur
MeterStyle=Set.String:S | Item.String:S
[Item02.Arrow.String]
Meter=String
MeterStyle=Set.String:S | Item.Arrow.String:S

[Divider132sdsd8]
Meter=Shape
X=(30*[Set.S])
MeterStyle=Set.Div:S
[Option11]
Meter=String
Text=Modulate (Brightness, Saturation, Hue)
MeterStyle=Set.String:S | Set.OptionName:S
[Divider132sdasd8]
Meter=Shape
MeterStyle=Set.Div:S

[Option12]
Meter=String
Text=Sigma
MeterStyle=Set.String:S | Set.OptionName:S
[Divider192weewe03s8]
Meter=Shape
MeterStyle=Set.Div:S

[Option13]
Meter=String
Text=Radius
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[BlurModulate]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option11:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value11]
Meter=String
Text=#BlurModulate#
MeterStyle=Set.String:S | Set.Value:S

[BlurSigma]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option12:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value12]
Meter=String
Text=#BlurSigma#
MeterStyle=Set.String:S | Set.Value:S

[BlurRadius]
Meter=Shape
MeterStyle=Set.TextBox:S
Y=([Option13:Y]-#Set.P#+(-30/2+8)*[Set.S])
[Value13]
Meter=String
Text=#BlurRadius#
MeterStyle=Set.String:S | Set.Value:S
