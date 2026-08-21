[Variables]
; --------------------------------- Operation -------------------------------- ;
ReplaceWin=1
DroptopIntegration=0
TypeToSearch=1
BackgroundMod=0
ShowTaskbar=1
; ----------------------------- Window properties ---------------------------- ;
@includeScaleContract=#SKINSPATH##MosaicShell\@Resources\Includes\ScaleContract.inc
Scale=#UiScale#
FixW=590
MinH=0
WinR=8
WinB=1
Pad=25
; Pad is experimental and only used in a few modules.
; ------------------------------ Window blending ----------------------------- ;
Blur=Acrylic
BlurCorner=Round
ImageUnderlay=0
ImageUnderlayPath=#SKINSPATH#..\CoreData\Inlay\DefaultUnderlay.png
ImageUnderlayAlpha=50

; ---------------------------- Element properties ---------------------------- ;
Font=Segoe UI
Stroke=1
Divider=1
R=8
FontSizeModifier=1

; ----------------------------- Position settings ---------------------------- ;
MonitorIndex=1
PreserveTaskbarSpace=1
Position=CC
AnimationDisplacement=30
XPad=10
YPad=10

; ---------------------------- Animation settings ---------------------------- ;
Animated=1
EaseType=OutSine
AniDir=Bottom
AniSteps=10

; -------------------------------- Module list ------------------------------- ;
Module0=None
Module1=None
Module2=None
Module3=None
Module4=None
Module5=None
Module6=None
ModuleAllNone=1

; ---------------------------------- Colors ---------------------------------- ;
Alpha=175
Accent_Color=35,159,255
Pri_Color=36,36,36
Sec_Color=48,48,48
Ter_Color=60,60,60
Dark_Color=190,190,190
Font_Color=255,255,255
; --------------------------- Legacy module colors --------------------------- ;
BoxBorder=80,80,80
BoxColor=60,60,60
Border=54,54,54
ListShowIcon=1
ListShowFolders=0
ListMax=100


; ---------------------------------------------------------------------------- ;
;                                File structure                                ;
; ---------------------------------------------------------------------------- ;
; ------------------------------ Adding a module ----------------------------- ;
; Duplicate none image in Core\Module\None.png -> your module name
; Subpage override (settings) in Core\Module\SubpageOverrides
; Add module section in correct page (1/2/3/4) in Core\Window\ValliModule
; Duplicate module Main\Modules\Searchbar.inc -> your module name
; Add module variable files in Main\Modules\Vars
; ---------------------- Adding a color scheme / preset ---------------------- ;
; Add preset override file in @Resources\Presets
; Add color scheme section in Core\Colorscheme.inc

;;╔══════════════════════════════════════════════════════════════════════════════════════════════════════╗
;;║ METERSTYLES                                                                                          ║
;;╚══════════════════════════════════════════════════════════════════════════════════════════════════════╝


[BoldText]
FontFace=#Font#
FontWeight=800
AntiAlias=1
FontColor=#Font_Color#
FontSize=((10*#Scale#)*#FontSizeModifier#)
SolidColor=0,0,0,1

[SemiBoldText]
FontFace=#Font#
FontWeight=500
AntiAlias=1
FontColor=#Font_Color#
FontSize=((10*#Scale#)*#FontSizeModifier#)
SolidColor=0,0,0,1

[RegularText]
FontFace=#Font#
AntiAlias=1
FontColor=#Font_Color#
FontSize=((10*#Scale#)*#FontSizeModifier#)
SolidColor=0,0,0,1
