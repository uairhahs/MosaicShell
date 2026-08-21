[Header.Shape]
Meter=

[Title.Text1]
Meter=String
MeterStyle=Set.String:S
Text=The next big thing from MosaicShell
X=(#Set.P#+10*[Set.S])
Y=(#Set.H#*0.28)
Group=Scroll
FontSize=(20*[Set.S])
FontFace=Righteous
StringAlign=LeftCenter
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
[Title.Text2]
Meter=String
MeterStyle=Set.String:S
Text=S-Hub
FontFace=Righteous
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
Text=S-Hub is a place for you to share your Windows themed setups, as well as sole widgets or themes, for your friends & other themers to download & use.
[Text.2]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(10*[Set.S])R
W=(#Set.W#-350*[Set.S])
ClipString=2
Text=Imagine a world where you can get what you see instantly. The chore of getting links to every individual thing is eliminated. No more “What’s that clock?” or “What’s my theme again?”, whether it’s getting or sharing, they’re now here within the reach of a few clicks. S-Hub is your one-stop shop for all creative themers.
[Text.3]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(10*[Set.S])R
W=(#Set.W#-350*[Set.S])
ClipString=2
Text=S-Hub is estimated to be released during / before Winter Holiday. Follow progress on GitHub.
InlinePattern=during / before Winter Holiday
inlinesetting=Color | #Set.Accent_Color_2#

[Logo]
Meter=Image
ImageName=#@#Images\CoreAssets\S-HubLogo256.png
X=(#Set.W#-#Set.P#-((256)*[Set.S]))
Y=(#Set.H#/2-(256/2*[Set.S])-#Set.Titlebarh#)
W=(256*[Set.S])
Container=BC
DynamicVariables=1
