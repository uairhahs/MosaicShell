[Variables]
VersionStatus=0
BackgroundCheckStatus=0
HardwareAcceleration=0


[ContentContainer]
MouseScrollDownAction=[!SetVariable Scroll "(Clamp(#Scroll#-100,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]
MouseScrollUpAction=[!SetVariable Scroll "(Clamp(#Scroll#+100,[AnchorChecker],0))"][!UpdateMeter *][!Redraw]

[WebParser]
Measure=WebParser
URL=https://raw.githubusercontent.com/uairhahs/MosaicShell/main/%40Resources/Version.inc
RegExp==(.*)$
FinishAction=[!UpdateMeasure mver]
Group=checkForBeta
CodePage=1200
[mVer]
Measure=WebParser
CodePage=1200
URL=[WebParser]
StringIndex=1
; OnUpdateAction=[!CommandMeasure PSRM "Check"]
Ifcondition=mVer = #Core.Ver#
IfTrueAction=[!SetVariable VersionStatus "0"][!UpdateMeter *][!Redraw]
IfCondition2=mVer > #Core.Ver#
IfTrueAction2=[!SetVariable VersionStatus "1"][!UpdateMeter *][!Redraw]
IfCondition3=mVer < #Core.Ver#
IfTrueAction3=[!SetVariable VersionStatus "2"][!UpdateMeter *][!Redraw]
; [mVer]
; Measure=Calc
; Formula=5000

[UpdatePromptActiveChecker]
Measure=Plugin
Plugin=ConfigActive
ConfigName=#MosaicShell\Accessories\UpdatePrompt
IfCondition=UpdatePromptActiveChecker = 1
IfTrueAction=[!SetVariable BackgroundCheckStatus 1][!UpdateMeter BackgroundCheckStatus][!Redraw]
IfFalseAction=[!SetVariable BackgroundCheckStatus 0][!UpdateMeter BackgroundCheckStatus][!Redraw]

[PSRM]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\AddShortcut.ps1

[CoreInstallHandler]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Installer.ps1
ExecutionPolicy=Unrestricted

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Section.Shape:S]
X=(30*[Set.S])
Y=(75*[Set.S])r
Shape=Rectangle 0,0,([ContentContainer:W]-#Set.P#),(100*[Set.S]),(#Set.R#/4) | StrokeWidth 0 | Fill Color #Set.Sec_Color#
[Section.Icon:S]
X=(#Set.P#)r
Y=(100/2*[Set.S])r
FontSize=(20*[set.s])
[Section.String:S]
X=(5*[Set.S])R
Y=r
W=(([ContentContainer:W]-#Set.P#)/2)
ClipString=2
FontSize=(10*[Set.S])
Container=ContentContainer
StringAlign=LeftCenter
InlinePattern=#CRLF#.*
InlineSetting=Color | #Set.SubText_Color#

[SectionButton:S]
Y=(-40/2*[Set.S])r
X=(-(90+150)*[Set.S])r
Shape=Rectangle 0,0,(150*[Set.S]),(40*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend This
This=Fill Color #Set.Text_Color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Text_Color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Container=ContentContainer
DynamicVariables=1
[SectionButton.String:S]
X=(150/2*[Set.S])r
Y=(40/2*[Set.S])r
FontSize=(10*[Set.S])
FontFace=#Set.Font#
Container=ContentContainer
StringAlign=CenterCEnter
DynamicVariables=1

[Section4.ButtonProg.False]
LeftMouseUpAction=[!CommandMeasure PSRM "StartFolder"][!SetOption Section4.Button2.Shape MeterStyle "SectionButton:S | Section4.ButtonProg.True"][!UpdateMeter Section4.Button2.Shape][!Redraw]
This=Fill Color #Set.Text_Color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Text_Color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
[Section4.ButtonProg.True]
This=Fill Color 0,255,0,100
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,255,0,200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,255,0,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure PSRM "RemoveStartFolder"][!SetOption Section4.Button2.Shape MeterStyle "SectionButton:S | Section4.ButtonProg.False"][!UpdateMeter Section4.Button2.Shape][!Redraw]
[Section4.ButtonDesk.False]
LeftMouseUpAction=[!CommandMeasure PSRM "Desktop"][!SetOption Section4.Button1.Shape MeterStyle "SectionButton:S | Section4.ButtonDesk.True"][!UpdateMeter Section4.Button1.Shape][!Redraw]
This=Fill Color #Set.Text_Color#,50
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Text_Color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
[Section4.ButtonDesk.True]
This=Fill Color 0,255,0,100
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,255,0,200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color 0,255,0,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure PSRM "RemoveDeskop"][!SetOption Section4.Button1.Shape MeterStyle "SectionButton:S | Section4.ButtonDesk.False"][!UpdateMeter Section4.Button1.Shape][!Redraw]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Behavior]
Meter=String
Text=Version
MeterStyle=Set.String:S | Set.OptionCat:S

; --------------------------------- Section1 --------------------------------- ;

[Section1]
Meter=Shape
X=r
Y=(15*[Set.S])R
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section1.Icon]
Meter=String
Text=[\xecc5]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S | Section1.Icon:#VersionStatus#
[Section1.Icon:0]
FontColor=#Set.Text_Color#
[Section1.Icon:1]
FontColor=#Set.Accent_Color#
InlineSetting=Shadow | 0 | 0 | 2 | #Set.Accent_Color#
[Section1.Icon:2]
FontColor=#Set.Text_Color#

[Section1.Text]
Meter=String
MeterStyle=Set.String:S | Section.String:S | Section1.Text:#VersionStatus#
Leftmouseupaction=["https://github.com/uairhahs/MosaicShell/releases/latest"]
[Section1.Text:0]
Text=v#Core.Ver##CRLF#You're up to date
[Section1.Text:1]
Text=v#Core.Ver# < v[mVer] #CRLF#See new version patch notes
[Section1.Text:2]
Text=v#Core.Ver# - Beta#CRLF#View release details

; Button1
[Section1.Button]
Meter=Shape
MeterStyle=Set.Button:S | SectionButton:S | Section1.Button:#VersionStatus#
Y=(-40/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-150*[Set.S])
[Section1.Button:0]
LeftMouseUpAction=[!Refresh]
[Section1.Button:1]
LeftMouseUpAction=[!CommandMeasure CoreInstallHandler "Install MosaicShell"]
[Section1.Button:2]
LeftMouseUpAction=[!Refresh]
[Section1.Button.Value]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S | Section1.Button.Value:#VersionStatus#
[Section1.Button.Value:0]
Text=Check for updates
[Section1.Button.Value:1]
Text=Update now
[Section1.Button.Value:2]
Text=Check for updates
; Button2
[Section1.Button2]
Meter=Shape
MeterStyle=Set.Button:S | SectionButton:S
Y=(-40/2*[Set.S])r
X=(-(90+150)*[Set.S])r
LeftMouseUpAction=[!CommandMeasure CoreInstallHandler "Install MosaicShell"]
Hidden=(#VersionStatus# = 1 ? 1 : 0)
[Section1.Button2.Value]
Meter=String
Text=Force update
MeterStyle=Set.String:S | SectionButton.String:S
Hidden=(#VersionStatus# = 1 ? 1 : 0)
; Button3
[Section1.Button3]
Meter=Shape
MeterStyle=Set.Button:S | SectionButton:S
Y=(-40/2*[Set.S])r
X=(-(90+150)*[Set.S])r
LeftMouseUpAction=[!CommandMeasure Func "interactionBox('Rollback', 'MosaicShell', '#Version#')"]
Hidden=(#VersionStatus# = 1 ? 1 : 0)
[Section1.Button3.Value]
Meter=String
Text=Version history
MeterStyle=Set.String:S | SectionButton.String:S
Hidden=(#VersionStatus# = 1 ? 1 : 0)

; --------------------------------- Section4 --------------------------------- ;

[Section4]
Meter=Shape
X=(30*[Set.S])
Y=(65*[Set.S])r
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section4.Icon]
Meter=String
Text=[\xe751]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section4.Text]
Meter=String
Text=Add shortcut to desktop#CRLF#For easy access to MosaicShell
MeterStyle=Set.String:S | Section.String:S


[Section4.Button1.Shape]
Meter=Shape
MeterStyle=SectionButton:S | Section4.ButtonDesk.False
Y=(-40/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-150*[Set.S])
[Section4.Button1.Text]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S
Text=Desktop
[Section4.Button2.Shape]
Meter=Shape
MeterStyle=SectionButton:S | Section4.ButtonProg.False
[Section4.Button2.Text]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S
Text=Program list

; ---------------------------------------------------------------------------- ;
;                                 Core options                                 ;
; ---------------------------------------------------------------------------- ;

[CoreOptions]
Meter=String
Text=Core options
Y=(80*[Set.S])r
MeterStyle=Set.String:S | Set.OptionCat:S

; --------------------------------- Section2 --------------------------------- ;

[Section2]
Meter=Shape
X=r
Y=(15*[Set.S])R
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section2.Icon]
Meter=String
Text=[\xe9a1]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section2.Text]
Meter=String
Text=Check for Core updates on startup (Recommended)#CRLF#Core will not check for updates in the background
MeterStyle=Set.String:S | Section.String:S

[BackgroundCheckStatus]
Meter=Shape
MeterStyle=Set.Bool:S | BackgroundCheckStatus:#BackgroundCheckStatus#
Y=(-25/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-50*[Set.S])
[BackgroundCheckStatus:0]
Leftmouseupaction=[!ActivateConfig "#MosaicShell\Accessories\UpdatePrompt"][!UpdateMeasure UpdatePromptActiveChecker]
[BackgroundCheckStatus:1]
Leftmouseupaction=[!DeactivateConfig "#MosaicShell\Accessories\UpdatePrompt"][!UpdateMeasure UpdatePromptActiveChecker]

; --------------------------------- Section5 --------------------------------- ;

[Section5]
Meter=Shape
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section5.Icon]
Meter=String
Text=[\xebd2]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section5.Text]
Meter=String
Text=Hardware acceleration (Recommended)#CRLf#Allocates processes of MosaicShell to your graphics processor
MeterStyle=Set.String:S | Section.String:S

