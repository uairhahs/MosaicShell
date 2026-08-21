[ContentContainer]
Y=(#Set.TitleBarH#+#Set.P#/2)
Shape=Rectangle (0),(0),(#Set.W#-#Set.P#*3-30*[Set.S]),(#Set.H#-#Set.P#-#Set.TitleBarH#),(#Set.R#),(#Set.R#) | StrokeWidth 0 | Fill Color 255,255,255

[Image]
Meter=Image
X=0
W=[ContentContainer:W]
H=[ContentContainer:H]
Container=ContentContainer
DynamicVariables=1
ImageName=#SKINSPATH##Skin.Name#\@Resources\Images\Overlay.png
ImageAlpha=50
MouseOverAction=[!SetOption #CURRENTSECTION# ImageAlpha 150][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# ImageAlpha 50][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseActionCursor=0
PreserveAspectRatio=2

[Header]
Meter=

[Header2]
Meter=String
X=([ContentContainer:W]/2)
Y=([ContentContainer:H]/2)
W=([ContentContainer:W]-#Set.P#*2)
H=(500*[Set.S])
ClipString=2
DynamicVariables=1
StringAlign=CenterCenter
Text=Layering effects and overlays
FontFace=#Set.Font#
FontWeight=650
FontSize=(42*[Set.S])
MeterStyle=Set.String:S
Container=ContentContainer
