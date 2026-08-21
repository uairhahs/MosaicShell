[Variables]
Page.Items.P=(25*[Set.S])
Page.Items.H=(([BC:H]-90*[Set.S]-#Page.Items.P#*3)/2)
Page.Items.Content.P=(25*[Set.S])
Page.R=(16*[Set.S])
Sec.Container=BC
PostWelcome.Guide=0
PostWelcome.GameMode=0

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[p.SysInfo.USER_NAME]
Measure=Plugin
Plugin=SysInfo
SysInfoType=USER_NAME
UpdateDivider=-1

[0.Container]
Meter=String
; To avoid errors

[m.CheckGuide]
Measure=String
STring=#PostWelcome.Guide##PostWelcome.GameMode#
DynamicVariables=1
IfMatch=^1.$
IfMatchAction=[!WriteKeyValue Variables Set.W (#SCREENAREAWIDTH#*0.65) "#@#Vars.inc"][!WriteKeyValue Variables Set.H (0.65*#SCREENAREAWIDTH#*3/4) "#@#Vars.inc"][!WriteKeyValue Set.S Formula "((#SCREENAREAWIDTH#/1920) < (#SCREENAREAHEIGHT#/1080) ? (#SCREENAREAWIDTH#/1920) : (#SCREENAREAHEIGHT#/1080))" "#@#Vars.inc"][!SetOption Set.S Formula "((#SCREENAREAWIDTH#/1920) < (#SCREENAREAHEIGHT#/1080) ? (#SCREENAREAWIDTH#/1920) : (#SCREENAREAHEIGHT#/1080))"][!UpdateMeasure Set.S][!SetVariable Set.W (#SCREENAREAWIDTH#*0.65)][!SetVariable Set.H (0.65*#SCREENAREAWIDTH#*3/4)][!UpdateMeter *][!Redraw][!Move (#SCREENAREAWIDTH#*(0.5-0.65/2)) (#SCREENAREAHEIGHT#/2-#SCREENAREAWIDTH#*0.65*0.75/2)]
IfMatch2=^.1$
IfMatchAction2=[!Manage GameMode]
OnUpdateAction=[!WriteKeyValue Variables PostWelcome.Guide 0 "#ROOTCONFIGPATH#CoreShell\Home\Page1.inc"][!WriteKeyValue Variables PostWelcome.GameMode 0 "#ROOTCONFIGPATH#CoreShell\Home\Page1.inc"]

[LogicalScript]
Measure=Script
ScriptFile=#ROOTCONFIGPATH#Accessories\UpdatePrompt\Logic.lua

; -------------------------------- News and page update check -------------------------------- ;

[NewsParser]
Measure=WebParser
URL=https://raw.githubusercontent.com/uairhahs/MosaicShell/main/CoreWebResources.inc
RegExp=(?siU).*NewsID=(.+)Page2ID=(.+)Page3ID=(.+)$
Group=BackgroundMeasures
Disabled=(0)
FinishAction=[!CommandMeasure LogicalScript "checkNews()"]

[mNewsID]
Measure=WebParser
URL=[NewsParser]
Group=BackgroundMeasures
Disabled=(0)
StringIndex=1

[ParseNews]
Measure=WebParser
URL=https://raw.githubusercontent.com/uairhahs/MosaicShell/main/Accessories/Popup/Variants/CoreNews.inc
CodePage=1200
RegExp=(?siU).*
Debug=2
Debug2File=#ROOTCONFIGPATH#Accessories\Popup\Variants\CoreNews.inc
DynamicVariables=1
Disabled=1
Group=ParseNews
FinishAction=[!Delay 500][!commandMeasure Func "startPopup('CoreNews', 'Left')"]

; ------------------------------- Version check ------------------------------ ;

[WebParser]
Measure=WebParser
URL=https://raw.githubusercontent.com/uairhahs/MosaicShell/main/%40Resources/Version.inc
RegExp==(.*)$
Group=BackgroundMeasures
Disabled=(0)
FinishAction=[!CommandMeasure LogicalScript "check()"]
CodePage=1200

[mVer]
Measure=WebParser
URL=[WebParser]
Group=BackgroundMeasures
Disabled=(0)
StringIndex=1
CodePage=1200

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[Header.Link.Shape:S]
X=(-30*[Set.S])r
Y=(16*[Set.S])r
Shape=Ellipse 0,0,(24*[Set.S]) | StrokeWidth 0 | Extend This | OffSet (-24*[Set.S]),0
This=Fill Color #Set.Subtext_Color#,75 | Scale 1,1
CoreGrad=45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
MouseOverAction=[!SetOption "#CURRENTSECTION#" This "Fill LinearGradient CoreGrad | Scale 1.2,1.2"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
MouseLeaveAction=[!SetOption "#CURRENTSECTION#" This "Fill Color #Set.Subtext_Color#,75 | Scale 1,1"][!UpdateMeter "#CURRENTSECTION#"][!Redraw]
DynamicVariables=1
Container=#Sec.Container#
[Header.Link.Image:S]
X=(-40*[Set.S])r
Y=(-16*[Set.S])r
W=(32*[Set.S])
H=(32*[Set.S])
DynamicVariables=1
Container=#Sec.Container#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header.Shape]
Meter=Shape
X=0
Y=(#SCroll#+#Set.TitleBarH#)
Shape=Rectangle 0,0,(#Set.W#),(90*[Set.S]) | StrokeWidth 0 | Fill Color #Set.Sec_Color#
Shape2=Rectangle 0,(90*[Set.S]),(#Set.W#),(2*[Set.S]) | StrokeWidth 0 | Fill Color #Set.Sec_Color#
DynamicVariables=1
Container=#Sec.Container#

[Header.MosaicShell.String]
Meter=String
X=(#HomePage.P#)
Y=(90*[Set.S]/2+2*[Set.S]+#Scroll#+#Set.TitleBarH#)
Stringalign=LeftCenter
Text=MosaicShell
FontSize=(32*[Set.S])
FontWeight=650
FontFace=Righteous
InlineSetting=GradientColor | 45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
MeterStyle=Set.String:S
LeftMouseUpAction=[!WriteKeyValue Variables Sec.Page "00000"][!Refresh]

; ----------------------------------- Links ---------------------------------- ;
[Header.Link1.Shape]
Meter=Shape
X=(#Set.W#-#HomePage.P#)
Y=r
LeftMouseUpAction=["https://github.com/uairhahs/MosaicShell"]
MeterStyle=Header.Link.Shape:S
[Header.Link1.Image]
Meter=Image
ImageName=#@#Icons\GitHub.png
MeterStyle=Header.Link.Image:S
ToolTipTitle=GitHub
ToolTipText=Source, releases, and issues for MosaicShell.

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; ---------------------------------------------------------------------------- ;
;                               Fallback content                               ;
; ---------------------------------------------------------------------------- ;

[Fallback.String]
Meter=String
Text=Welcome to MosaicShell#CRLF#Your desktop, composed.
X=(#Set.W#/2)
Y=(#Set.H#/2)
Hidden=1
LeftMouseUpAction=["https://github.com/uairhahs/MosaicShell"]
StringAlign=CenterCenter
MeterStyle=Set.String:S
