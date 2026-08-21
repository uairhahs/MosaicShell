[ImageMeter]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#)
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#)
UpdateDivider=1
COntainer=Hour
[ImageMeter2]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#-[Tail:X])
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#)
UpdateDivider=1
COntainer=Tail


@includeWallpaper=#@#Includes\WallpaperCheck.inc
