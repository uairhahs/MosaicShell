﻿[Rainmeter]
MouseScrollUpAction=[!CommandMeasure CScript Move(-1)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
MouseScrollDownAction=[!CommandMeasure CScript Move(1)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]

[ActionTimer]
Custom1=[!UpdateMeasure CurrentDayCalc][!UpdateMeter TodayHL][!Updatemeter BA][!Redraw]


[Variables]
Sec.W=(300*#Scale#)
Sec.H=(300*#Scale#)
;----------------------------------
NumberOfItems=2
NumberOfDividers=0

LinkA0=[ms-settings:dateandtime]
LinkA1=[ms-settings:dateandtime]

;Custom text for Weekday labels. Uses the following format: Sun|Mon|Tue|Wed|Thu|Fri|Sat
DayLabels=Sun|Mon|Tue|Wed|Thu|Fri|Sat

;Set to 1 to hide week 6 if not included in the current month
HideLastWeek=1

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

;Spacers and bumper
;Horizontal space between cells
Space.x=(40*#Scale#)

;Space on left and right of calendar
Bumper.x=(#Sec.W#/2-#Space.x#*6/2)
Bumper.y=(5*#Scale#)

;Cell Dimensions - The dimensions of the individual string meters.
C.w=((#Sec.W#-#Bumper.x#*2-(6*5)*#scale#)/7)
C.h=((#Sec.W#-(5*7)*#Scale#)/7)

;Vertical space between cells
Space.y=(5*#Scale#)

;Space on top of calendar
Bumper.top=(10*#scale#)


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
X=#Space.X#r
Y=r
FontFace=#Font#
FontColor=#Font_Color#
FontSize=((10*#Scale#)*#FontSizeModifier#)
stringAlign=center
AntiAlias=1

[LblTxtStart]
X=(#Bumper.x#)
Y=(#Bumper.top#+#C.h#+#Space.y#+(8*#Scale#))

[LblCurrSty]
FontColor=#Font_Color#

[TextStyle]
Padding=0,(2*#Scale#),0,0
H=#C.h#
W=(#C.w#+(-1*#Scale#))
X=#Space.X#r
Y=r
FontColor=#Font_Color#
FontFace=#Font#
FontSize=((10*#Scale#)*#FontSizeModifier#)
stringAlign=CenterCenter
AntiAlias=1

[FirstDay]
X=(#Bumper.x#)
Y=((#Space.y#/2)+#Bumper.y#)R

[NewWk]
X=(#Bumper.x#)
Y=#Space.y#R

[PreviousMonth]
FontColor=#Font_Color#,55

[NextMonth]
FontColor=#Font_Color#,55

[HolidayStyle]
SolidColor=#Sec_Color#
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

[BA]
Meter=Shape
DynamicVariables=1
Shape2=Line ([mLabel:XW]+10*#scale#),(30*#Scale#),(#Sec.W#-60*#scale#),(30*#Scale#) | Extend Common
Shape3=Line ([mLabel:XW]+10*#scale#),(20*#scale#),([mLabel:XW]+10*#scale#),(40*#scale#) | Extend Common
Common=StrokeWidth (2*#scale#) | Stroke Color #Ter_Color# | StrokeStartCap Round | StrokeEndCap Round
; Shape2=Rectangle 0,0,(#Sec.W#),(80*#Scale#),#Sec.R# | Fill Color #Font_Color#,255 | StrokeWidth #Stroke# | Stroke Color #BoxColor#

[mLabel]
Meter=String
StringAlign=LeftCenter
Text=#MonthLabel#
X=(20*#scale#)
Y=(30*#Scale#)
FontSize=((15*#Scale#)*#FontSizeModifier#)
FOntWeight=650
LeftMouseUpAction=[!CommandMeasure CScript Move()][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
MEterStyle=Sec.String:S
DynamicVariables=1

;Control Bar
[ControlForward]
Meter=String
MeterStyle=TextStyle
StringAlign=RightCenter
Text="❯"
Y=r
X=(#Sec.W#-15*#scale#)
LeftMouseUpAction=[!CommandMeasure CScript Move(1)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
RightMouseUpAction=[!CommandMeasure CScript Move(12)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
MiddleMouseUpAction=[!CommandMeasure CScript Move(12)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
Group=Controls

[ControlBack]
Meter=String
StringAlign=RightCenter
MeterStyle=TextStyle|ControlForward
Text="❮"
X=(-20*#scale#)r
LeftMouseUpAction=[!CommandMeasure CScript Move(-1)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
RightMouseUpAction=[!CommandMeasure CScript Move(-12)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]
MiddleMouseUpAction=[!CommandMeasure CScript Move(-12)][!UpdateMeasure CScript][!UpdateMeter *][!UpdateMeter BA][!Redraw]

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
; [mDay36]
; Meter=string

; [mDay37]
; Meter=string

; [mDay38]
; Meter=string

; [mDay39]
; Meter=string

; [mDay40]
; Meter=string

; [mDay41]
; Meter=string

; [mDay42]
; Meter=string

;Date Indicator
[TodayHL]
Meter=Shape
Shape=Rectangle 0,0,#c.W#,(#c.H#+(2*#Scale#)),(1*#Scale#) | Fill Color #Font_Color#,25 | StrokeWidth 2 | Stroke Color #Font_Color#
X=([&mDay[&CurrentDayCalc]:X]-1)
Y=([&mDay[&CurrentDayCalc]:Y])
DynamicVariables=1
Hidden=#NotCurrentMonth#
