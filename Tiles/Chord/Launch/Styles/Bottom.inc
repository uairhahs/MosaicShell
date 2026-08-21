[Variables]
skinW=([#SCREENAREAWIDTH@#LaunchAt#])
skinH=(60*#Style.Scale#)
moveX=0@#LaunchAt#
moveY=([#SCREENAREAHEIGHT@#LaunchAt#])
anchorX=0
anchorY=(60*#Style.Scale#)

[LoadActions]
ActionList1=Start0 | Wait #Style.Time1# | Start1 | Wait #Style.Time1# | Deactivate
ActionList3=Wait #Style.Time1# | Action

[mTween]
Measure=Script
ScriptFile=#@#LuaTween/Main.lua
TweenGroup=Tweenable
Tween0=Single | Variable | TweenNode1 | 0 | 1 | #Style.Time1#
Tween1=Single | Variable | TweenNode1 | 1 | 0 | #Style.Time1#
Optional0=Easing InOutExpo
Optional1=Easing InOutExpo
UpdateDivider=1

[Shape]
Meter=Shape
X=(#skinW#/2-[Shape:W]/2)
; Y=0
; X=(#SCREENAREAWIDTH#/2-([String:W]+32+#Style.Pad#*2)/2)
Y=((60-60*#TweenNode1#)*#Style.Scale#)
DynamicVariables=1
Shape=Rectangle 0,0,([String:W]+32*#Style.Scale#+#Style.Pad#*2*#Style.Scale#),(50*#Style.Scale#),(10*#Style.Scale#) | StrokeWidth 0 | Fill Color #MainCColor#,(#MainCOpacity#-#MainCOpacity#*#TweenNode2#)

[Image]
MEter=Image
X=(#Style.Pad#*#Style.Scale#)r
Y=((50/2-32/2)*#Style.Scale#)r
W=(32*#Style.Scale#)
H=(32*#Style.Scale#)
ImageAlpha=(255*(1-#TweenNode2#))
DynamicVariables=1
ImageName=#SKINSPATH#..\CoreData\Chord\IconCache\#Sec.Icon#.png
Greyscale=#Greyscale#

[String]
Meter=String
FontSize=(#Style.FontSize#*#Style.Scale#)
X=(5*#Style.Scale#)R
Y=(32/2*#Style.Scale#)r
FontColor=#FontColor#,(255*(1-#TweenNode2#))
FontFace=#GlobalFont#
Text=#Sec.Name#
AntiAlias=1
StringAlign=LeftCenter
DynamicVariables=1
InlineSetting=Shadow | 0 | 0 | #Blur# | #Glowcolor#,#GlowOpacity#
