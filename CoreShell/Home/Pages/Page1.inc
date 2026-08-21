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
IfMatchAction=[!WriteKeyValue Variables Set.W (#SCREENAREAWIDTH#*0.65) "#@#Vars.inc"][!WriteKeyValue Variables Set.H (0.65*#SCREENAREAWIDTH#*3/4) "#@#Vars.inc"][!WriteKeyValue Set.S Formula "(#DpiScale#*#UserScale#)" "#@#Vars.inc"][!SetOption Set.S Formula "(#DpiScale#*#UserScale#)"][!UpdateMeasure Set.S][!SetVariable Set.W (#SCREENAREAWIDTH#*0.65)][!SetVariable Set.H (0.65*#SCREENAREAWIDTH#*3/4)][!UpdateMeter *][!Redraw][!Move (#SCREENAREAWIDTH#*(0.5-0.65/2)) (#SCREENAREAHEIGHT#/2-#SCREENAREAWIDTH#*0.65*0.75/2)]
IfMatch2=^.1$
IfMatchAction2=[!Manage GameMode]
OnUpdateAction=[!WriteKeyValue Variables PostWelcome.Guide 0 "#ROOTCONFIGPATH#CoreShell\Home\Pages\Page1.inc"][!WriteKeyValue Variables PostWelcome.GameMode 0 "#ROOTCONFIGPATH#CoreShell\Home\Pages\Page1.inc"]

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

[Discover.Card.Shape:S]
X=#HomePage.P#
Y=(#Page.Items.P#)R
Shape=Rectangle 0,0,((#Set.W#-#HomePage.P#*3)/2),#Page.Items.H#,(#Set.R#) | StrokeWidth 0 | Extend This
This=Fill Color #Set.Sec_Color#
DynamicVariables=1
Container=#Sec.Container#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]

[Discover.Card.Title:S]
X=(#Page.Items.Content.P#)r
Y=(#Page.Items.Content.P#)r
FontSize=(18*[Set.S])
FontWeight=650
Container=#Sec.Container#

[Discover.Card.Body:S]
X=r
Y=(8*[Set.S])R
W=((#Set.W#-#HomePage.P#*3)/2-#Page.Items.Content.P#*2)
ClipString=2
FontColor=#Set.SubText_Color#
Container=#Sec.Container#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header.String]
Text=Discover

; ----------------------------------- Links ---------------------------------- ;
[Header.Link1.Shape]
Meter=Shape
X=(#Set.W#-#HomePage.P#)
Y=(90*[Set.S]/2+2*[Set.S]+#Scroll#)
LeftMouseUpAction=["https://github.com/uairhahs/MosaicShell"]
MeterStyle=Header.Link.Shape:S
[Header.Link1.Image]
Meter=Image
ImageName=#@#Icons\GitHub.png
MeterStyle=Header.Link.Image:S
ToolTipTitle=GitHub
ToolTipText=Source, releases, and issues for MosaicShell.

; ----------------------------- Discover cards ------------------------------- ;

[Card.Library.Shape]
Meter=Shape
X=#HomePage.P#
Y=(90*[Set.S]+#Page.Items.P#+#Scroll#)
MeterStyle=Discover.Card.Shape:S
LeftMouseUpAction=[!WriteKeyValue Variables Sec.Page "3"][!Refresh]
[Card.Library.Title]
Meter=String
Text=Library
MeterStyle=Set.String:S | Discover.Card.Title:S
[Card.Library.Body]
Meter=String
Text=Browse installed tiles and open module settings from one place.
MeterStyle=Set.String:S | Discover.Card.Body:S

[Card.Settings.Shape]
Meter=Shape
X=(#Set.W#/2+#HomePage.P#/2)
Y=(90*[Set.S]+#Page.Items.P#+#Scroll#)
MeterStyle=Discover.Card.Shape:S
LeftMouseUpAction=[!WriteKeyValue Variables Skin.Name "#MosaicShell" "#@#CacheVars\Configurator.inc"][!WriteKeyValue Variables Skin.Set_Page General "#@#CacheVars\Configurator.inc"][!ActivateConfig "#CURRENTCONFIG#" "Settings.ini"]
[Card.Settings.Title]
Meter=String
Text=Settings
MeterStyle=Set.String:S | Discover.Card.Title:S
[Card.Settings.Body]
Meter=String
Text=Theme, scale, blur, and MosaicShell appearance options.
MeterStyle=Set.String:S | Discover.Card.Body:S

[Card.Welcome.Shape]
Meter=Shape
X=#HomePage.P#
Y=(#Page.Items.P#)R
MeterStyle=Discover.Card.Shape:S
LeftMouseUpAction=[!WriteKeyValue Variables process.page 0 "#ROOTCONFIGPATH#Accessories\Welcome\Main.ini"][!ActivateConfig "#MosaicShell\Accessories\Welcome"]
[Card.Welcome.Title]
Meter=String
Text=Welcome setup
MeterStyle=Set.String:S | Discover.Card.Title:S
[Card.Welcome.Body]
Meter=String
Text=Re-run the first-run guide for desktop shortcuts and startup options.
MeterStyle=Set.String:S | Discover.Card.Body:S

[Card.About.Shape]
Meter=Shape
X=(#Set.W#/2+#HomePage.P#/2)
Y=r
MeterStyle=Discover.Card.Shape:S
LeftMouseUpAction=[!WriteKeyValue Variables Sec.Page "5"][!Refresh]
[Card.About.Title]
Meter=String
Text=About
MeterStyle=Set.String:S | Discover.Card.Title:S
[Card.About.Body]
Meter=String
Text=What MosaicShell is and how the composable desktop layer fits together.
MeterStyle=Set.String:S | Discover.Card.Body:S

; Title easter egg kept subtle via right-click on header string in Home.ini — Discover title is not a hidden door.

[DevLink]
Meter=String
X=(#Set.W#-#HomePage.P#)
Y=([BC:H]-#Set.P#*2+#Scroll#)
StringAlign=RightBottom
FontSize=(10*[Set.S])
FontColor=#Set.SubText_Color#,80
Text=Developer tools
MeterStyle=Set.String:S
MouseOverAction=[!SetOption #CURRENTSECTION# FontColor "#Set.SubText_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# FontColor "#Set.SubText_Color#,80"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables Sec.Page "00000"][!Refresh]
ToolTipText=Internal setup and packaging utilities
