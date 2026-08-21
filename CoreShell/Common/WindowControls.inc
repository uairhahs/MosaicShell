
[DragBar.Shape]
Meter=Shape
X=(#Set.WinB#/2)
Y=(#Set.WinB#/2)
Shape=Rectangle 0,0,#Set.W#,(#Set.TitleBarH#),#Set.WinR# | StrokeWidth #Set.WinB# | Fill Color #Set.Ter_Color# | Stroke Color #Set.Ter_Color#
MouseOverAction=[!CommandMeasure WindowHandler ToggleDrag(1)]
MouseLeaveAction=[!CommandMeasure WindowHandler ToggleDrag(0)]
LeftMouseDoubleClickAction=[!CommandMeasure WindowHandler ToggleMaximize()]
MouseActionCursor=0
DynamicVariables=1

[DragBar.Image]
Meter=Image
ImageName=#@#Images\CoreAssets\#Set.IconStyle#Logo64.png
X=(5)r
Y=(5)r
W=(#Set.TitleBarH#-10)
H=(#Set.TitleBarH#-10)
LeftMouseUpAction=[!WriteKeyValue Variables Sec.Page "1" "#ROOTCONFIGPATH#Main\Home.ini"][!ActivateConfig "#CURRENTCONFIG#" "Home.ini"]
DynamicVariables=1

[WindowControl.Home]
Meter=String
X=(#Set.TitleBarH#/2*3)
Y=(#Set.TitleBarH#/2+1)
W=(#Set.TitleBarH#+1)
H=(#Set.TitleBarH#+1)
SolidColor=0,0,0,1
MeterStyle=Set.String:S
Text=[\xe88a]
Fontface=Material Icons Round
FontSize=(14)
FontColor=#Set.Text_Color#,50
StringAlign=CenterCenter
MouseOverAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables Sec.Page "1" "#ROOTCONFIGPATH#Main\Home.ini"][!ActivateConfig "#CURRENTCONFIG#" "Home.ini"]
InlineSetting=Shadow | 0 | 0 | 5 | #Set.Pri_Color#
Container=


[CommandBar.Shape]
Meter=Shape
X=(#Set.W#/2)
y=(#Set.WinB#/2+#Set.TitleBarH#/2)
Shape=Rectangle 0,0,(#Set.W#*0.4),(#Set.TitleBarH#-10),(10) | StrokeWidth 1 | Fill Color #Set.Ter_Color# | Stroke Color #Set.Text_Color#,50 | OffSet (-(#Set.W#*0.4)/2),(-(#Set.TitleBarH#-10)/2)
MouseActionCursor=0
LeftMouseUpAction=[!CommandMeasure Func "startOverlay('CommandBar')"]
DynamicVariables=1
[CommandBar.String]
Meter=String
X=r
Y=r
Text=#Sec.drag_bar_text#
FontSize=(8)
FontColor=#Set.SubText_Color#
MeterStyle=Set.String:s | DragBar.String:S1
StringAlign=CenterCenter
Container=

[WindowControl.Minimize]
Meter=String
X=(#Set.W#-#Set.TitleBarH#/2*5)
Y=(#Set.TitleBarH#/2+1)
W=(#Set.TitleBarH#+1)
H=(#Set.TitleBarH#+1)
SolidColor=0,0,0,1
MeterStyle=Set.String:S
Text=[\xe931]
Fontface=Material Icons Round
FontSize=(14)
FontColor=#Set.Text_Color#,50
StringAlign=CenterCenter
MouseOverAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure WindowHandler Minimize()]
InlineSetting=Shadow | 0 | 0 | 5 | #Set.Pri_Color#
Container=

[WindowControl.Maximize]
Meter=String
X=(#Set.W#-#Set.TitleBarH#/2*3)
Y=(#Set.TitleBarH#/2+1)
W=(#Set.TitleBarH#+1)
H=(#Set.TitleBarH#+1)
SolidColor=0,0,0,1
MeterStyle=Set.String:S | WindowControl.Maximize:#Set.Maximized#
Fontface=Material Icons Round
FontSize=(14)
FontColor=#Set.Text_Color#,50
StringAlign=CenterCenter
MouseOverAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure WindowHandler ToggleMaximize()]
InlineSetting=Shadow | 0 | 0 | 5 | #Set.Pri_Color#
Container=
[WindowControl.Maximize:0]
Text=[\xe5d0]
[WindowControl.Maximize:1]
Text=[\xe5d1]

[WindowControl.Esc]
Meter=String
X=(#Set.W#-#Set.TitleBarH#/2)
Y=(#Set.TitleBarH#/2+1)
W=(#Set.TitleBarH#+1)
H=(#Set.TitleBarH#+1)
SolidColor=0,0,0,1
Text=[\xe5cd]
MeterStyle=Set.String:S
Fontface=Material Icons Round
FontSize=(14)
FontColor=#Set.Text_Color#,50
StringAlign=CenterCenter
MouseOverAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# FontColor "#Set.Text_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue variables Sec.page 1 "#ROOTCONFIGPATH#Main\Home.ini"][!DeactivateConfig]
RightMOuseUpAction=[!commandMeasure Func "startDrop('Rainmeter', '#CURRENTSECTION#', 'MosaicShell')"]
InlineSetting=Shadow | 0 | 0 | 5 | #Set.Pri_Color#
Container=
