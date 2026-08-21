[Variables]
CURRENTVALLIACTION=[!UpdateMeasure mToggle "Inlay\Main"]

[isValliOn]
Measure=Calc
Formula=0
IfCondition=isValliOn = 0
IfTrueAction=[!SetVariable CURRENTVALLIACTION """[!UpdateMeasure mToggle "Inlay\Main"]"""][!UpdateMeter Home]
IfFalseAction=[!SetVariable CURRENTVALLIACTION """"""][!UpdateMeter Home]
DynamicVariables=1
UpdateDivider=-1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; [Home]
; MeterStyle=InfoStyle | Block:Off
; [Block:Off]
; LeftMouseDownAction=[!UpdateMeasure mToggle "Inlay\Main"]
; [Block:On]
; LeftMouseDownAction=[!SetOption Block MeterStyle Block:Off][!UpdateMeter Block]

