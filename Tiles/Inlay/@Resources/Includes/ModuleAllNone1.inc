[Info.Shape]
Meter=shape
X=(25*#Scale#)
Y=(25*#scale#)r
Shape=Rectangle 0,0,(#W#-50*#Scale#),(130*#Scale#),(#R#*#Scale#) | StrokeWidth 2 | Extend This
This=Stroke color #Border#,#Alpha# | Fill Color #Pri_color#,#Alpha#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Stroke color #BoxBorder# | Fill Color #BoxColor#,#Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Stroke color #Border#,#Alpha# | Fill Color #Pri_color#,#Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!DeactivateConfig "#MosaicShell\Main"][!Delay 100][!WriteKeyvalue Variables Skin.Name #ROOTCONFIG# "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!WriteKeyvalue Variables Skin.Set_Page Layout "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!ActivateConfig "#MosaicShell\Main" "Settings.ini"]
[Info.Container]
Meter=SHape
X=r
Y=r
Shape=Rectangle 0,0,(#W#-50*#Scale#),(130*#Scale#),(#R#*#Scale#) | StrokeWidth 0
[Info.Image]
Meter=Image
ImageName=#@#Images\NoModule.png
W=(#W#-50*#Scale#)
H=(130*#Scale#)
PreserveAspectRatio=2
Container=Info.Container
[Info.String]
Meter=String
Text=Welcome to Inlay#CRLF#Inlay comes with numerous different modules for you to use in your new start menu. Click here to select a layout!
StringAlign=LeftCenter
X=(25*#Scale#)r
Y=(130/2*#Scale#)r
W=(300*#Scale#)
ClipString=2
InlinePattern=.*#CRLF#
InlineSetting=Weight | 650
InlinePattern2=.*#CRLF#
InlineSetting2=Size | (16*#Scale#)
MeterStyle=regularText

[Info2.Shape]
Meter=shape
X=(25*#Scale#)
Y=((130/2+25)*#Scale#)r
Shape=Rectangle 0,0,(#W#-50*#Scale#),(130*#Scale#),(#R#*#Scale#) | StrokeWidth 2 | Extend This
This=Stroke color #Border#,#Alpha# | Fill Color #Pri_color#,#Alpha#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Stroke color #BoxBorder# | Fill Color #BoxColor#,#Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Stroke color #Border#,#Alpha# | Fill Color #Pri_color#,#Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!DeactivateConfig "#MosaicShell\Main"][!Delay 100][!WriteKeyvalue Variables Skin.Name #ROOTCONFIG# "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!WriteKeyvalue Variables Skin.Set_Page Appearance "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!ActivateConfig "#MosaicShell\Main" "Settings.ini"]
[Info2.Container]
Meter=SHape
X=r
Y=r
Shape=Rectangle 0,0,(#W#-50*#Scale#),(130*#Scale#),(#R#*#Scale#) | StrokeWidth 0
[Info2.Image]
Meter=Image
ImageName=#@#Images\NoModule2.png
W=(#W#-50*#Scale#)
H=(130*#Scale#)
PreserveAspectRatio=2
Container=Info2.Container
[Info2.String]
Meter=String
Text=YOUR start menu#CRLF#Inlay is extremely customizable (unlike it's counterpart), change the position, dimension, colors and more!
StringAlign=LeftCenter
X=(225*#Scale#)r
Y=(130/2*#Scale#)r
W=(300*#Scale#)
ClipString=2
InlinePattern=.*#CRLF#
InlineSetting=Weight | 650
InlinePattern2=.*#CRLF#
InlineSetting2=Size | (16*#Scale#)
MeterStyle=regularText

[Info3.Shape]
Meter=shape
X=(25*#Scale#)
Y=((130/2+25)*#Scale#)r
Shape=Rectangle 0,0,(#W#-50*#Scale#),(60*#Scale#),(#R#*#Scale#) | StrokeWidth 2 | Extend This
This=Stroke color #Border#,#Alpha# | Fill Color #Pri_color#,#Alpha#
MouseOverAction=[!SetOption #CURRENTSECTION# This "Stroke color #BoxBorder# | Fill Color #BoxColor#,#Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Stroke color #Border#,#Alpha# | Fill Color #Pri_color#,#Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!DeactivateConfig "#MosaicShell\Main"][!Delay 100][!WriteKeyvalue Variables Skin.Name #ROOTCONFIG# "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!WriteKeyvalue Variables Skin.Set_Page Position "#SKINSPATH##MosaicShell\@Resources\CacheVars\Configurator.inc"][!ActivateConfig "#MosaicShell\Main" "Settings.ini"]
[Info3.String]
Meter=String
Text=Inlay's on the wrong monitor? Change the monitor index in the position settings!
StringAlign=LeftCenter
X=(25*#Scale#)r
Y=(60/2*#Scale#)r
W=(#W#-100*#Scale#)
ClipString=2
MeterStyle=regularText


[InfoAnchor]
Meter=String
Y=((60/2+25)*#Scale#)r
DynamicVariables=1
