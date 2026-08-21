; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
MonHeight=160
Monitor=3
MonW=((#W#-(50+3*20)*#Scale#)/4)

[SettingImport3]
Measure=String
String=#Monitor#
DynamicVariables=1
IfMatch=1
IfMatchAction=[!SetOption MonBox1 Monitor "Fill Color #Accent_Color#"][!SetOption MonBox2 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox3 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox4 Monitor "Fill Color 0,0,0,0"]
IfMatch2=2
IfMatchAction2=[!SetOption MonBox2 Monitor "Fill Color #Accent_Color#"][!SetOption MonBox1 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox3 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox4 Monitor "Fill Color 0,0,0,0"]
IfMatch3=3
IfMatchAction3=[!SetOption MonBox3 Monitor "Fill Color #Accent_Color#"][!SetOption MonBox2 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox1 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox4 Monitor "Fill Color 0,0,0,0"]
IfMatch4=4
IfMatchAction4=[!SetOption MonBox4 Monitor "Fill Color #Accent_Color#"][!SetOption MonBox2 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox3 Monitor "Fill Color 0,0,0,0"][!SetOption MonBox1 Monitor "Fill Color 0,0,0,0"]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[MonStyle]
X=((#MonW#/2-(Clamp(#MonW#-40*#Scale#,0,64*#scale#))/2)+20*#Scale#)R
Y=(-55*#scale#/2+(Clamp(#MonW#-40*#Scale#,0,64*#scale#))/2)r
Shape2=Rectangle (8*#Scale#),(7*#Scale#),(4*#Scale#),(55*#scale#-7*2*#Scale#),(2*#Scale#) | StrokeWidth 0 | Extend Monitor
Shape=Rectangle 0,0,#MonW#,(55*#scale#),(#R#) | StrokeWidth (#Stroke#*2) | StrokeWidth (#Stroke#*2) | Extend This
This=Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.4) | Stroke Color #Font_Color#,(#Alpha#*0.9)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Monitor=Fill Color 0,0,0,0
Group=Monboxes
[MonIconStyle]
X=(#MonW#/2-(Clamp(#MonW#-40*#Scale#,0,64*#scale#))/2)r
Y=(55*#scale#/2-(Clamp(#MonW#-40*#Scale#,0,64*#scale#))/2)r
W=(Clamp(#MonW#-40*#Scale#,0,64*#scale#))
[MonTextStyle]
X=(20*#Scale#+#MonW#)r
Y=r
StringAlign=Center
FontColor=#Dark_Color#
FontSize=((10*#Scale#)*#FontSizeModifier#)

[MonSubtitle]
Meter=String
X=(25*#Scale#)
Y=(25*#Scale#)r
MeterStyle=RegularText
Text=Monitor Configuration
FontColor=#Dark_Color#,200
[MonBox1]
Meter=Shape
x=r
y=(10*#Scale#)R
LeftMouseUpAction=["DisplaySwitch.exe" "/internal"][!SetVariable Monitor 1][!WriteKeyValue Variables Monitor 1 "#ROOTCONFIGPATH#Main\Modules\Mon.inc"][!UpdateMeasure SettingImport4][!UpdateMeterGroup MonBoxes][!Redraw]
meterstyle=monstyle
[MonIcon1]
Meter=Image
ImageTint=#Font_Color#
ImageName=#@#Icons\Single.png
MeterStyle=MonIconStyle
[MonBox2]
Meter=Shape
meterstyle=monstyle
LeftMouseUpAction=["DisplaySwitch.exe" "/double"][!SetVariable Monitor 2][!WriteKeyValue Variables Monitor 2 "#ROOTCONFIGPATH#Main\Modules\Mon.inc"][!UpdateMeasure SettingImport4][!UpdateMeterGroup MonBoxes][!Redraw]
[MonIcon2]
Meter=Image
ImageTint=#Font_Color#
ImageName=#@#Icons\Duplicate.png
MeterStyle=MonIconStyle
[MonBox3]
Meter=Shape
LeftMouseUpAction=["DisplaySwitch.exe" "/extend"][!SetVariable Monitor 3][!WriteKeyValue Variables Monitor 3 "#ROOTCONFIGPATH#Main\Modules\Mon.inc"][!UpdateMeasure SettingImport4][!UpdateMeterGroup MonBoxes][!Redraw]
meterstyle=monstyle
[MonIcon3]
Meter=Image
ImageTint=#Font_Color#
ImageName=#@#Icons\Extend.png
MeterStyle=MonIconStyle
[MonBox4]
Meter=Shape
LeftMouseUpAction=["DisplaySwitch.exe" "/external"][!SetVariable Monitor 4][!WriteKeyValue Variables Monitor 4 "#ROOTCONFIGPATH#Main\Modules\Mon.inc"][!UpdateMeasure SettingImport4][!UpdateMeterGroup MonBoxes][!Redraw]
meterstyle=monstyle
[MonIcon4]
Meter=Image
ImageTint=#Font_Color#
ImageName=#@#Icons\Second.png
MeterStyle=MonIconStyle
[MonText1]
Meter=String
X=(25*#Scale#+#MonW#/2)
Y=(65*#Scale#)r
Text=Single
MeterStyle=RegularText | MonTextStyle
[MonText2]
Meter=String
Text=Duplicate
MeterStyle=RegularText | MonTextStyle
[MonText3]
Meter=String
Text=Extend
MeterStyle=RegularText | MonTextStyle
[MonText4]
Meter=String
Text=Second
MeterStyle=RegularText | MonTextStyle

[MonAnchor]
Meter=String
Y=(45*#Scale#)r
DynamicVariables=1
