[ImageMeter]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#-250*#Scale#)
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#-170*#Scale#)
UpdateDivider=1
COntainer=Hour
[ImageMeter2]
Meter=Image
ImageName=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
DynamicVariables=1
W=#SCREENAREAWIDTH#
H=#SCREENAREAHEIGHT#
X=(-#CURRENTCONFIGX#+#SCREENAREAX#-250*#Scale#)
Y=(-#CURRENTCONFIGY#+#SCREENAREAY#-380*#Scale#)
UpdateDivider=1
COntainer=Min


[HourShadow]
Meter=String
MeterStyle=String:S | Hour
FontColor=255,255,255,25
InlineSetting=Shadow | 0 | 0 | 5 | 0,0,0
[Hour]
Meter=String

[MinShadow]
Meter=String
MeterStyle=String:S | Min
FontColor=255,255,255,25
InlineSetting=Shadow | 0 | 0 | 5 | 0,0,0
[Min]
Meter=String


@includeWallpaper=#@#Includes\WallpaperCheck.inc
