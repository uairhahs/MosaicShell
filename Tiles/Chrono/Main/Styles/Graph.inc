[Rainmeter]
Update=#Rm.Update#
SkinWidth=#SW#
Skinheight=#SH#
; SolidColor=255,255,255,25
; BackgroundMode=2
DynamicWindowSize=1

[Variables]
Spacing=(160*#Scale#)
SH=(500*#Scale#)
SW=(500*#Scale#)
StartEnd=(50*#Scale#)


[mHour]
Measure=Time
Format=%#gVar.2412#
AverageSize=12
[mMinA]
Measure=Time
Format=%M
AverageSize=30
MaxValue=60
[mSecA]
Measure=Time
Format=%S
AverageSize=30
IfCondition=msecA
MaxValue=60
[mMin]
Measure=Calc
Formula=(mMinA%60)
MaxValue=60
UpdateDivider=1
[mSec]
Measure=Calc
Formula=(mSecA%60)
MaxValue=60
UpdateDivider=1

[mHourClock]
Measure=Time
Format=%I
UpdateDivider=1
[mMinClock]
Measure=Time
Format=%M
UpdateDivider=1

[Style1]
FontColor=#OppositeColor#
Text="%1"
X=#Spacing#r
Y=r
FontSize=(70*#scale#)
StringAlign=CenterCenter
Group=Text

[Graph]
Meter=shape
DynamicVariables=1
X=(#SW#/2)
Y=(#SH#/2)
Shape=Line 0,0,(175*#Scale#*(cos(rad(360*[mHourClock]/12-90)))),(150*#Scale#*(sin(rad(360*[mHourClock]/12-90)))) | Extend Common | StrokeWidth (24*#scale#)
Shape2=Line 0,0,(125*#Scale#*(cos(rad(360*[mMinClock]/60-90)))),(125*#Scale#*(sin(rad(360*[mMinClock]/60-90)))) | Extend Common | StrokeWidth (16*#scale#)
Shape3=Line 0,0,(75*#Scale#*(cos(rad(360*[mSec]/60-90)))),(75*#Scale#*(sin(rad(360*[mSec]/60-90)))) | Extend Common | StrokeWidth (12*#scale#)
Common=StrokeStartCap Round | StrokeEndCap Round | Stroke Color #LineColor#,#LineOpacity#



[Hour]
Meter=String
MeasureName=mHour
X=(90*#Scale#)
Y=(#SH#/2)
MeterStyle=String:S | style1 | Shadow#Shadow#
[Minute]
Meter=String
MeasureName=mMin
MeterStyle=String:S | style1 | Shadow#Shadow#
[seconds]
Meter=String
MeasureName=mSec
MeterStyle=String:S | style1 | Shadow#Shadow#

[Style1]
FontColor=#FontColor#,#FontOpacity#
[Shadow1]
InlineSetting=Shadow | 1 | 1 | 3 | #ShadowColor#,75
