[Variables]
skinW=([#SCREENAREAWIDTH@#LaunchAt#])
skinH=(300*#Style.Scale#)
moveX=50%@#LaunchAt#
moveY=50%@#LaunchAt#
anchorX=50%
anchorY=50%

[mTween]
Measure=Script
ScriptFile=#@#LuaTween/Main.lua
TweenGroup=Tweenable
Tween0=Single | Variable | TweenNode1 | 0 | 1 | #Style.Time1#
Tween1=Single | Variable | TweenNode2 | 0 | 1 | #Style.Time2#
Optional0=Easing InOutExpo
Optional1=Easing InOutExpo
UpdateDivider=1

[Image2]
MEter=Image
X=(#SkinW#/2-#SkinH#*#TweenNode1#/2-[String:W]/2)
Y=(#SkinH#/2-#SkinH#*#TweenNode1#/2)
W=(#SkinH#*#TweenNode1#)
H=(#SkinH#*#TweenNode1#)
ImageAlpha=(50*(1-#TweenNode2#))
DynamicVariables=1
TransformationMatrix=(Cos(Rad(#TweenNode1#*360)));(-Sin(Rad(#TweenNode1#*360)));(Sin(Rad(#TweenNode1#*360)));(Cos(Rad(#TweenNode1#*360)));(([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])-([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])*Cos(Rad(#TweenNode1#*360))-([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])*Sin(Rad(#TweenNode1#*360)));(([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])+([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])*Sin(Rad(#TweenNode1#*360))-([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])*Cos(Rad(#TweenNode1#*360)))
ImageName=#SKINSPATH#..\CoreData\Keylaunch\IconCache\#Sec.Icon#.png
Greyscale=#Greyscale#

[Image]
MEter=Image
X=(#SkinW#/2-256*#Style.Scale#*#TweenNode1#/2-[String:W]/2)
Y=(#SkinH#/2-256*#Style.Scale#*#TweenNode1#/2)
W=(256*#Style.Scale#*#TweenNode1#)
H=(256*#Style.Scale#*#TweenNode1#)
ImageAlpha=(255*(1-#TweenNode2#))
DynamicVariables=1
TransformationMatrix=(Cos(Rad(#TweenNode1#*360)));(-Sin(Rad(#TweenNode1#*360)));(Sin(Rad(#TweenNode1#*360)));(Cos(Rad(#TweenNode1#*360)));(([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])-([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])*Cos(Rad(#TweenNode1#*360))-([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])*Sin(Rad(#TweenNode1#*360)));(([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])+([#CURRENTSECTION#:W]*0.5+[#CURRENTSECTION#:X])*Sin(Rad(#TweenNode1#*360))-([#CURRENTSECTION#:H]*0.5+[#CURRENTSECTION#:Y])*Cos(Rad(#TweenNode1#*360)))
ImageName=#SKINSPATH#..\CoreData\Keylaunch\IconCache\#Sec.Icon#.png
Greyscale=#Greyscale#


[String]
Meter=String
FontSize=(#Style.FontSize#*#Style.Scale#)
X=(256*#Style.Scale#+#CircSize#*#Style.Scale#-(128-10)*#Style.Scale#-#SkinH#*(1-#TweenNode1#))r
Y=(#SkinH#/2)
FontColor=#FontColor#,(255*(#TweenNode1#-0.5)/0.5-255*#TweenNode2#)
FontFace=#GlobalFont#
Text=#Sec.Name#
AntiAlias=1
StringAlign=LeftCenter
DynamicVariables=1
InlineSetting=Shadow | 0 | 0 | #Blur# | #Glowcolor#,#GlowOpacity#
