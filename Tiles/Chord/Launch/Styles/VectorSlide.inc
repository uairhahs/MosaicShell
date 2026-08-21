[Variables]
skinW=([#SCREENAREAWIDTH@#LaunchAt#])
skinH=(70*#Style.Scale#)
moveX=0@#LaunchAt#
moveY=#Style.yPos#@#LaunchAt#
anchorX=0
anchorY=(0)
TweenNode3=0
TweenNode4=0

[LoadActions]
ActionList1=Start0 | Wait #Style.VectorWait# | Start2 | Wait #Style.VectorTime# | Start3 | Start4 | Wait #Style.WaitTime# | Reset4 | Rewind2 | Wait #Style.VectorTime# | Rewind3 | Start1 | Wait #Style.Time1# | Deactivate
ActionList3=Wait #Style.Time1# | Action
Start2=[!CommandMeasure mTween "Start(2)"]
Start3=[!CommandMeasure mTween "Start(3)"]
Start4=[!CommandMeasure mTween "Start(4)"]
Rewind2=[!CommandMeasure mTween "Rewind(2)"]
Rewind3=[!CommandMeasure mTween "Rewind(3)"]
Reset4=[!CommandMeasure mTween "Reset(4)"]

[mTween]
Measure=Script
ScriptFile=#@#LuaTween/Main.lua
TweenGroup=Tweenable
Tween0=Single | Variable | TweenNode1 | 0 | 1 | #Style.Time1#
Tween1=Single | Variable | TweenNode1 | 1 | 0 | #Style.Time1#
Tween2=Single | Variable | TweenNode2 | 0 | 1 | #Style.Time1#
Tween3=Single | Variable | TweenNode3 | 0 | 1 | #Style.Time1#
Tween4=Single | Variable | TweenNode4 | 0 | 1 | #Style.WaitTime#
Optional0=Easing InOutExpo
Optional1=Easing InOutExpo
Optional2=Easing InOutExpo
Optional3=Easing InOutExpo
UpdateDivider=1


[Shape]
Meter=Shape
X=0
DynamicVariables=1
Shape=Rectangle 0,0,(([String:W]+32*#Style.Scale#+#Style.Pad#*3*#Style.Scale#)*#TweenNode1#),#skinH# | StrokeWidth 0 | Fill Color #Style.Pri_Color#

[ShapeC]
Meter=Shape
X=0
DynamicVariables=1
Group=Tweenable
Shape=Rectangle 0,0,(([String:W]+32*#Style.Scale#+#Style.Pad#*2*#Style.Scale#)*#TweenNode1#),#skinH# | StrokeWidth 0

[ShapeVector]
Meter=Shape
X=0
DynamicVariables=1
Group=Tweenable
Shape=Rectangle ((([String:W]+32*#Style.Scale#+#Style.Pad#*3*#Style.Scale#)*#TweenNode1#)-5*#Style.Scale#),0,(5*#Style.Scale#),(#skinH#) | StrokeWidth 0 | Fill Color #Style.Accent_Color#
Shape2=Rectangle (([String:W]+32*#Style.Scale#+#Style.Pad#*3*#Style.Scale#)*#TweenNode3#),0,(([String:W]+32*#Style.Scale#+#Style.Pad#*3*#Style.Scale#)*(#TweenNode2#-#Tweennode3#)),#skinH# | StrokeWidth 0 | Fill Color #Style.Accent_Color#
Shape3=Line (5*#Style.Scale#),(#skinH#-5*#Style.Scale#),(([String:W]+32*#Style.Scale#+#Style.Pad#*3*#Style.Scale#-20*#Style.Scale#)*#TweenNode4#),(#skinH#-5*#Style.Scale#) | StrokeWidth 2 | Stroke Color #Style.Prog_Color#,(255*#TweenNode4#)

[Image]
MEter=Image
X=(#Style.Pad#*#Style.Scale#)
Y=(#skinH#/2-32/2*#Style.Scale#)
W=(32*#Style.Scale#)
H=(32*#Style.Scale#)
DynamicVariables=1
ImageName=#SKINSPATH#..\CoreData\Chord\IconCache\#Sec.Icon#.png
Greyscale=#Greyscale#
Container=ShapeC

[String]
Meter=String
FontSize=(#Style.FontSize#*#Style.Scale#)
X=(20*#Style.Scale#)R
Y=(32/2*#Style.Scale#)r
FontColor=#FontColor#
FontFace=#GlobalFont#
Text=#Sec.Name#
AntiAlias=1
StringAlign=LeftCenter
Container=ShapeC
DynamicVariables=1
InlineSetting=Shadow | 0 | 0 | #Blur# | #Glowcolor#,#GlowOpacity#
