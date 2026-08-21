[Rainmeter]
DynamicWindowSize=1
AccurateText=1
DefaultFadeDuration=100
DefaultKeepOnscreen=1
OnRefreshAction=[!CommandMeasure DefaultStartActions "Execute 1"][!WriteKeyValue "#CURRENTCONFIG#" Active 0 "#SETTINGSPATH#Rainmeter.ini"]
OnFocusAction=[!CommandMeasure WindowHandler UnMinimize()]

[p.Focus]
Measure=Plugin
Plugin=Focus

[m.OnLoad]
Measure=Calc
Formula=Counter
IfEqualValue=1
IfEqualAction=[!Delay 200][!CommandMeasure p.Focus "#CURRENTCONFIG#"]
; Fixes hang issue
UpdateDivider=-1

[Variables]
@includeCoreVars=#@#Vars.inc
@includeVer=#@#Version.inc
Scroll=0

; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

@IncludeWindow=#@#Includes/Window.inc
@IncludeMouse=#@#Includes/ScriptMouseHandler#Set.PerformanceMode#.ini

[ScaleContract]
Measure=Plugin
Plugin=PowershellRM
ScriptFile=#@#Powershell\ScaleContract.ps1
DynamicVariables=1

[ScaleContractInit]
Measure=Calc
Formula=Counter
IfEqualValue=1
IfEqualAction=[!CommandMeasure ScaleContract "Initialize-ScaleContract"]
UpdateDivider=-1

[p.FrostedGlass]
Measure=Plugin
Plugin=FrostedGlass
Type=#Set.Blur#
Corner=#Set.BlurCorner#
DynamicVariables=1
Border=All

[DefaultStartActions]
Measure=Plugin
Plugin=ActionTimer
ActionList1=Custom1 | Custom2 | Custom3 | Wait 100 | NUOLOn
NUOLOn=[!EnableMeasureGroup NUOL]
Deactivate=[!DeactivateConfig]
DynamicVariables=1

[Func]
Measure=Script
ScriptFile=#@#Lua\Func.lua

[Auto_Refresh:M]
Measure=String
OnUpdateAction=[!Refresh "#Skin.Name#\Main"]
Group=NUOL
Disabled=1

[m.ShowInToolbar]
Measure=Plugin
Plugin=ShowInToolbar
WindowTitle=MosaicShell #Skin.Name#
WindowIcon=#@#Images\Logo.ico
