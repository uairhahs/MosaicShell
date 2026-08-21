[Rainmeter]
SkinWidth=#Sec.W#

[Variables]
Sec.W=(300*#Sec.S#)
Sec.H=(300*#Sec.S#)
AniDir=Bottom
@includeSecVars=#@#CacheVars\Configurator.inc
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
Command1=[!WriteKeyValue variables AvatarPath """$Path$""" #Sec.SaveLocation#][!UpdateMEasure Auto_Refresh:M "#MosaicShell\Main"][!Refresh "#MosaicShell\Main"][!Refresh]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[UserImage.Shape]
Meter=Shape
X=(#Sec.W#/2-64*#Scale#/2)
Y=(#Sec.H#/2-64*#Scale#/2)
Shape=Rectangle 0,0,64,64,32 | StrokeWidth 2 | Stroke Color #BoxColor# | Scale #Scale#,#Scale#,0,0
[UserImage.Container]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,64,64,32 | StrokeWidth 0 | Scale #Scale#,#Scale#,0,0
[UserImage.Image]
Meter=Image
X=r
Y=r
W=(64*#scale#)
H=(64*#scale#)
ImageName=#AvatarPath#
Container=userImage.Container
LeftMouseUpAction=[!CommandMeasure Choose:M "ChooseFile 1"]

[AvatarPath.String]
Meter=String
Text=#AvatarPath#
W=(#Sec.W#-#Sec.P#*2)
ClipString=2
X=(32*#scale#)r
Y=(#Sec.P#)R
StringAlign=Center
FontColor=#Set.Subtext_Color#
MeterStyle=Sec.String:S
