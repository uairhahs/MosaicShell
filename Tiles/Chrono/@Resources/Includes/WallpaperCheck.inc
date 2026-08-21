[WallpaperPath]
Measure=Registry
RegHKey=HKEY_CURRENT_USER
RegKey=Control Panel\Desktop
RegValue=Wallpaper
UpdateDivider=-1
[WallpaperRegex]
Measure=Registry
RegHKey=HKEY_CURRENT_USER
RegKey=Control Panel\Desktop
RegValue=Wallpaper
Substitute="\":"\\",".":"\."
OnUpdateAction=[!UpdateMeasure WallpaperCheck:M]
UpdateDivider=-1
[WallpaperCheck:M]
Measure=String
String=#cache.wallpaper#
ifMatch=[WallpaperRegex]
ifMatchAction=[!Log "WallpaperChecked:match"]
ifnotmatchaction=[!Delay 200][!Log "Wallpaper changed: [WallpaperPath]"]["#@#Actions\nircmd.exe" sendkeypress LWin+D][!Delay 200][!WriteKeyValue Variables Cache.Wallpaper "[WallpaperPath]" "#@#Vars.inc"][!WriteKeyValue #CURRENTCONFIG# AutoSelectScreen "1" "#SETTINGSPATH#Rainmeter.ini"][!Toggle *][!UpdateMeasure MM][!CommandMeasure MM "Reload Image"][!Delay 1000][!Toggle *][!Refresh]
DynamicVariables=1
UpdateDivider=-1


[MM]
Measure=Plugin
Plugin=MagickMeter
DynamicVariables=1
Image=File Screenshot | RenderSize (#SCREENAREAWIDTH#/2),(#SCREENAREAWIDTH#/2) | Modulate #BlurModulate# | Blur #BlurRadius#,#BlurSigma#
ExportTo=#SKINSPATH#..\CoreData\Modularclocks\Blur.png
UpdateDivider=-1
Disabled=1
Group=NUOL
