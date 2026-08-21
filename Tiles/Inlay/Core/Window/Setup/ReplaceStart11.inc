; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
Sec.SaveLocation=#SKINSPATH#Inlay\WinBlock\Vars\Winblock.inc

[p.PSRM]
Measure=Plugin
Plugin=powershellrm
ScriptFile=#SKINSPATH#Inlay\@Resources\Powershell\StartToLeft.ps1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Description.String]
Text=Replacing the start button is only possible for left-aligned start button. Proceed?

[Button.1.Shape]
Meter=Shape
X=(#W#/2-(600*[S]+10*[S])/2)
Y=(#Set.P#)R
LeftMouseUpAction=[!WriteKeyValue Variables "WinblockDisplay" "1" "#Sec.Savelocation#"][!ActivateConfig "Inlay\WinBlock"][!Move 0 (#SCREENAREAHEIGHT#-40) "Inlay\WinBlock"][!CommandMeasure p.PSRM Set-TaskbarAlignment][!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.1.String]
Meter=String
Text=Yes
MeterStyle=Sec.String:S | Button.String:S

[Button.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Neu_Button.Shape:S
[Button.2.String]
Meter=String
Text=No, proceed with setup
MeterStyle=Sec.String:S | Button.String:S
