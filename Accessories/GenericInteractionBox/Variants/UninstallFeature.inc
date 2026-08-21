[Variables]
Sec.W=(600*#Sec.S#)
Sec.H=(310*#Sec.S#)
AniDir=Bottom
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

[Sec.Button:S]
X=((150/2+64/2+10)*#Sec.S#)r
Y=(-(150-64)/2*#Sec.S#)r
Shape=Rectangle 0,0,150,150,8,8 | StrokeWidth 0 | Extend Fill | Scale #Sec.S#,#Sec.S#,0,0
[Sec.Button.Image:S]
Y=((150-64)/2*#Sec.S#)r
X=((150-64)/2*#Sec.S#)r
W=(64*#Sec.S#)
H=(64*#Sec.S#)
Greyscale=1
ImageTint=#Set.Text_Color#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; [HeaderImage]
; Meter=Image
; ImageName=#@#Images\CoreAssets\#Set.IconStyle#Logo.png
; X=(#Sec.W#/2-(30*#Sec.S#)/2)
; Y=(#Sec.P#/2)
; W=(30*#Sec.S#)
; H=(30*#Sec.S#)
; ImageAlpha=25

[HeaderString]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.P#)
StringAlign=Center
Text=Missing a feature? Let me know!
MeterStyle=Sec.String:S
FontSize=(14*#sec.s#)
FontWeight=650
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0

[Description]
Meter=String
X=r
Y=R
W=(#sec.W#-#sec.p#*2)
clipstring=2
StringAlign=Center
Text=Feel free to suggest a feature on GitHub Issues!
MeterStyle=Sec.String:S
FontSize=(10*#sec.s#)

[Button.DC]
Meter=Shape
MeterStyle=Sec.Button:S
X=(#SEc.W#/2-(150/2)*#Sec.S#)
Y=(#Sec.P#)R
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color 88, 101, 242"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color 88, 101, 242, 150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Fill=Fill Color 88, 101, 242, 150
LeftMouseUpAction=["https://github.com/uairhahs/MosaicShell/issues"]
[Image.DC]
Meter=Image
MeterStyle=Sec.Button.Image:S
ImageName=#@#Icons\Discord.png

[SubtleText]
Meter=String
X=(#Sec.W#/2)
Y=(#Sec.H#-#Sec.P#)
FontColor=#Set.Subtext_Color#
StringAlign=CenterBottom
Text=Proceed uninstallation
MouseOverAction=[!SetOption #CURRENTSECTION# InlineSetting "Underline"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# InlineSetting ""][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure CoreInstallHandler "Uninstall"]
MeterStyle=Sec.String:S


[Esc]
LeftMouseUpAction=[!CommandMeasure Func "interactionBox('Uninstall', '#Arg.1#')"][!Refresh]
