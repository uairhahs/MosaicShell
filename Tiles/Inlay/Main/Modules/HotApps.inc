; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
@includeModuleVariable=Vars\HotApps.inc

[FuncPS]
Measure=Plugin
Plugin=PowerShellRM
ScriptFile=#@#Powershell\Func.ps1

[USER_SID]
Measure=SysInfo
SysInfoType=USER_SID
IfMatch=^(S\-1\-5\-21\-[\S]+)$
IfMatchAction=[!EnableMeasureGroup Location]
RegExpSubstitute=1
Substitute="^0$":""

[CurrentUserProfilePictureSizes]
Group=Location
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\[USER_SID]
OutputType=ValueList
OutputDelimiter=,
DynamicVariables=1
Disabled=1

; Note: The following measure specifies the location of the current users profile
; picture if it exists. A default picture will be shown if no user picture is
; available.
[CurrentUserProfilePictureLocation]
Group=Location
Measure=Registry
RegHKey=HKEY_LOCAL_MACHINE
RegKey=SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\[USER_SID]
RegValue=Image[&Func:FindLargestNumber('[&CurrentUserProfilePictureSizes]')]
DynamicVariables=1
Disabled=1
IfMatch=(S\-1\-5\-21\-[\S]+)
IfMatchAction=[!SetOption UserImage.Image MeasureName "#CURRENTSECTION#"][!UpdateMeter UserImage.Image]
IfNotMatchAction=[!SetOption UserImage.Image MeasureName ""][!UpdateMeter UserImage.Image]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[UserImage.Shape]
Meter=Shape
X=(50*#Scale#)
y=((90/2-32/2)*#Scale#+2)r
Shape=Rectangle 0,0,32,32,16 | StrokeWidth 2 | Stroke Color #BoxColor# | Scale #Scale#,#Scale#,0,0
[UserImage.Container]
Meter=Shape
X=r
Y=r
Shape=Rectangle 0,0,32,32,16 | StrokeWidth 0 | Scale #Scale#,#Scale#,0,0
[UserImage.Image]
Meter=Image
X=r
Y=r
W=(32*#scale#)
H=(32*#scale#)
ImageName=#@#Images\DefaultUser.png
Container=userImage.Container
LeftMouseUpAction=["ms-settings:yourinfo"]
[UserImage.String]
Meter=String
MeasureName=MeasureUser
Text=#UserName#
X=(15*#scale#)R
Y=(16*#scale#)r
StringAlign=LeftCenter
MeterStyle=RegularText
[Button.BackShape]
Meter=Shape
Y=r
Hidden=1
Shape=Rectangle (-9*#Scale#),(-18*#Scale#),(36*#Scale#),(36*#Scale#),(5*#Scale#) | StrokeWidth 0 | Fill Color #Font_Color#,25
[Button.StringIcon:S]
X=(-50*#scale#)r
Y=r
FontFace=Segoe Fluent Icons
FontSize=((14*#Scale#)*#FontSizeModifier#)
StringAlign=RightCenter
DynamicVariables=1
MouseOverAction=[!SetOption Button.Backshape X [#CURRENTSECTION#:X]][!ShowMeter Button.BackShape][!UpdateMeter Button.BackShape][!Redraw]
MouseLeaveAction=[!HideMeter Button.BackShape][!Redraw]
[ShutdownButton.StringIcon]
Meter=String
Text=[\xe7e8]
X=(#W#-50*#scale#)
MeterStyle=RegularText | Button.StringIcon:S
LeftMouseUpAction=[!CommandMeasure Func "LaunchPopup('[#CURRENTSECTION#:X]','[#CURRENTSECTION#:Y]','Power')"]

@includeHotApp1=#ROOTCONFIGPATH#Main\HotApps\#HotApp1#.inc
@includeHotApp2=#ROOTCONFIGPATH#Main\HotApps\#HotApp2#.inc
@includeHotApp3=#ROOTCONFIGPATH#Main\HotApps\#HotApp3#.inc
@includeHotApp4=#ROOTCONFIGPATH#Main\HotApps\#HotApp4#.inc
@includeHotApp5=#ROOTCONFIGPATH#Main\HotApps\#HotApp5#.inc
@includeHotApp6=#ROOTCONFIGPATH#Main\HotApps\#HotApp6#.inc

[HotAppsAnchor]
Meter=String
Y=((90/2)*#Scale#+2)r
