; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[MeasureSysInfo]
Measure=SysInfo
Sysinfotype=OS_VERSION
IfMatch=11
IfMatchAction=[!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
IfNotMatchAction=[!WriteKeyValue Variables Sec.arg2 "RemoveDefaultIcon-Win10" "#CURRENTPATH#Main.ini"][!Refresh]
IfMatchMode=1
Disabled=1
Group=NUOL
[MeasureSysInfo2]
Measure=SysInfo
Sysinfotype=OS_VERSION
IfMatch=11
IfMatchAction=[!WriteKeyvalue variables IconTheme "MixerDark" "#SKINSPATH#Mixdeck\@Resources\Vars.inc"][!ActivateConfig  "Mixdeck\Main"]
IfNotMatchAction=[!ActivateConfig  "Mixdeck\Main"]
IfMatchMode=1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Mixdeck can be activated by a newly generated tray icon or by a hotkey. Check if the tray icon is in your overflow menu, and drag it to the taskbar.
InlinePattern=generated tray icon|hotkey|overflow menu|drag

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(300*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!UpdateMeasure MeasureSysInfo]
MeterStyle=Pos_Button.Shape:S
[Button.1.String]
Meter=String
Text=Done!
MeterStyle=Sec.String:S | Button.String:S
