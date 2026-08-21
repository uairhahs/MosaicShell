[Variables]
BarHandler=11

[ContentContainer]
Y=(#Set.TitleBarH#+#Set.P#/2)
Shape=Rectangle (0),(0),(#Set.W#-#Set.P#*3-30*[Set.S]),(#Set.H#-#Set.P#-#Set.TitleBarH#),(#Set.R#/2),(#Set.R#/2) | StrokeWidth 0 | Fill Color 255,255,255

[Image]
Meter=Image
X=0
W=[ContentContainer:W]
H=[ContentContainer:H]
Container=ContentContainer
DynamicVariables=1
ImageName=#@#Images\CoreTexture.png
ImageAlpha=50
PreserveAspectRatio=2

[Header]
Meter=String
X=([ContentContainer:W]/2)
Y=([ContentContainer:H]/2)
DynamicVariables=1
StringAlign=CenterCenter
Text=Need help?
FontFace=#Set.Font#
FontWeight=650
FontSize=(42*[Set.S])
MeterStyle=Set.String:S
Container=ContentContainer

[Option1.Shape]
Meter=Shape
X=(-400/2*[Set.S])r
Y=R
DynamicVariables=1
Shape=Rectangle 0,0,400,35,(#Set.R#) | StrokeWidth 0 | Scale [Set.S],[Set.S],0,0 | Extend THis
OverColor=255,255,255
LeaveColor=255,255,255
Leftmouseupaction=["https://mosaicshell.gitbook.io/core/getting-started/faqs"]
MeterStyle=Shape.Over:S
Container=ContentContainer
[Option1.String]
Meter=String
X=(400/2*[Set.S])r
y=(35/2*[Set.S])r
FontSize=(12*[Set.S])
FontFace=#Set.Font#
FontColor=59, 69, 78
StringAlign=CenterCenter
Text=Check out the FAQs
Container=ContentContainer
MeterStyle=SEt.String:S | ButtonT:S

[Option2.Shape]
Meter=Shape
X=(-400/2*[Set.S])r
Y=((35/2+10)*[Set.S])r
DynamicVariables=1
Shape=Rectangle 0,0,400,35,(#Set.R#) | StrokeWidth 0 | Scale [Set.S],[Set.S],0,0 | Extend THis
OverColor=#Set.Accent_Color_2#
LeaveColor=#Set.Accent_Color#
Leftmouseupaction=["https://github.com/uairhahs/MosaicShell/issues"]
MeterStyle=Shape.Over:S
Container=ContentContainer
[Option2.String]
Meter=String
X=(400/2*[Set.S])r
y=(35/2*[Set.S])r
FontSize=(12*[Set.S])
FontFace=#Set.Font#
StringAlign=CenterCenter
Text=Open an issue on GitHub
Container=ContentContainer
MeterStyle=SEt.String:S | ButtonT:S

[Option3.Shape]
Meter=Shape
X=(-400/2*[Set.S])r
Y=((35/2+10)*[Set.S])r
DynamicVariables=1
Shape=Rectangle 0,0,400,35,(#Set.R#) | StrokeWidth 0 | Scale [Set.S],[Set.S],0,0 | Extend THis
OverColor=#Set.Ter_Color#
LeaveColor=#Set.Sec_Color#
Leftmouseupaction=["https://github.com/uairhahs/MosaicShell"]
MeterStyle=Shape.Over:S
Container=ContentContainer
[Option3.String]
Meter=String
X=(400/2*[Set.S])r
y=(35/2*[Set.S])r
FontSize=(12*[Set.S])
FontFace=#Set.Font#
StringAlign=CenterCenter
Text=Visit MosaicShell on GitHub
Container=ContentContainer
MeterStyle=SEt.String:S | ButtonT:S
