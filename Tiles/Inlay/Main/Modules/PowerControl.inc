; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;


[Hybrid:Act]
LeftMouseUpAction=[%systemroot%\system32\shutdown.exe -s -hybrid -t 00][!UpdateMeasure mToggle]
[Hybrid:Text]
Text=Hybrid shutdown

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

[module_powercontrol.image:S]
ImageName=#@#Icons\Popup\#CURRENTSECTION#Icon.png
X=(20*#scale#)R
Y=r
W=(24*#Scale#)
H=(24*#Scale#)
MouseOverAction=[!SetOption Button.Backshape X [#CURRENTSECTION#:X]][!ShowMeter Button.BackShape][!UpdateMeter Button.BackShape][!Redraw]
MouseLeaveAction=[!HideMeter Button.BackShape][!Redraw]
ToolTipText=#CURRENTSECTION#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Button.BackShape]
Meter=Shape
Y=((90/2)*#Scale#+2)r
Hidden=1
Shape=Rectangle (-6*#Scale#),(-18*#Scale#),(36*#Scale#),(36*#Scale#),(5*#Scale#) | StrokeWidth 0 | Fill Color #Font_Color#,25
[Lock]
Meter=Image
X=(#W#/2-(24*6+20*5)*#Scale#/2)
Y=(-24/2*#Scale#)r
MeterStyle=module_powercontrol.image:S | #CURRENTSECTION#:Act
[Logoff]
Meter=Image
MeterStyle=module_powercontrol.image:S | #CURRENTSECTION#:Act
[Sleep]
Meter=Image
MeterStyle=module_powercontrol.image:S | #CURRENTSECTION#:Act
[Restart]
Meter=Image
MeterStyle=module_powercontrol.image:S | #CURRENTSECTION#:Act
[Hybrid]
Meter=Image
ToolTipText=Hybrid shutdown
MeterStyle=module_powercontrol.image:S | #CURRENTSECTION#:Act
[Shutdown]
Meter=Image
MeterStyle=module_powercontrol.image:S | #CURRENTSECTION#:Act

[PowerControlAnchor]
Meter=String
Y=((90/2)*#Scale#+(24/2)*#Scale#)r
