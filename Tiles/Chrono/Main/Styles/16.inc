[Rainmeter]
; SolidColor=255,255,255,25
; BackgroundMode=2
SkinWidth=#SW#
SkinHeight=#SH#

[Variables]
; @includeWeather=#@#Includes\GlobalWeather.inc
SW=(500*#Scale#)
SH=(500*#Scale#)

[mTime]
Measure=Time
Format=%##gVar.2412#:%M
RegexpSubstitute=1
[mDate]
Measure=Time
Format=%A, %d %B
FormatLocale=#FormatLocale#
[String:S]
FontColor=#TintColor#

[Time:S]
MeasureName=mTime
FontSize=(100*#scale#)
FontWeight=600
X=(#SW#/2)
Y=(#SW#/2)
StringAlign=CenterCenter
[TimeUnderlay]
Meter=String
FontColor=255,255,255,#ShadowStrength#
InlineSetting=Shadow | 0 | 0 | #Style_ShadowSigma# | 0,0,0
MeterStyle=String:S | Time:S
[Time]
Meter=String
MeterStyle=String:S | Time:S
[TimeOverlay]
Meter=String
FontColor=255,255,255,#Whitening#
MeterStyle=String:S | Time:S

[Date:S]
X=r
Y=r
MeasureName=mDate
DynamicVariables=1
StringAlign=CenterBottom
FontSize=(28*#scale#)
FontWeight=400
[DateUnderlay]
Meter=String
X=(#SW#/2)
Y=(10*#Scale#-[TimeOverlay:H]/2)r
FontColor=255,255,255,(#ShadowStrength#+50)
InlineSetting=Shadow | 0 | 0 | #Style_ShadowSigma# | 0,0,0
MeterStyle=String:S | Date:S
[Date]
Meter=String
MeterStyle=String:S | Date:S
[DateOverlay]
Meter=String
FontColor=255,255,255,#Whitening2#
MeterStyle=String:S | Date:S


@includeRender=Render\16\#Style_Render#.inc
