[Header.Shape]
Meter=

[Title.Text1]
Meter=String
MeterStyle=Set.String:S
Text=About
X=(#Set.P#+10*[Set.S])
Y=(#Set.H#*0.33)
Group=Scroll
FontSize=(20*[Set.S])
FontFace=#Set.Font#
StringAlign=LeftCenter
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
[Title.Text2]
Meter=String
MeterStyle=Set.String:S
Text=MosaicShell
X=(-10*[Set.S])r
Y=(60*[Set.s])r
FontSize=(60*[Set.S])
FontFace=#Set.Font#
FontWeight=(650)
StringAlign=LeftCenter
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0

[Text.1]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(78*[Set.S])r
W=(#Set.W#-350*[Set.S])
ClipString=2
Text=MosaicShell is a modular desktop layer for widgets, utilities, and workflows. Arrange the pieces, tune the surface, and build an environment that fits the way you work.
[Text.2]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(10*[Set.S])R
W=(#Set.W#-350*[Set.S])
ClipString=2
Text=The hub is where you manage settings, updates, and tiles. Each module installs and updates independently.

[Logo]
Meter=Image
ImageName=#@#Images\CoreAssets\#Set.IconStyle#Logo256.png
X=(#Set.W#-#Set.P#-((256)*[Set.S]))
Y=(#Set.H#/2-(256/2*[Set.S])-#Set.Titlebarh#)
W=(256*[Set.S])
Container=BC
DynamicVariables=1
