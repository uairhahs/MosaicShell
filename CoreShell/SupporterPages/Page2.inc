
[Header.Button.Icon.String]
Text=[\xe627]
[Header.Button.Text.String]
Text=Refreshing...

; ---------------------------------------------------------------------------- ;
;                                     Style                                    ;
; ---------------------------------------------------------------------------- ;

[Variables]
@includeListOfInstalledDLCs=#SKINSPATH#..\CoreData\@DLCs\InstalledDLCs.inc
@includeSecvar=#@#CacheVars\Configurator.inc
TweenNode1=0

[p.PSRM.DLC]
Measure=Plugin
Plugin=PowershellRm
ScriptFile=#@#Powershell\MosaicShellSupporter.ps1
ExecutionPolicy=Unrestricted


[DefaultStartActions]
ActionList2=
Custom1=[!CommandMeasure DefaultStartActions "Execute 2"][!Delay 100][!CommandMeasure p.PSRM.DLC "check-update"]
Restart=[!CommandMeasure DefaultStartActions "Execute 2"]
DynamicVariables=1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Loading.Image]
Meter=Image
X=([BC:W]/2-128/2*[SEt.S])
Y=([BC:H]/2-128/2*[SEt.S])
W=(128*[Set.S])
H=(128*[Set.S])
DynamicVariables=1
Container=BC
Group=Anim
ImageName=#@#Images\CoreAssets\LogoIcon.png

[Error.String]
Meter=String
X=(#Set.W#/2)
Y=(20*[Set.S])R
FontColor=255,0,0
FontSize=(16*[Set.S])
StringAlign=Center
Hidden=1
Group=errorString
MeterStyle=Set.String:S

[Error.Description.String]
Meter=String
X=r
Y=(20*[Set.S])R
FontColor=#Set.Subtext_Color#
StringAlign=Center
Hidden=1
Group=errorString
MeterStyle=Set.String:S
