
;================================================================================================================================================================================

[Behavior]
Meter=String
X=(30*#Set.S#)
Y=(30*#Set.S#)R
FontSize=(14*#Set.S#)
FontWeight=500
Text=Behavior
MeterStyle=Set.String:S
Container=ContentContainer

[Option01]
Meter=String
X=r
Y=(15*#Set.S#)R
FontSize=(10*#Set.S#)
Text=Auto hide music player when not playing
MeterStyle=Set.String:S
Container=ContentContainer


;================================================================================================================================================================================

[AutoHide]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option01:Y]-#Set.P#+(-20/2+8)*#Set.S#)
