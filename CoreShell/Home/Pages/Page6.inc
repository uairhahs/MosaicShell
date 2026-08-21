
[Header.String]
Text=About Jax

[Text.1]
Meter=String
MeterStyle=Set.String:S
X=(#HomePage.P#)
Y=(#Set.P#+#Sec.selector_height#)
W=(#Set.W#-#HomePage.P#*2)
ClipString=2
Text=I'm a student who enjoys graphic design and coding!#CRLF##CRLF#MosaicShell is my passion project, which originates from my desire for good-looking customizable UI on Windows, which is very lacking to this day, even with Windows 11.#CRLF##CRLF#The support and appreciation I've received along the way motivate me to keep going and improve the user experience. So don't hesitate to message me about anything!#CRLF##CRLF#Some of my far future goals are to learn front-end web development and to be able to make MosaicShell standalone!

[Logo]
Meter=Image
ImageName=#@#Images\CoreAssets\Jax.png
X=(#Set.W#-#HomePage.P#-((64)*[Set.S]))
Y=((90/2-32)*[Set.S])
W=(64*[Set.S])
H=(64*[Set.S])
Container=BC
DynamicVariables=1
