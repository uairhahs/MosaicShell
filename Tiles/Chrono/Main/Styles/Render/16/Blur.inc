[ImageMeter]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#-[Time:X]-[Time:W]/2)
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#-[Time:Y]-[Time:H]/2)
UpdateDivider=1
COntainer=Time
[ImageMeter2]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#-[Date:X]-[Date:W]/2)
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#-[Date:Y])
UpdateDivider=1
COntainer=Date


@includeWallpaper=#@#Includes\WallpaperCheck.inc


[MM]
Image=File Screenshot | RenderSize (#SCREENAREAWIDTH#/4),(#SCREENAREAWIDTH#/4) | Modulate #BlurModulate# | Blur #BlurRadius#,#BlurSigma# | MedianFilter 10
