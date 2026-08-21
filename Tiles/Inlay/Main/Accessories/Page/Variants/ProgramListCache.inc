
[1]
Meter=Shape
MeterStyle=Index:Style
[Index1Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index1Name]
Meter=String
MeterStyle=RegularText | Name:Style

[2]
Meter=Shape
MeterStyle=Index:Style
[Index2Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index2Name]
Meter=String
MeterStyle=RegularText | Name:Style

[3]
Meter=Shape
MeterStyle=Index:Style
[Index3Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index3Name]
Meter=String
MeterStyle=RegularText | Name:Style

[4]
Meter=Shape
MeterStyle=Index:Style
[Index4Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index4Name]
Meter=String
MeterStyle=RegularText | Name:Style

[5]
Meter=Shape
MeterStyle=Index:Style
[Index5Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index5Name]
Meter=String
MeterStyle=RegularText | Name:Style

[6]
Meter=Shape
MeterStyle=Index:Style
[Index6Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index6Name]
Meter=String
MeterStyle=RegularText | Name:Style

[7]
Meter=Shape
MeterStyle=Index:Style
[Index7Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index7Name]
Meter=String
MeterStyle=RegularText | Name:Style

[8]
Meter=Shape
MeterStyle=Index:Style
[Index8Icon]
Meter=Image
MeterStyle=Icon:Style | Icon1:Style
[Index8Name]
Meter=String
MeterStyle=RegularText | Name:Style
[mPath]
Measure=Plugin
Plugin=FileView
Path="C:\ProgramData\Microsoft\Windows\Start Menu\Programs"
Count=8
ShowFolder=0
ShowDotDot=0
HideExtensions=1
FinishAction=[!ShowMeterGroup List][!UpdateMeasureGroup Children][!UpdateMeter *][!Redraw]
[mFileCount]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileCount
Group=Children

[mIndex1Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=1
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex2Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=2
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex3Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=3
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex4Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=4
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex5Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=5
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex6Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=6
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex7Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=7
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex8Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=8
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

[mIndex1Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=1
Group=Children

[mIndex2Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=2
Group=Children

[mIndex3Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=3
Group=Children

[mIndex4Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=4
Group=Children

[mIndex5Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=5
Group=Children

[mIndex6Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=6
Group=Children

[mIndex7Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=7
Group=Children

[mIndex8Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=8
Group=Children

