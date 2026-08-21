
[Header.String]
Text=About

[Text.1]
Meter=String
MeterStyle=Set.String:S
X=(#HomePage.P#)
Y=(#Set.P#+#Sec.selector_height#)
W=(#Set.W#-#HomePage.P#*2)
ClipString=2
Text=MosaicShell is a configurable desktop shell built from composable surfaces.#CRLF##CRLF#Build your desktop from the pieces that matter. Widgets, utilities, and workflows you control.#CRLF##CRLF#This project is a hard fork of JaxCore by EnhancedJax (archived November 2024). Issues and contributions are welcome on GitHub.

[Logo]
Meter=Image
ImageName=#@#Images\CoreAssets\LogoIconAccent.png
X=(#Set.W#-#HomePage.P#-((64)*[Set.S]))
Y=((90/2-32)*[Set.S])
W=(64*[Set.S])
H=(64*[Set.S])
Container=BC
DynamicVariables=1
