
[AppVol1]
Measure=Plugin
Plugin=AppVolume
Parent=AppVolumeParent
Index=1
Substitute=#ReplaceApp#
Group=UpdateWhenChange

[AppVolPeak1]
Measure=Plugin
Plugin=AppVolume
Parent=AppVolumeParent
Index=1
NumberType=Peak
UpdateDivider=1
Disabled=(#VolumePeaks#-1)

[AppVolPer1]
Measure=Calc
Formula=Round(AppVol1 * 100)
Group=UpdateWhenChange
Substitute="-100":"Muted"

[ppVol1]
Meter=Shape
MeterStyle=Item.Background.Shape:S

[pVol1]
Meter=Image
MeterStyle=Item.Image:S

[Vol1]
Meter=String
MeterStyle=String:S | Item.Name.String:S

[1]
Meter=Shape
MeterStyle=Item.Shape:S

[VolPer1]
Meter=String
MeterStyle=String:S | Item.Vol.String:S

