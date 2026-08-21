[Variables]
@includeCacheVarsConfigurator=#@#CacheVars\Configurator.inc
@includeListOfInstalledDLCs=#SKINSPATH#..\CoreData\@DLCs\InstalledDLCs.inc
Sec.W=(350*#Sec.S#)
Sec.H=(350*#Sec.S#)
AniDir=Bottom
InstallText=Inititalizing...
Progress=0
Switch=0
[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[CoreInstallHandler]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Installer.ps1
ExecutionPolicy=Unrestricted
OnUpdateAction=[!CommandMeasure mActions "Execute 1"][!WriteKeyValue "#CURRENTCONFIG#" Active 0 "#SETTINGSPATH#Rainmeter.ini"]

[mActions]
Measure=Plugin
Plugin=ActionTimer
ActionList1=Wait 500 | Kill | Wait 100 | Download
; ActionList2=Wait 500 | NewCache | Wait 100 | CheckDLC
; NewCache=[!CommandMeasure CoreInstallHandler "New-Cache"]
; CheckDLC=[!CommandMeasure CoreInstallHandler "CheckForDLC"]
Kill=[!Setvariable InstallText "Deactivating #Skin.Name#\Main"][!UpdateMeterGroup Progress][!Redraw][!DeactivateConfig "#Skin.Name#\Main"]
Download=[!SetVariable InstallText "Downloading latest package from Github"][!UpdateMeterGroup Progress][!Redraw][!CommandMeasure CoreInstallHandler "Install"]
DynamicVariables=1

; ---------------------------------- Circle ---------------------------------- ;

[s.CircleBarHelper]
Measure=Script
ScriptFile=#@#Lua\CircleBarHelper.lua
BarMeter=SCRIPTED.ProgressBar.Shape
BarShapeIndex=2

[CircleBarCenterX]
Measure=Calc
Formula=(#Sec.W#/2)
[CircleBarCenterY]
Measure=Calc
Formula=((#Sec.H#+[Description:YH])/2)
DynamicVariables=1

[CircleBarRadius]
Measure=Calc
Formula=((#Sec.W#-#Sec.P#*4-[Description:YH])/2)
DynamicVariables=1
OnUpdateAction=[!CommandMeasure s.CircleBarHelper "ImportCircleBar()"][!CommandMeasure s.CircleBarHelper "DrawCircleBar(0)"]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[BC]
Meter = Shape
Shape=Rectangle 0,0,(#Sec.W#),(#Sec.H#),#Sec.WinR# | StrokeWidth 0 | Fill Color #Set.Pri_color#
DynamicVariables=1

[BCI]
Meter=Image
W=(#Sec.W#)
H=(#Sec.H#)
PreserveAspectRatio=2
ImageName=#@#Images\Cards\#Skin.Name#.png
ImageAlpha=25
Container=BC

[HeaderString]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.P#)
W=(#Sec.W#-#Sec.P#*2)
ClipString=1
StringAlign=Center
MeterStyle=Sec.String:S | HeaderString:#Switch#
FontSize=(14*#sec.s#)
FontWeight=650
DynamicVariables=1
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
[HeaderString:0]
Text=Installing #Skin.Name#
[HeaderString:1]
Text=Finishing installation

[Description]
Meter=String
X=r
Y=(#Sec.P#/2)R
W=(#Sec.W#-#Sec.P#*2)
Clipstring=1
StringAlign=Center
Text=#InstallText#
MeterStyle=Sec.String:S
FontSize=(10*#sec.s#)
Group=Progress
DynamicVariables=1


[SCRIPTED.ProgressBar.Shape]
Meter=shape
Line=0,0 | LineTo 0,0
Shape=Ellipse [CircleBarCenterX],[CircleBarCenterY],[CircleBarRadius] | StrokeWidth (2*#Sec.S#) | Stroke Color #Set.Subtext_Color# | Fill Color 0,0,0,0
Shape2=Path Line
CircleBarLine=StrokeWidth (12*#Sec.S#) | Stroke LinearGradient AccentGradient | StrokeStartCap Round | StrokeEndCap Round | Fill Color 0,0,0,0
AccentGradient=45 | #Set.Accent_Color# ; 0 | #Set.Accent_Color_2# ; 1
Group=CircleBar
DynamicVariables=1

[ProgressBar.String]
Meter=String
X=[CircleBarCenterX]
Y=[CircleBarCenterY]
Text=0%
FontWeight=600
StringAlign=CenterCenter
FontSize=(20*#Sec.S#)
Group=Progress
DynamicVariables=1
MeterStyle=Sec.String:S


; [ProgressBar]
; Meter=Shape
; X=(#Sec.P#)
; Y=(#Sec.P#)R
; Shape=Line 0,0,((#Sec.W#-#sec.P#*2)),0 | StrokeWidth 2 | Stroke Color #set.Subtext_Color# | StrokeStartCap Round | StrokeEndcap Round
; Shape2=Line 0,0,((#Sec.W#-#sec.P#*2)*#Progress#/100),0 | StrokeWidth 4 | Stroke LinearGradient This | StrokeStartCap Round | StrokeEndcap Round
; This=180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0
; DynamicVariables=1
; Group=Progress
; MeterStyle=ProgressBar:#Switch#
; [ProgressBar:0]
; Hidden=0
; [ProgressBar:1]
; Hidden=1
