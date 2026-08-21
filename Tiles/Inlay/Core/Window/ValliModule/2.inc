

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
Text=Shortcut modules
FontColor=#Set.Subtext_Color#
X=(#W#/2)
Y=(#Scroll#)r
DynamicVariables=1
InlineSetting=CharacterSpacing | 2 | 2
StringAlign=CenterCenter
MeterStyle=Sec.String:S


[Shortcuts.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*2)r
MeterStyle=Module.Shape:S
[Shortcuts.Image]
Meter=Image
MeterStyle=Module.Image:S
[Shortcuts.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Shortcuts.Description.String]
Meter=String
Text=Shortcuts module with multiple visual styles. (added in v3.5)
MEterStyle=Sec.String:S | Module.Description.STring:S

[Hotapps.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Hotapps.Image]
Meter=Image
MeterStyle=Module.Image:S
[Hotapps.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Hotapps.Description.String]
Meter=String
Text=Username and different mini-apps that you can access, including calendar and power control
MEterStyle=Sec.String:S | Module.Description.STring:S

[PowerControl.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[PowerControl.Image]
Meter=Image
MeterStyle=Module.Image:S
[PowerControl.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[PowerControl.Description.String]
Meter=String
Text=A simple array of power controls
MEterStyle=Sec.String:S | Module.Description.STring:S


[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
