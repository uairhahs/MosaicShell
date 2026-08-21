; ---------------------------------------------------------------------------- ;
;                                   Functions                                  ;
; ---------------------------------------------------------------------------- ;

[m.volbar_padding]
Measure=Calc
Formula=#Layout.box_padding#

[m.volbar_divide_margin]
Measure=Calc
Formula=#Layout.volbar_divide_margin#

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

; --------------- MeterStyle for elements of generated section --------------- ;

[Item.Background.Shape:S]
X=#Layout.P#
Y=((#IconSize#*#Scale#+#Layout.box_padding#*2)/2+#Layout.volbar_divide_margin#*2)r
Shape=Rectangle 0,0,(#Layout.left_box_width#),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 2 | Fill Color 0,0,0,0 | Stroke Color #shadow_color# | OffSet 0,1
Shape2=Rectangle 0,0,(#Layout.left_box_width#),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #foreground_color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #foreground_hover_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #foreground_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]

[Item.Image:S]
X=(#Layout.box_padding#*2)r
Y=#Layout.box_padding#r

[Item.Name.String:S]
X=#Layout.box_padding#R
Y=(#IconSize#*#Scale#/2)r
W=(#Layout.left_box_width#-#Layout.box_padding#*5-#IconSize#*#Scale#)
StringAlign=LeftCenter

[Item.Vol.String:S]
X=(#Layout.P#+#Layout.volbar_divide_margin#*2+#Layout.left_box_width#+#Layout.box_padding#*2)
Y=r
W=(75*#scale#)
FontSize=(10*#scale#)
InlineSetting=Shadow | 0 | 2 | 5 | #shadow_color#
StringAlign=LeftCenter

; ------------------------ MeterStyles for volume bars ----------------------- ;
[Item.Shape:S]
X=(#Layout.P#+#Layout.volbar_divide_margin#*2+#Layout.left_box_width#)
Y=r
Shape=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 2 | Fill Color 0,0,0,0 | Stroke Color #shadow_color# | OffSet 0,2
Shape2=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 0 | Extend Fill
Shape3=Rectangle #Layout.box_padding#,(#Layout.box_padding#-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),((#Layout.volbar_width#-#Layout.box_padding#*2)*[AppVolPer#CURRENTSECTION#:]/100),(#IconSize#*#Scale#),(#IconSize#*#Scale#/2) | StrokeWidth 0 | Fill Color #bar_color# | Scale ([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1),([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1)
Fill=Fill Color #foreground_color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #foreground_hover_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #foreground_color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Int=1

[Item.Shape:S.2]
X=(#Layout.volbar_divide_margin#*2)R
Y=r
Shape=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#/2-#Layout.volbar_divide_margin#),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 2 | Fill Color 0,0,0,0 | Stroke Color #shadow_color# | OffSet 0,2
Shape2=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#/2-#Layout.volbar_divide_margin#),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 0 | Extend Fill
Shape3=Rectangle #Layout.box_padding#,(#Layout.box_padding#-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),((#Layout.volbar_width#/2-#Layout.volbar_divide_margin#-#Layout.box_padding#*2)*[AppVolPer#CURRENTSECTION#:]/100),(#IconSize#*#Scale#),(#IconSize#*#Scale#/2) | StrokeWidth 0 | Fill Color #bar_color# | Scale ([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1),([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1)
Int=2

[Item.Shape:S.3]
X=(#Layout.volbar_divide_margin#*2)R
Y=r
Shape=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 2 | Fill Color 0,0,0,0 | Stroke Color #shadow_color# | OffSet 0,2
Shape2=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 0 | Extend Fill
Shape3=Rectangle #Layout.box_padding#,(#Layout.box_padding#-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),((#Layout.volbar_width#/3-#Layout.volbar_divide_margin#*2-#Layout.box_padding#*2)*[AppVolPer#CURRENTSECTION#:]/100),(#IconSize#*#Scale#),(#IconSize#*#Scale#/2) | StrokeWidth 0 | Fill Color #bar_color# | Scale ([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1),([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1)
Int=3

[Item.Shape:S.4]
X=(#Layout.volbar_divide_margin#*2)R
Y=r
Shape=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 2 | Fill Color 0,0,0,0 | Stroke Color #shadow_color# | OffSet 0,2
Shape2=Rectangle 0,(-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),(#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3),(#IconSize#*#Scale#+#Layout.box_padding#*2),(#IconSize#*#Scale#/2+#Layout.box_padding#) | StrokeWidth 0 | Extend Fill
Shape3=Rectangle #Layout.box_padding#,(#Layout.box_padding#-(#IconSize#*#Scale#+#Layout.box_padding#*2)/2),((#Layout.volbar_width#/4-#Layout.volbar_divide_margin#*3-#Layout.box_padding#*2)*[AppVolPer#CURRENTSECTION#:]/100),(#IconSize#*#Scale#),(#IconSize#*#Scale#/2) | StrokeWidth 0 | Fill Color #bar_color# | Scale ([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1),([AppVolPer#CURRENTSECTION#:] <= -1 ? 0 : 1)
Int=4

[Item.Background.Shape:S]

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Background.Shape]
X=(#Stroke#*#scale#/2)
Y=(#Stroke#*#scale#/2)
Shape=Rectangle 0,0,#W#,#H#,#R# | StrokeWidth (#Stroke#*#scale#) | Fill Color #background_color#,#Alpha# | Stroke Color #foreground_hover_color#

; ------------------------------ Header section ------------------------------ ;

[Header.String]
Meter=String
X=#Layout.P#
Y=#Layout.P#
Text=#header_text#
FontSize=(#scale#*15)
FontWeight=500
MeterStyle=String:S

[Header.DeviceIcon.String]
Meter=String
MeterStyle=String:S
X=(#W#-24*#Scale#)
Y=(24*#Scale#)
FontFace=Material Icons Round
Text=[\xf003]
FontSize=(16*#scale#)
DynamicVariables=1
Greyscale=1
ImageTint=#FontColor#
StringAlign=Right
LeftMouseDownAction=[!SKinCustomMenu]

--------------------------------- FirstBar --------------------------------- ;

[ppVol0]
Meter=Shape
X=#Layout.P#
Y=(70*#scale#)
MeterStyle=Item.Background.Shape:S

[pVol0]
Meter=Image
ImageName=#@#Icons\Png\Material#process.volume_icon_index#.png
ImageTint=#FontColor#
MeterStyle=Item.Image:S
Group=Volumebar
[Vol0]
Meter=String
MeterStyle=String:S | Item.Name.String:S
Group=Volumebar
[0]
Meter=Shape
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
Y=(#Layout.P#+(#IconSize#*#Scale#+#Layout.box_padding#*2)/2)r
