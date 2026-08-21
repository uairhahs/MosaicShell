[Shadow]
Measure=Plugin
Plugin=MagickMeter
DynamicVariables=1
Image=Rectangle 0,0,(#SW#-(#Stroke#)),(#SH#-(#Stroke#)),(#Rounding#*#Scale#),(#Rounding#*#Scale#) | InnerShadow 75,(10*#scale#),0,0,1 ; #MainColor#
UpdateDivider=-1


[ShadowImage]
Meter=Image
X=(#Stroke#)
Y=(#Stroke#)
MeasureName=Shadow
[ImageMeter]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#)
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#)
UpdateDivider=50
COntainer=ActualWave
[Hour]
Meter=
[Hour2]
Meter=
[MaskWave]
Meter=
[Hour3]
Meter=String


@includeWallpaper=#@#Includes\WallpaperCheck.inc
