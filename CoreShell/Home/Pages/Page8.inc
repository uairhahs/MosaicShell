[Header.Shape]
Meter=

[Title.Text1]
Meter=String
MeterStyle=Set.String:S
Text=Coming later
X=(#Set.P#+10*[Set.S])
Y=(#Set.H#*0.28)
Group=Scroll
FontSize=(20*[Set.S])
FontFace=#Set.Font#
StringAlign=LeftCenter
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
[Title.Text2]
Meter=String
MeterStyle=Set.String:S
Text=S-Hub
FontFace=#Set.Font#
X=r
Y=(80*[Set.s])r
FontSize=(60*[Set.S])
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
Text=S-Hub will be a place to share Windows setups, widgets, and themes so others can install what they see without hunting links.
[Text.2]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(10*[Set.S])R
W=(#Set.W#-350*[Set.S])
ClipString=2
Text=Share a layout once; others pull the same tiles and settings. Progress is tracked on GitHub.
[Text.3]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(10*[Set.S])R
W=(#Set.W#-350*[Set.S])
ClipString=2
Text=Not released yet — follow development on GitHub.
InlinePattern=Not released yet
inlinesetting=Color | #Set.Accent_Color_2#

[Logo]
Meter=Image
ImageName=#@#Images\CoreAssets\S-HubLogo256.png
X=(#Set.W#-#Set.P#-((256)*[Set.S]))
Y=(#Set.H#/2-(256/2*[Set.S])-#Set.Titlebarh#)
W=(256*[Set.S])
Container=BC
DynamicVariables=1
