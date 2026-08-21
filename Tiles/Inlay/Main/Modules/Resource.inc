; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
[Variables]
ResourceHeight=180

[MeasurePhysMemTotal]
Measure=PhysicalMemory
Total=1
[MeasurePhysMemUsed]
Measure=PhysicalMemory
UpdateDivider=1
[MeasureRAM]
Measure=Calc
Formula=(MeasurePhysMemUsed / MeasurePhysMemTotal) * 100
UpdateDivider=1
[MeasureCPU]
Measure=Plugin
Plugin=UsageMonitor
Alias=CPU
Index=0
RegExpSubstitute=1
Substitute="^0$":"0.01"
UpdateDivider=1
[MeasureGPU]
Measure=Plugin
Plugin=UsageMonitor
Alias=GPU
Index=0
Blacklist=dwm
DynamicVariables=1
UpdateDivider=1
; [MeasureCPUHigh]
; Measure=Plugin
; Plugin=UsageMonitor
; Alias=CPU
; Index=1
; UpdateDivider=1
; [MeasureRAMHigh]
; Measure=Plugin
; Plugin=UsageMonitor
; Alias=RAM
; Index=1
; UpdateDivider=1
; [MeasureGPUHigh]
; Measure=Plugin
; Plugin=UsageMonitor
; Alias=GPU
; Index=1
; UpdateDivider=1
; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

;;=====================================
;; bars
;;=====================================

[RAMBox]
MEter=Shape
X=(#P#)
Y=(#P#)r
Shape2=Rectangle 0,0,(#W#-50*#scale#),(35*#Scale#),(#R#*#Scale#) | StrokeWidth 0
Shape3=Rectangle 0,0,(Clamp((#W#-(25*#scale#)*2-1)*[MeasureRAM:]*0.01,0,(#W#-(25*#scale#)*2-1))),(35*#Scale#) | Fill Color #Dark_Color#,(#Alpha#*0.3) | StrokeWidth 0
Shape4=Combine Shape3 | Intersect Shape2
Shape=Rectangle 0,0,(#W#-50*#scale#),(35*#Scale#),(#R#*#Scale#) | StrokeWidth (#Stroke#*2) | Extend This
This=Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.4) | Stroke Color #Font_Color#,(#Alpha#*0.9)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
UpdateDivider=1

[MeterRAMLabel]
Meter=String
Text=RAM
X=(10*#Scale#)r
Y=(35/2*#Scale#)r
StringAlign=leftcenter
MeterStyle=RegularText

; [RAMTOP]
; Meter=String
; X=(#W#/2)
; Y=r
; MeasureName=MeasureRAMHigh
; Text=%1
; DynamicVariables=1
; FontSize=(10*#scale#)
; Container=container
; StringAlign=CenterCenter
; FontColor=#Font_Color#,200
; MeterStyle=RegularText

[RAMPER]
Meter=String
MeasureName=MeasureRAM
Text=%1%
X=(#W#-35*#scale#)
Y=r
FontSize=((10*#scale#)*#FontSizeModifier#)
StringAlign=RightCenter
MeterStyle=RegularText
UpdateDivider=5

[CPUBox]
MEter=Shape
Y=(30*#scale#)r
UpdateDivider=1
Shape3=Rectangle 0,0,(Clamp((#W#-(25*#scale#)*2-1)*[MeasureCPU:]*0.01,0,(#W#-(25*#scale#)*2-1))),(35*#Scale#) | Fill Color #Dark_Color#,(#Alpha#*0.3) | StrokeWidth 0
MeterStyle=RAMBox

[MeterCPULabel]
Meter=String
Text=CPU
MeterStyle=RegularText | MeterRAMLabel

; [CPUTOP]
; Meter=String
; MeasureName=MeasureCPUHigh
; MeterStyle=RegularText | RAMTOP

[CPuPER]
Meter=String
Text=[MeasureCPU:1]%
DynamicVariables=1
MeterStyle=RegularText | RAMPER
UpdateDivider=1

[GPUBox]
MEter=Shape
Y=(30*#scale#)r
UpdateDivider=1
Shape3=Rectangle 0,0,(Clamp((#W#-(25*#scale#)*2-1)*[MeasureGPU:]*0.01,0,(#W#-(25*#scale#)*2-1))),(35*#Scale#) | Fill Color #Dark_Color#,(#Alpha#*0.3) | StrokeWidth 0
MeterStyle=RAMBox

[MeterGPULabel]
Meter=String
Text=GPU
MeterStyle=RegularText | MeterRAMLabel

; [GPUTOP]
; Meter=String
; MeasureName=MeasureGPUHigh
; MeterStyle=RegularText | RAMTOP

[GPUPER]
Meter=String
Text=[MeasureGPU:1]%
DynamicVariables=1
MeterStyle=RegularText | RAMPER
UpdateDivider=1

[ResourceAnchor]
Meter=String
Y=(35/2*#scale#+#P#*#Divider#)r
DynamicVariables=1
