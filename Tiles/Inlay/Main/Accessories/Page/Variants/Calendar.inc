﻿[ActionTimer]
Custom0=[!UpdateMeasure CurrentDayCalc][!UpdateMeter TodayHL][!Redraw]


[Variables]
;----------------------------------
NumberOfItems=2
NumberOfDividers=0

LinkA0=[ms-settings:dateandtime]
LinkA1=[ms-settings:dateandtime]

;Custom text for Weekday labels. Uses the following format: Sun|Mon|Tue|Wed|Thu|Fri|Sat
DayLabels=Sun|Mon|Tue|Wed|Thu|Fri|Sat

;Set to 1 to hide week 6 if not included in the current month
HideLastWeek=0

;Holidays File names
;Note: For Holiday file names, multiple sources can be used by uisng the pipe delimiter (|).
EventFile=#SKINSPATH#Inlay\@Resources\Calendars\Holidays.xml

;Custom Text formatting for Month Labels. See Readme for details.
LabelText=

;Set to 1 to add a leading zero to any number below 10.
LeadingZeroes=0

;Custom text for Month labels. Uses the following format: Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec
MonthLabels=Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec

;Set to 1 to have the week start on Monday
StartOnMonday=0

;The format for the list of upcoming events. {$day} and {$desc} are valid variables.
NextFormat=

;Show the Moon Phases as events
ShowMoonPhases=0

;Color to use for the Moon Phases Events
MoonColor=

;Set to 0 to hide all calendar events including the Moon Phases
ShowEvents=1

;Set to 1 to disable scroll actions for changing the month
DisableScroll=0
;----------------------------------

