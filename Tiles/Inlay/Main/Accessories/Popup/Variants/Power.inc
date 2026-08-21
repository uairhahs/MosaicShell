
; ---------------------------------------------------------------------------- ;
;                          Context option meter styles                         ;
; ---------------------------------------------------------------------------- ;

[Hybrid:Act]
LeftMouseUpAction=[%systemroot%\system32\shutdown.exe -s -hybrid -t 00][!UpdateMeasure mToggle]
[Hybrid:Text]
Text=Hybrid

[Shutdown:Act]
LeftMouseUpAction=[%systemroot%\system32\shutdown.exe -s -t 00][!UpdateMeasure mToggle]
[Shutdown:Text]
Text=Shutdown

[Restart:Act]
LeftMouseUpAction=[%systemroot%\system32\shutdown.exe -r -t 00][!UpdateMeasure mToggle]
[Restart:Text]
Text=Restart

[Sleep:Act]
LeftMouseUpAction=["#@#Actions\nircmd.exe" standby][!UpdateMeasure mToggle]
[Sleep:Text]
Text=Sleep

[Lock:Act]
LeftMouseUpAction=[%windir%\System32\rundll32.exe user32.dll LockWorkStation][!UpdateMeasure mToggle]
[Lock:Text]
Text=Lock

[Logoff:Act]
LeftMouseUpAction=[%systemroot%\system32\shutdown.exe -l][!UpdateMeasure mToggle]
[Logoff:Text]
Text=Logoff

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Lock]
Meter=Shape
Y=(#Sec.P#+1)
MeterStyle=SecBox:S | Lock:Act
[LockIcon]
Meter=Image
MeterStyle=SecIcon:S
[LockText]
Meter=String
MeterStyle=Sec.String:S | SecText:S | Lock:Text

[Logoff]
Meter=Shape
MeterStyle=SecBox:S | Logoff:Act
[LogoffIcon]
Meter=Image
MeterStyle=SecIcon:S
[LogoffText]
Meter=String
MeterStyle=Sec.String:S | SecText:S | Logoff:Text

[Sleep]
Meter=Shape
MeterStyle=SecBox:S | Sleep:Act
[SleepIcon]
Meter=Image
MeterStyle=SecIcon:S
[SleepText]
Meter=String
MeterStyle=Sec.String:S | SecText:S | Sleep:Text

[Restart]
Meter=Shape
MeterStyle=SecBox:S | Restart:Act
[RestartIcon]
Meter=Image
MeterStyle=SecIcon:S
[RestartText]
Meter=String
MeterStyle=Sec.String:S | SecText:S | Restart:Text

[Hybrid]
Meter=Shape
MeterStyle=SecBox:S | Hybrid:Act
[HybridIcon]
Meter=Image
MeterStyle=SecIcon:S
[HybridText]
Meter=String
MeterStyle=Sec.String:S | SecText:S | Hybrid:Text

[Shutdown]
Meter=Shape
MeterStyle=SecBox:S | Shutdown:Act
[ShutdownIcon]
Meter=Image
MeterStyle=SecIcon:S
[ShutdownText]
Meter=String
MeterStyle=Sec.String:S | SecText:S | Shutdown:Text
