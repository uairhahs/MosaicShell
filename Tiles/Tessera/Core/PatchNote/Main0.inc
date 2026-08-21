﻿

[Title.New]
Meter=String
X=(#Sec.W#/2)
StringAlign=Center
Text=New to Tessera?
MeterStyle=Sec.String:S | Style.Title:S

[Button.1.Shape]
Meter=Shape
X=(#Sec.W#/2-270*#Sec.S#-5*#Sec.S#)
Y=(#Sec.P#)R
Shape=Rectangle 0,0,(270*#Sec.S#),(40*#Sec.S#),(#Sec.R#/2) | StrokeWidth (0) | Fill Color "" | Stroke Color "" | Extend Fill
Fill=Fill Color #Set.Positive_Color#,150
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Positive_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMEasure Func "window([[#SKINSPATH##Skin.Name#\Core\Window\Setup.inc]], '', '1')"][!CommandMeasure ActionTimer "Execute 2"]
[Button.1.String]
Meter=String
X=(270*#Sec.S#/2)r
Y=(20*#Sec.S#)r
StringAlign=CenterCenter
Text=Yes, walk me through the setup
MeterStyle=Sec.String:S | Style.Description:S

[Button.2.Shape]
Meter=Shape
X=(270*#Sec.S#/2+5*#Sec.S#)r
Y=(-20*#Sec.S#)r
Shape=Rectangle 0,0,(270*#Sec.S#),(40*#Sec.S#),(#Sec.R#/2) | StrokeWidth (0) | Fill Color "" | Stroke Color "" | Extend Fill
Fill=Fill Color #Set.Ter_Color#,150
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables "Core.WelcomeCheck" 1 "#SKINSPATH##Skin.Name#\@Resources\WelcomeVar.inc"][!Refresh]
[Button.2.String]
Meter=String
X=(270*#Sec.S#/2)r
Y=(20*#Sec.S#)r
StringAlign=CenterCenter
Text=No, show me the patch notes
MeterStyle=Sec.String:S | Style.Description:S

[ESc]
Meter=
