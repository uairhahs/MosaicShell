[Variables]
Sec.W=(500*#Sec.S#)
Sec.H=(700*#Sec.S#)
Sec.HideEsc=1
AniDir=Bottom
Log=[i] psVersion >= 5.1, proceed translation...#CRLF#[i] Last Set.Lang isn't English.#CRLF#Current Set.Lang = #CRLF#[+] Powershell verion >= 5.1#CRLF#Current Core.Ver = 44500#CRLF#Script.Lua successfully loaded at C:\Users\enhan\Documents\Rainmeter\Skins\#MosaicShell\Main\Installation\Screen.inc#CRLF#Initialize...
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

[ScriptLua]
Measure=Script
ScriptFile=#ROOTCONFIGPATH#Main\Installation\Script.lua

[Func]
Measure=script
ScriptFile=#@#Lua\Func.lua

[PSRM]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Translate.ps1
ExecutionPolicy=Unrestricted

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
Text=Translating core to #Set.Lang#...
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
Container=LogContainer
Inlinepattern=\n\[\-\].*
InlineSetting=Color | 255,0,0
Inlinepattern2=\n\[\+\].*
InlineSetting2=Color | 0,200,0
Inlinepattern3=\n\[i\].*
InlineSetting3=Color | 74, 139, 245
MeterStyle=SEc.String:S
DynamicVariables=1
