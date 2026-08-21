[Variables]
Sec.SaveLocation=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
; ------------------------------ User variables ------------------------------ ;
UserNameCheckVariable=mosaicshell
AlreadyHasImageMagick=0
; ---------------------------------- Remote ---------------------------------- ;
Set.Lang=English
Set.UseExtInstaller=0
; -------------------------- Window and performance -------------------------- ;
Set.W=1129
Set.H=828
Set.Maximized=0
Set.PerformanceMode=0
Set.LegacyWindowResizing=0
; ----------------------------- Scaling variables ---------------------------- ;
@includeScaleVars=#@#ScaleVars.inc
@includeScaleContract=#@#Includes\ScaleContract.inc
Sec.SaveLocationScale=#@#ScaleVars.inc
Set.Home.P=(100*[Set.S])
Set.L=(200*([Set.S]+0.2))
Set.P=(#Set.P.Handle#*[Set.S])
Set.P.Handle=40
Set.R=10
Set.WinR=2
Set.WinB=2
; --------------------------- Appearance variables --------------------------- ;
; Catppuccin Mocha with Sky / Mauve accents from MosaicShell logo
Set.Style=CoreUIv2
Set.IconStyle=Normal
Set.Alpha=255
Set.Pri_Color=17,17,27
Set.Sec_Color=30,30,46
Set.Ter_Color=49,50,68
Set.Accent_Color=137,220,235
Set.Accent_Color_2=203,166,247
Set.Text_Color=205,214,244
Set.SubText_Color=166,173,200
Set.Positive_Color=166,227,161
Set.Font=Poppins
Set.Blur=None
Set.BlurCorner=Round

[Set.S]
Measure=Calc
Formula=(#DpiScale#*#UserScale#)
DynamicVariables=1

[Set.WindowSave]
Measure=Calc
X=2106
Y=137
W=1278
H=807
S=1
Set.Minimized=false
