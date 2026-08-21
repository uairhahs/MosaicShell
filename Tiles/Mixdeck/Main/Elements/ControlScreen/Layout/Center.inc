; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[m.volbar_divide_margin]
Measure=Calc
Formula=#Layout.volbar_divide_margin#

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

; --------------- MeterStyle for elements of generated section --------------- ;

[Item.Image:S]
X=(25*#scale#)
Y=((30+#line_pad#)*#scale#)r
GreyScale=#greyscale_icons#

[Item.Name.String:S]
X=(22*#Scale#)R
Y=(#IconSize#/2*#scale#)r
W=((#name_width#-16-44-10)*#scale#)
StringAlign=LeftCenter

[Item.Vol.String:S]
X=(#W#-22*#scale#)
Y=r
W=(75*#scale#)

[String:S]
InlineSetting=Shadow | 5 | 5 | 5 | 0,0,0,75

; ------------------------ MeterStyles for volume bars ----------------------- ;

; ------------------------ MeterStyles for volume bars ----------------------- ;
[Item.Shape:S]
Shape=Line 0,0,#Layout.volbar_width#,0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend BackgroundStrokeColor
Shape2=Line 0,0,#Layout.volbar_width#,0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend ForegroundStrokeColor | Scale ([AppVolPer#CURRENTSECTION#:]/100 > 0 ? [AppVolPer#CURRENTSECTION#:]/100 : 0), 1, 0, 0
Shape3=Line 0,0,#Layout.volbar_width#,0 | StrokeWidth (40*#scale#) | Fill Color 0,0,0,1 | Stroke Color 0,0,0,1
Shape4=Ellipse (#Layout.volbar_width#*[AppVolPer#CURRENTSECTION#:]/100),0,(#Layout.circ_size#) | StrokeWidth 0 | Extend NodeExtends
Shape5=Line 0,3,#Layout.volbar_width#,3 | StrokeWidth (2*#Scale#*#VolumePeaks#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend VolumePeakStrokeColor | Scale (Clamp(([AppVolPeak#CURRENTSECTION#:] > 0 ? [AppVolPeak#CURRENTSECTION#:] : 0), 0, 1)), 1, 0, 0
Hidden=([AppVolPer#CURRENTSECTION#:] <= -1 ? 1 : 0)
Int=1

[Item.Shape:S.2]
X=((#Layout.volbar_width#/2-#Layout.volbar_divide_margin#)+#Layout.volbar_divide_margin#*2)r
Shape=Line 0,0,(#Layout.volbar_width#/2-#Layout.volbar_divide_margin#),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend BackgroundStrokeColor
Shape2=Line 0,0,(#Layout.volbar_width#/2-#Layout.volbar_divide_margin#),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend ForegroundStrokeColor | Scale ([AppVolPer#CURRENTSECTION#:]/100 > 0 ? [AppVolPer#CURRENTSECTION#:]/100 : 0), 1, 0, 0
Shape3=Line 0,0,(#Layout.volbar_width#/2-#Layout.volbar_divide_margin#),0 | StrokeWidth (40*#scale#) | Fill Color 0,0,0,1 | Stroke Color 0,0,0,1
Shape4=Ellipse ((#Layout.volbar_width#/2-#Layout.volbar_divide_margin#)*[AppVolPer#CURRENTSECTION#:]/100),0,(#Layout.circ_size#) | StrokeWidth 0 | Extend NodeExtends
Shape5=Line 0,3,(#Layout.volbar_width#/2-#Layout.volbar_divide_margin#),3 | StrokeWidth (2*#Scale#*#VolumePeaks#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend VolumePeakStrokeColor | Scale (Clamp(([AppVolPeak#CURRENTSECTION#:] > 0 ? [AppVolPeak#CURRENTSECTION#:] : 0), 0, 1)), 1, 0, 0
Int=2

[Item.Shape:S.3]
X=((#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2)+#Layout.volbar_divide_margin#*2)r
Shape=Line 0,0,(#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend BackgroundStrokeColor
Shape2=Line 0,0,(#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend ForegroundStrokeColor | Scale ([AppVolPer#CURRENTSECTION#:]/100 > 0 ? [AppVolPer#CURRENTSECTION#:]/100 : 0), 1, 0, 0
Shape3=Line 0,0,(#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2),0 | StrokeWidth (40*#scale#) | Fill Color 0,0,0,1 | Stroke Color 0,0,0,1
Shape4=Ellipse ((#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2)*[AppVolPer#CURRENTSECTION#:]/100),0,(#Layout.circ_size#) | StrokeWidth 0 | Extend NodeExtends
Shape5=Line 0,3,(#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2),3 | StrokeWidth (2*#Scale#*#VolumePeaks#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend VolumePeakStrokeColor | Scale (Clamp(([AppVolPeak#CURRENTSECTION#:] > 0 ? [AppVolPeak#CURRENTSECTION#:] : 0), 0, 1)), 1, 0, 0
Int=3

[Item.Shape:S.4]
X=((#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3)+#Layout.volbar_divide_margin#*2)r
Shape=Line 0,0,(#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend BackgroundStrokeColor
Shape2=Line 0,0,(#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3),0 | StrokeWidth (2*#Scale#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend ForegroundStrokeColor | Scale ([AppVolPer#CURRENTSECTION#:]/100 > 0 ? [AppVolPer#CURRENTSECTION#:]/100 : 0), 1, 0, 0
Shape3=Line 0,0,(#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3),0 | StrokeWidth (40*#scale#) | Fill Color 0,0,0,1 | Stroke Color 0,0,0,1
Shape4=Ellipse ((#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3)*[AppVolPer#CURRENTSECTION#:]/100),0,(#Layout.circ_size#) | StrokeWidth 0 | Extend NodeExtends
Shape5=Line 0,3,(#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3),3 | StrokeWidth (2*#Scale#*#VolumePeaks#) | Fill Color 0,0,0,1 | StrokeStartCap Round | StrokeEndCap Round | Extend VolumePeakStrokeColor | Scale (Clamp(([AppVolPeak#CURRENTSECTION#:] > 0 ? [AppVolPeak#CURRENTSECTION#:] : 0), 0, 1)), 1, 0, 0
Int=4

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

; ------------------------------ Header section ------------------------------ ;

[Header.String]
Meter=String
X=(25*#Scale#)
Y=(20*#Scale#)
Text=#header_text#
FontSize=(#scale#*15)
FontWeight=500
MeterStyle=String:S

[Header.DeviceIcon.String]
Meter=String
MeterStyle=String:S
X=(#W#-24*#Scale#)
Y=(24*#Scale#)
FontFace=Segoe Fluent Icons
Text=[\xe7f3]
FontSize=(16*#scale#)
DynamicVariables=1
Greyscale=1
ImageTint=#FontColor#
StringAlign=Right
LeftMouseDownAction=[!SKinCustomMenu]

--------------------------------- FirstBar --------------------------------- ;

[pVol0]
Meter=Image
X=(25*#scale#)
Y=(70*#scale#)
ImageName=#@#Icons\Png\ColoredFluent#process.volume_icon_index#.png
MeterStyle=Item.Image:S
Group=Volumebar
[Vol0]
Meter=String
MeterStyle=String:S | Item.Name.String:S
Group=Volumebar
[0]
Meter=Shape
Shape5=
MeterStyle=Item.Shape:S
Group=Volumebar
[VolPer0]
Meter=String
MeterStyle=String:S | Item.Vol.String:S
Group=Volumebar

; ---------------------------------------------------------------------------- ;
;                           Generated mixer contents                           ;
; ---------------------------------------------------------------------------- ;

@includeGenerated=#ROOTCONFIGPATH#Main\Elements\ControlScreen\Cache\MixerContent.inc

[Anchor]
Meter=String
X=0
Y=(22*#scale#)R
