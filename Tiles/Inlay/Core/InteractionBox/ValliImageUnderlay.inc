﻿[Rainmeter]
SkinWidth=#Sec.W#

[Variables]
Sec.W=(300*#Sec.S#)
Sec.H=(300*#Sec.S#)
AniDir=Bottom
@includeSecVars=#@#SecVar.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
@includeSkinvars=#SKINSPATH##Skin.Name#\@Resources\Vars.inc

[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Choose:M]
Measure=Plugin
Plugin=FileChoose
UseNewStyle=0
GetTarget=0
AllowNonstorageFolder=1
CopyLink=1
GetIcon=0
ReturnValue=WholePath
DynamicVariables=1
Command1=[!WriteKeyValue variables ImageUnderlayPath """$Path$""" #Sec.SaveLocation#][!UpdateMEasure Auto_Refresh:M "#MosaicShell\Main"][!Refresh "#MosaicShell\Main"][!Refresh]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[BC]
Meter = Shape
Shape=Rectangle 0,0,(#Sec.W#),(#Sec.H#),#Set.WinR# | StrokeWidth 0 | Fill Color #Set.Pri_color#,#Set.Alpha#
DynamicVariables=1
[UserImage.Image]
Meter=Image
W=(#Sec.W#)
H=(#Sec.H#)
PreserveAspectRatio=2
ImageName=#ImageUnderlayPath#
ImageAlpha=#ImageUnderlayAlpha#
Container=BC

[ImageUnderlayPath.String]
Meter=String
Text=#ImageUnderlayPath#
W=(#Sec.W#-#Sec.P#*2)
ClipString=2
X=(#Sec.W#/2)
Y=(#Sec.H#/2)
InlineSetting=Shadow | 0 | 0 | 2 | #Set.Pri_color#
StringAlign=CenterCenter
FontColor=#Set.Subtext_Color#
MeterStyle=Sec.String:S
LeftMouseUpAction=[!CommandMeasure Choose:M "ChooseFile 1"]
