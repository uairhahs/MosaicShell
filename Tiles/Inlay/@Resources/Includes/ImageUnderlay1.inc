[BackgroundContainer]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,#W#,#H#,(#WinR#*#scale#) | StrokeWidth (#WinB#*2) | Fill Color #Pri_color#,#ImageUnderlayAlpha#
DynamicVariables=1
[ImageUnderlay]
Meter=Image
W=#W#
H=#H#
PreserveAspectRatio=2
ImageName=#ImageUnderlayPath#
container=BackgroundContainer
DynamicVariables=1
