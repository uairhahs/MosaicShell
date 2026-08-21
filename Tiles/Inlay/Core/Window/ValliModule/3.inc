

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
Text=Informational modules
FontColor=#Set.Subtext_Color#
X=(#W#/2)
Y=(#Scroll#)r
DynamicVariables=1
InlineSetting=CharacterSpacing | 2 | 2
StringAlign=CenterCenter
MeterStyle=Sec.String:S

[Connection.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*2)r
MeterStyle=Module.Shape:S
[Connection.Image]
Meter=Image
MeterStyle=Module.Image:S
[Connection.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Connection.Description.String]
Meter=String
Text=Connection module to toggle Wifi, Bluetooth and Internet access
MEterStyle=Sec.String:S | Module.Description.STring:S

[Mon.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Mon.Image]
Meter=Image
MeterStyle=Module.Image:S
[Mon.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Mon.Description.String]
Meter=String
Text=Change your monitor configuration on the fly
MEterStyle=Sec.String:S | Module.Description.STring:S

[Music.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Music.Image]
Meter=Image
MeterStyle=Module.Image:S
[Music.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Music.Description.String]
Meter=String
Text=Music control which can switch between different types of media players, and has the album art as the background
MEterStyle=Sec.String:S | Module.Description.STring:S

[Resource.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*3+20*[S])r
MeterStyle=Module.Shape:S
[Resource.Image]
Meter=Image
MeterStyle=Module.Image:S
[Resource.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Resource.Description.String]
Meter=String
Text=Shows the percentage of usage for CPU, RAM and GPU
MEterStyle=Sec.String:S | Module.Description.STring:S

[Volume.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Volume.Image]
Meter=Image
MeterStyle=Module.Image:S
[Volume.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Volume.Description.String]
Meter=String
Text=Change the volume by sliding the slider or scrolling, and change your playback device
MEterStyle=Sec.String:S | Module.Description.STring:S




[AnchorSuppli]
Meter=String
Container=ContentContainer
x=r
Y=R
