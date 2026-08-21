[Set.String:S]
FontFace=#Set.Font#
AntiAlias=1
FontSize=(12*[Set.S])
FontColor=#Set.Text_Color#
Container=#Sec.Container#
SolidColor=0,0,0,1
DynamicVariables=1

[Shape.Over:S]
This=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]
OverColor=#Set.Ter_Color#
LeaveColor=#Set.Sec_Color#
DynamicVariables=1
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'OverColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]

[Shape.Over_Grad:S]
This=Fill LinearGradient 1232412414
1232412414=45 | [&Func:LocalVar('#CURRENTSECTION#', 'Grad.0')] ; 0.0 | [&Func:LocalVar('#CURRENTSECTION#', 'Grad.1')] ; 1.0
Grad.0=255,255,255
Grad.1=255,255,255
Grad.2=255,255,255
Grad.3=255,255,255
DynamicVariables=1
MouseOverAction=[!SetOption #CURRENTSECTION# 1232412414 "45 | [&Func:LocalVar('#CURRENTSECTION#', 'Grad.2')] ; 0.0 | [&Func:LocalVar('#CURRENTSECTION#', 'Grad.3')] ; 1.0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# 1232412414 "45 | [&Func:LocalVar('#CURRENTSECTION#', 'Grad.0')] ; 0.0 | [&Func:LocalVar('#CURRENTSECTION#', 'Grad.1')] ; 1.0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
