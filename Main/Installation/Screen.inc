[Variables]
Sec.W=(500*#Sec.S#)
Sec.H=(700*#Sec.S#)
Sec.HideEsc=1
AniDir=Bottom

Log=Initialize...
SkipTrans=0
@includeCorever=#@#Version.inc
[moveX]
Formula=(#SCREENAREAWIDTH#*0.5)
[moveY]
Formula=(#SCREENAREAHEIGHT#*0.5)
[anchorX]
Formula=50
[anchorY]
Formula=50

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[MeasurePSVer]
Measure=Plugin
Plugin=RunCommand
Program=PowerShell
Parameter=$a=($PSVersionTable.PSVersion); \"$($a.Major).$($a.Minor)\"
OutputType=ANSI
OnUpdateAction=[!CommandMeasure MeasurePSVer "Run"]
FinishAction=[!CommandMeasure ScriptLua "checkPS()"]
State=Hide
Disabled=1

[p.SysInfo.USER_NAME]
Measure=Plugin
Plugin=SysInfo
SysInfoType=USER_NAME
UpdateDivider=-1
OnUpdateAction=[!CommandMeasure ScriptLua "checkNAME()"]
Disabled=1

[ScriptLua]
Measure=Script
ScriptFile=#ROOTCONFIGPATH#Main\Installation\Script.lua

[Func]
Measure=script
ScriptFile=#@#Lua\Func.lua

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[ImgFade]
Meter=Image
X=(#Sec.W#/2-[ImgFade:W]/2)
Y=(#Sec.H#/2-[ImgFade:W]/2)
W=(256*#Sec.S#)
H=([ImgFade:W])
DynamicVariables=1
ImageAlpha=50
ImageName=#@#Images\CoreAssets\LogoIconAccent.png

[LogTitle]
Meter=String
X=(#Sec.P#)
Y=(#Sec.P#)
Text=Verifying installation of Core...
FontWeight=650
FontSize=(15*#Sec.S#)
MeterStyle=Sec.String:S
[LogContainer]
Meter=String
X=r
Y=(#Sec.P#)R
W=(#Sec.W#-#sec.P#*2)
H=(#Sec.H#-#Sec.P#*3-[LogTitle:H])
SolidColor=255,255,255
DynamicVariables=1
[Log]
MEter=String
Text=#Log#
W=(#SEc.W#-#Sec.P#*2)
ClipString=2
Container=LogContainer
Inlinepattern=\[\-\].*
InlineSetting=Color | 255,0,0
Inlinepattern2=\[\+\].*
InlineSetting2=Color | 0,200,0
Inlinepattern3=\[i\].*
InlineSetting3=Color | 74, 139, 245
MeterStyle=SEc.String:S
DynamicVariables=1

[PSButton.Yes:Shape]
Meter=Shape
X=((#Sec.W#)/2-(150*2*#Sec.S#+#sec.P#)/2)
Y=(#Sec.H#-#sec.P#-40*#sec.S#)
ColorLeave=0,100,0,150
ColorOver=0,100,0,250
MeterStyle=Sec.Button:S
Group=PSButton
Hidden=1
[PSButton.Yes:Shape:Cant]
LeftMouseUpAction=[!CommandMeasure ScriptLua "checkTRANS()"]
[PSButton.Yes:Shape:Not]
LeftMouseUpAction=["http://woshub.com/install-update-powershell-windows/"][!CommandMeasure ActionTimer "Execute 2"]
[PSButton.Yes:String]
Meter=String
Text=Continue
Group=PSButton
Hidden=1
MEterStyle=Sec.String:S | Sec.Text:S

[PSButton.No:Shape]
Meter=Shape
LeftMouseUpAction=[!CommandMeasure ActionTimer "Execute 2"]
MeterStyle=Sec.Button:S
Group=PSButton
Hidden=1
[PSButton.No:String]
Meter=String
Text=Quit
Group=PSButton | PSButtonString
Hidden=1
MEterStyle=Sec.String:S | Sec.Text:S
