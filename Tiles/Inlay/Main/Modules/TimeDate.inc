; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
TimeDateHeight=(64+#Pad#*(1+#Divider#))

[MeasureTime]
Measure=Time
Format=%#gVar.2412#:%M
UpdateDivider=1
Disabled=#CustomFormat#
[MeasureSec]
Measure=Time
Format=:%S
UpdateDivider=1
Disabled=#CustomFormat#
[MeasureDate]
Measure=Time
Format=%F

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[TimeDate.Background]
Meter=Shape
X=#P#
Y=#P#r
Shape=Rectangle 0,0,0,(64*#Scale#) | StrokeWidth 0 | Fill Color 0,0,0,255
Shape2=Line 0,(32*#scale#),((#W#-#P#*4)/2-[Timedate.String:W]/2),(32*#scale#) | Extend Common
Shape3=Line ((#W#-#P#*0)/2+[Timedate.String:W]/2),(32*#scale#),(#W#-#P#*2),(32*#scale#) | Extend Common
Shape4=Line ((#W#-#P#*0)/2+[Timedate.String:W]/2),(#P#),((#W#-#P#*0)/2+[Timedate.String:W]/2),(64*#scale#-#P#) | Extend Common
Shape5=Line ((#W#-#P#*4)/2-[Timedate.String:W]/2),(#P#),((#W#-#P#*4)/2-[Timedate.String:W]/2),(64*#scale#-#P#) | Extend Common
Common=StrokeWidth (2*#scale#) | Stroke Color #Ter_Color# | StrokeStartCap Round | StrokeEndCap Round
DynamicVariables=1

[TimeDate.String]
Meter=String
X=(#W#/2)
Y=(32*#scale#)r
Text=%1#CRLF#%2
MeasureName=MeasureTime
MeasureName2=MeasureDate
FontSize=((#scale#*12)*#FontSizeModifier#)
StringAlign=CenterCenter
MeterStyle=RegularText
FontWeight=650
InlinePattern=(.*)#CRLF#
InlineSetting=Size | (40*#Scale#)
InlinePattern2=#CRLF#.*
InlineSetting2=Color | #Dark_Color#
InlinePattern3=#CRLF#.*
InlineSetting3=Weight | 400
UpdateDivider=1

; [Date]
; Meter=String
; X=(#W#-25*#Scale#)
; Y=r
; StringAlign=Right
; FontSize=(#scale#*15)
; FontWeight=500
; MeasureName=MeasureTime
; MeasureName2=MeasureSec
; MeasureName3=MeasureAP
; MeasureName4=MeasureCustomFormat
; MeterStyle=RegularText | Date:#CustomFormat#:#gVar.2412#:#ShowSec#
; InlinePattern=A|P|M
; InlineSetting=Size | (#Scale#*10)
; FontColor=#Dark_Color#
; UpdateDivider=1
; LeftMouseUpAction=["#@#Actions\Nircmd.exe" sendkeypress LWin+Alt+D]
; [Date:0:H:0]
; Text=%1
; [Date:0:I:0]
; Text=%1  %3
; [Date:0:H:1]
; Text=%1%2
; [Date:0:I:1]
; Text=%1%2  %3
; [Date:1:H:0]
; Text=%4
; [Date:1:I:0]
; Text=%4
; [Date:1:H:1]
; Text=%4
; [Date:1:I:1]
; Text=%4

[TimeDateAnchor]
Meter=String
Y=([TimeDate.Background:YH]+#P#*#Divider#)
DynamicVariables=1