[HardwareAcceleration]
Meter=Shape
MeterStyle=Set.Bool:S
Y=(-25/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-50*[Set.S])
LeftMouseUpAction=[!CommandMeasure PSRM "ToggleHA"]

; --------------------------------- Section3 --------------------------------- ;

[Section3]
Meter=Shape
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section3.Icon]
Meter=String
Text=[\xec4a]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section3.Text]
Meter=String
Text=Performance mode#CRLF#Turns off Window resizing, sliding animations and more
MeterStyle=Set.String:S | Section.String:S

[Set.PerformanceMode]
Meter=Shape
MeterStyle=Set.Bool:S | PerformanceMode:#Set.PerformanceMode#
Y=(-25/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-50*[Set.S])
[PerformanceMode:0]
Leftmouseupaction=[!FadeDuration 0][!WriteKeyValue Variables Set.Blur None "#Sec.SaveLocation#"][!WriteKeyValue Variables Set.W (#SCREENAREAWIDTH#*0.65) "#@#Vars.inc"][!WriteKeyValue Variables Set.H (0.65*#SCREENAREAWIDTH#*3/4) "#@#Vars.inc"][!WriteKeyValue Set.S Formula "((#SCREENAREAWIDTH#/1920) < (#SCREENAREAHEIGHT#/1080) ? (#SCREENAREAWIDTH#/1920) : (#SCREENAREAHEIGHT#/1080))" "#@#Vars.inc"][!Move (#SCREENAREAWIDTH#*(0.5-0.65/2)) (#SCREENAREAHEIGHT#/2-#SCREENAREAWIDTH#*0.65*0.75/2)][!WriteKeyValue Variables #CURRENTSECTION# 1 "#Sec.SaveLocation#"][!Refresh]
[PerformanceMode:1]
Leftmouseupaction=[!FadeDuration 100][!WriteKeyValue Variables #CURRENTSECTION# 0 "#Sec.SaveLocation#"][!WriteKeyValue Variables Set.Blur Blur "#Sec.SaveLocation#"][!Refresh]

; --------------------------------- Section6 --------------------------------- ;

[Section6]
Meter=Shape
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section6.Icon]
Meter=String
Text=[\xe751]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section6.Text]
Meter=String
Text=Game mode#CRLF#Closes all of MosaicShell when full-screen games are launched
MeterStyle=Set.String:S | Section.String:S


[Section6.Button1.Shape]
Meter=Shape
MeterStyle=SectionButton:S
Y=(-40/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-150*[Set.S])
LeftMouseUpAction=[!Manage GameMode]["#@#Actions\AHKv1.exe" "#@#Addons\CoreAHKFunctions.ahk" Setup_GameMode_FullScreenToggleOn]
[Section6.Button1.Text]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S
Text=Turn on
[Section6.Button2.Shape]
Meter=Shape
LeftMouseUpAction=[!Manage GameMode]["#@#Actions\AHKv1.exe" "#@#Addons\CoreAHKFunctions.ahk" Setup_GameMode_FullScreenToggleOff]
MeterStyle=SectionButton:S
[Section6.Button2.Text]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S
Text=Turn off

; ---------------------------------------------------------------------------- ;
;                                 Debug options                                ;
; ---------------------------------------------------------------------------- ;

[CoreDebug]
Meter=String
Text=Debug options
Y=(80*[Set.S])r
MeterStyle=Set.String:S | Set.OptionCat:S

; --------------------------------- Section7 --------------------------------- ;

[Section7]
Meter=Shape
X=r
Y=(15*[Set.S])R
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section7.Icon]
Meter=String
Text=[\xf19d]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section7.Text]
Meter=String
Text=Fix blurring visuals#CRLF#Use the options on the right to select whether to use absolute scaling or not
MeterStyle=Set.String:S | Section.String:S

