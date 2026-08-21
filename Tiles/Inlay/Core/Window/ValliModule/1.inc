

[Top.Div]
Meter=Shape
X=#Sec.P#
Y=(#Sec.P#*2+40*[S])
Shape=Line 0,0,(#W#-#Sec.P#*2),0 | StrokeWidth 4 | Fill Color #Set.Pri_Color#,0 | Stroke LinearGradient This
This=0 | #Set.Text_Color#,25 ; 0.0 | #Set.Text_Color#,0 ; 0.5 | #Set.Text_Color#,25 ; 1.0
DynamicVariables=1
Container=ContentContainer

[Top.String]
Meter=String
Text=Welcome modules
FontColor=#Set.Subtext_Color#
X=(#W#/2)
Y=(#Scroll#)r
DynamicVariables=1
InlineSetting=CharacterSpacing | 2 | 2
StringAlign=CenterCenter
MeterStyle=Sec.String:S

[TimeDate.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*2)r
MeterStyle=Module.Shape:S
[TimeDate.Image]
Meter=Image
MeterStyle=Module.Image:S
[TimeDate.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[TimeDate.Description.String]
Meter=String
Text=New time date header
MEterStyle=Sec.String:S | Module.Description.STring:S

[GenericHeader.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[GenericHeader.Image]
Meter=Image
MeterStyle=Module.Image:S
[GenericHeader.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[GenericHeader.Description.String]
Meter=String
Text=The default header module for Inlay.
MEterStyle=Sec.String:S | Module.Description.STring:S

[SearchBar.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[SearchBar.Image]
Meter=Image
MeterStyle=Module.Image:S
[SearchBar.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[SearchBar.Description.String]
Meter=String
Text=A button which sends a hotkey, which the default key is to open the search menu.
MEterStyle=Sec.String:S | Module.Description.STring:S

[Weather.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*3+20*[S])r
MeterStyle=Module.Shape:S
[Weather.Image]
Meter=Image
MeterStyle=Module.Image:S
[Weather.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Weather.Description.String]
Meter=String
Text=Shows Today's and Tomorrow's weather and temperature
MEterStyle=Sec.String:S | Module.Description.STring:S


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
