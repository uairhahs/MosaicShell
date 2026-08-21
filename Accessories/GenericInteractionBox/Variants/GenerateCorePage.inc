[Variables]
LastSelectedFile=C:\Users\enhan\Desktop\RmPackage\Test.ini
LastOutputSkin=Mixdeck
LastOutputPage=General
Editing=

Sec.W=(300*#Sec.S#)
Sec.H=(500*#Sec.S#)
AniDir=Right
[moveX]
Formula=(#SCREENAREAWIDTH#-20)
[moveY]
Formula=(#SCREENAREAHEIGHT#-20)
[anchorX]
Formula=100
[anchorY]
Formula=100

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[CoreFactory]
Measure=Script
ScriptFile=#@#Lua\CoreFactory.lua

[Choose:M]
Measure=Plugin
Plugin=FileChoose
ReturnValue=WholePath
FileInitialDirectory=#@#
DynamicVariables=1
Command1=[!CommandMeasure CoreFactory "appendFile('LastSelectedFile', [[$Path$]])"]

[Input:M]
Measure=Plugin
Plugin=InputText
SolidColor=#Set.Pri_Color#
FontColor=#Set.Text_Color#
FontFace=#Set.Font#
FontSize=(10*#Sec.S#)
StringAlign=Left
W=(#SEc.W#-#Sec.P#*2)
H=([LastOutputSkin:H])
DynamicVariables=1
DefaultValue="[##Editing#]"
Command1=[!CommandMeasure CoreFactory "appendFile('#editing#', [[$UserInput$]])"]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header]
Meter=String
X=(#SEc.W#/2)
Y=(#Sec.P#)
StringAlign=Center
Text=CoreFactory.lua
FontSize=(15*#Sec.S#)
Fontweight=650
MEterStyle=Sec.String:S

[DropBox]
Meter=Shape
X=#Sec.P#
Y=(#Sec.P#)R
Shape=Rectangle 0,0,(#SEc.W#-#Sec.P#*2),(200*#Sec.S#),(#Sec.R#/2) | StrokeWidth 2 | Fill Color 0,0,0,1 | Stroke Color #Set.Ter_Color#
LeftMouseUpAction=[!CommandMeasure Choose:M "ChooseFile 1"]

[SelectedFile]
Meter=String
X=(#SEc.W#/2)
Y=([DropBox:YH]-100*#Sec.S#)
W=([DropBox:W])
ClipString=2
FontColor=#Set.SubText_Color#
DynamicVariables=1
StringAlign=CenterCenter
Text=#LastSelectedFile#
MEterStyle=Sec.String:S

[LastOutputSkin]
Meter=String
X=#Sec.P#
Y=(#Sec.P#+#Sec.S#*100)r
Text=[##CURRENTSECTION#]
DynamicVariables=1
LeftMouseUpAction=[!SetOption Input:M X "([#CURRENTSECTION#:X])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y])"][!SetVariable Editing "#CURRENTSECTION#"][!CommandMeasure Input:M "ExecuteBatch 1"]
MEterStyle=Sec.String:S

[LastOutputPage]
Meter=String
X=#Sec.P#
Y=R
Text=[##CURRENTSECTION#]
DynamicVariables=1
LeftMouseUpAction=[!SetOption Input:M X "([#CURRENTSECTION#:X])"][!SetOption Input:M Y "([#CURRENTSECTION#:Y])"][!SetVariable Editing "#CURRENTSECTION#"][!CommandMeasure Input:M "ExecuteBatch 1"]
MEterStyle=Sec.String:S

[Button:Shape]
Meter=Shape
X=(#Sec.W#/2)
Y=(#Sec.H#-#Sec.P#)
Shape=Rectangle 0,0,(150*#Sec.S#),(40*#Sec.S#),(#Sec.R#/2) | STrokeWidth 0 | OffSet (-75*#Sec.S#),(-40*#Sec.S#) | Extend Fill
Fill=Fill Color #Set.Positive_Color#,150
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_Color#,230"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure CoreFactory "GeneratePageContent()"]

[StartGenerate]
Meter=String
X=r
Y=(-20*#Sec.S#)r
StringAlign=CenterCenter
Text=Start
MEterStyle=Sec.String:S