[Section7.Button1.Shape]
Meter=Shape
MeterStyle=SectionButton:S
Y=(-40/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-150*[Set.S])
LeftMouseUpAction=[!CommandMeasure PSRM "Set-DPICompatability"]
[Section7.Button1.Text]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S
Text=Absolute
[Section7.Button2.Shape]
Meter=Shape
LeftMouseUpAction=[!CommandMeasure PSRM "Reset-DPICompatability"]
MeterStyle=SectionButton:S
[Section7.Button2.Text]
Meter=String
MeterStyle=Set.String:S | SectionButton.String:S
Text=Default

; --------------------------------- Section8 --------------------------------- ;

[Section8]
Meter=Shape
X=(30*[Set.S])
Y=(65*[Set.S])r
MeterStyle=Section.Shape:S
Container=ContentContainer
DynamicVariables=1

[Section8.Icon]
Meter=String
Text=[\xec26]
MeterStyle=Set.String:S | Set.Icon:S | Section.Icon:S

[Section8.Text]
Meter=String
Text=Use legacy .rmskin extension installer#CRLF#Runs the .rmskin file to install a module instead of extracting directly.
MeterStyle=Set.String:S | Section.String:S

[Set.UseExtInstaller]
Meter=Shape
MeterStyle=Set.Bool:S
Y=(-25/2*[Set.S])r
X=([ContentContainer:W]-#Set.P#-50*[Set.S])

; ---------------------------------------------------------------------------- ;
;                                Core overrides                                ;
; ---------------------------------------------------------------------------- ;

[Auto_Refresh:M]
Measure=

[Help:Category]
Y=(#Set.H#-100*[Set.S])

[DivCat:1293s18]
Meter=
[BottomButton]
Meter=
[BottomText]
Meter=