; ---------------------------------------------------------------------------- ;
;                                    MYVARS                                    ;
; ---------------------------------------------------------------------------- ;
; W=((275*#Scale#)-((275*#Scale#)%1))
; H=((29*#scale#)-((29*#scale#)%1))

;Cell Dimensions - The dimensions of the individual string meters.
C.w=((#W#-(44+5*6)*#Scale#)/7)
C.h=((#W#-(44+5*6)*#Scale#)/7)

;Spacers and bumper
;Horizontal space between cells
Space.x=(5*#Scale#)

;Vertical space between cells
Space.y=(5*#Scale#)

;Space on left and right of calendar
Bumper.x=(22*#Scale#)
Bumper.y=(22*#Scale#)

;Space on top of calendar
Bumper.top=(22*#scale#)


;----------------------------------

[MeasureTime]
Measure=Time
Format=%#H:%M:%S %p
FormatLocale=EN-US

[AnalyzeTotal]
Measure=Calc
Formula=1

[CurrentDayCalc]
Measure=Calc
Formula=(((#ThisWeek#-1)*7)+(#Week#+1))
DynamicVariables=1
UpdateDivider=-1

;-----------------------------

[LblTxtSty]
H=#C.h#
W=(#C.w#+(-1*#Scale#))
X=#Space.X#R
Y=r
FontFace=#Font#
FontColor=#Pri_color#
FontSize=((10*#Scale#)*#FontSizeModifier#)
stringAlign=center
AntiAlias=1

[LblTxtStart]
X=(#Bumper.x#+#C.w#/2)
Y=(#Bumper.top#+#C.h#+#Space.y#+(8*#Scale#))

[LblCurrSty]
FontColor=#Pri_color#

[TextStyle]
Padding=0,(2*#Scale#),0,0
H=#C.h#
W=(#C.w#+(-1*#Scale#))
X=#Space.X#R
Y=r
FontColor=#Font_Color#
FontFace=#Font#
FontSize=((10*#Scale#)*#FontSizeModifier#)
stringAlign=CenterCenter
AntiAlias=1

[FirstDay]
X=(#Bumper.x#+#C.w#/2)
Y=((#Space.y#/2)+#Bumper.y#)R

[NewWk]
X=(#Bumper.x#+#C.w#/2)
Y=#Space.y#R

[PreviousMonth]
FontColor=#Font_Color#,55

[NextMonth]
FontColor=#Font_Color#,55

[HolidayStyle]
SolidColor=#Font_Color#,15
SolidColor2=#Font_Color#,125
GradientAngle=45
InlineSetting=Weight | 800
InlineSetting2=Color | #Font_Color#,255
InlineSetting3=Shadow | 0 | (1*#Scale#) | 1 | #Pri_color#,185



; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;
[WinIcon]
ImageName=#@#Images\Orbs\Calendar.png
[Start]
Text=Calendar

[TimeBox]
Meter=Shape
Shape=Rectangle 0,0,(#W#+2-(0*2)),((80*#Scale#)-(0*2)),(#WinR#*#scale#) | Fill Color #Font_Color#,255 | StrokeWidth #Stroke# | Stroke Color #BoxColor#
Shape2=Rectangle 0,((65*#Scale#)+0),(#W#+2-(0*2)),((15*#Scale#)-(0*2)),0
Shape3=Rectangle 0,(0+1),(#W#+2-(0*2)),#H#,#WinR# | Fill Color 0,0,0,1 | StrokeWidth 0
Shape4=Combine Shape | Union Shape2
Y=((3*#Scale#))
H=((60*#Scale#)-(0*2))
MouseOverAction=[!SetOptionGroup Controls Font_Color "#*Color*#,255"][!UpdateMeterGroup Controls][!Redraw]
MouseLeaveAction=[!SetOptionGroup Controls Font_Color "#*Color*#,128"][!UpdateMeterGroup Controls][!Redraw]
MouseScrollUpAction=[!CommandMeasure CScript Move(-1)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
MouseScrollDownAction=[!CommandMeasure CScript Move(1)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
UpdateDivider=-1

[mTime]
Meter=String
MeasureName=MeasureTime
StringAlign=Center
W=(#W#/1.4)
X=(#W#/2)
Y=(8*#Scale#)r
H=#C.h#
FontColor=#Pri_color#,155
FontFace=#Font#
FontWeight=400
FontSize=((11*#Scale#)*#FontSizeModifier#)
LeftMouseUpAction=[!CommandMeasure CScript Move()][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
AntiAlias=1
DynamicVariables=1

[mLabel]
Meter=String
StringAlign=Center
Text=#MonthLabel#
W=(#W#/1.4)
X=(#W#/2)
Y=(#Bumper.top#+(8*#Scale#))
H=#C.h#
FontColor=#Pri_color#,255
FontFace=#Font#
FontWeight=400
FontSize=((15*#Scale#)*#FontSizeModifier#)
LeftMouseUpAction=[!CommandMeasure CScript Move()][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
AntiAlias=1
DynamicVariables=1

;Control Bar
[ControlForward]
Meter=String
MeterStyle=TextStyle
StringAlign=CenterCenter
FontColor=#Pri_color#,128
Text="❯"
Y=(#Bumper.top#+(20*#Scale#))
X=(#W#-#C.w#+(1*#Scale#))
LeftMouseUpAction=[!CommandMeasure CScript Move(1)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
RightMouseUpAction=[!CommandMeasure CScript Move(12)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
MiddleMouseUpAction=[!CommandMeasure CScript Move(12)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
Group=Controls

[ControlBack]
Meter=String
StringAlign=CenterCenter
MeterStyle=TextStyle|ControlForward
Text="❮"
X=#C.w#
LeftMouseUpAction=[!CommandMeasure CScript Move(-1)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
RightMouseUpAction=[!CommandMeasure CScript Move(-12)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]
MiddleMouseUpAction=[!CommandMeasure CScript Move(-12)][!UpdateMeasure CScript][!UpdateMeter *][!Redraw]

[CScript]
Measure=Script
ScriptFile=#SKINSPATH#Inlay\@Resources\Lua\CScript.lua

;Day Labels
[l0]
Meter=string

[l1]
Meter=string

[l2]
Meter=string

[l3]
Meter=string

[l4]
Meter=string

[l5]
Meter=string

[l6]
Meter=string

;Week 1 Begin
[mDay1]
Meter=string

[mDay2]
Meter=string

[mDay3]
Meter=string

[mDay4]
Meter=string

[mDay5]
Meter=string

[mDay6]
Meter=string

[mDay7]
Meter=string

;Week 2 Begin
[mDay8]
Meter=string

[mDay9]
Meter=string

[mDay10]
Meter=string

[mDay11]
Meter=string

[mDay12]
Meter=string

[mDay13]
Meter=string

[mDay14]
Meter=string

;Week 3 Begin
[mDay15]
Meter=string

[mDay16]
Meter=string

[mDay17]
Meter=string

[mDay18]
Meter=string

[mDay19]
Meter=string

[mDay20]
Meter=string

[mDay21]
Meter=string

;Week 4 Begin
[mDay22]
Meter=string

[mDay23]
Meter=string

[mDay24]
Meter=string

[mDay25]
Meter=string

[mDay26]
Meter=string

[mDay27]
Meter=string

[mDay28]
Meter=string

;Week 5 Begin
[mDay29]
Meter=string

[mDay30]
Meter=string

[mDay31]
Meter=string

[mDay32]
Meter=string

[mDay33]
Meter=string

[mDay34]
Meter=string

[mDay35]
Meter=string

;Week 6 Begin
[mDay36]
Meter=string

[mDay37]
Meter=string

[mDay38]
Meter=string

[mDay39]
Meter=string

[mDay40]
Meter=string

[mDay41]
Meter=string

[mDay42]
Meter=string

;Date Indicator
[TodayHL]
Meter=Shape
Shape=Rectangle 0,0,#c.W#,(#c.H#+(2*#Scale#)),(1*#Scale#) | Fill Color #Font_Color#,25 | StrokeWidth 2 | Stroke Color #Font_Color#
X=([&mDay[&CurrentDayCalc]:X]-1)
Y=([&mDay[&CurrentDayCalc]:Y])
DynamicVariables=1
Hidden=#NotCurrentMonth#
