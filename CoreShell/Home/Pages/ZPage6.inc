﻿
[B]
Shape2=Rectangle 0,0, #Set.W#, #Set.H#, #Set.WinR# | StrokeWidth 0 | Fill RadialGradient This
This=(-#Set.W#/2),(#set.H#/2),0,0,(#Set.H#/2),(#Set.H#/2) | #Set.Pri_Color#,50 ; 1 | #Set.Accent_Color#,50 ; 0

; ---------------------------------- Limited --------------------------------- ;
; [Limited.Shape]
; Meter=Shape
; X=(#Set.P#+10*[Set.S])
; Y=(#Set.H#*0.25)
; Shape=Rectangle 0,0,(500*[Set.S]),(50*[SEt.S]),(#Set.R#/2) | StrokeWidth 2 | Stroke LinearGradient CoreGrad | Fill Color #Set.Ter_Color#
; CoreGrad=45 | #Set.Accent_Color# ; 1 | #Set.Accent_Color_2# ;0
; DynamicVariables=1
; Container=#Sec.Container#
; LeftMouseUpAction=["https://forms.gle/pFkV8sYqPfAkSFAF8"]
; [Limited.String]
; Meter=String
; Text=❤️ Take a survey to help improve the experience!
; X=(10*[Set.S])r
; Y=(25*[Set.S])r
; FontWeight=600
; StringAlign=LeftCenter
; MeterStyle=Set.String:S
; ------------------------------------- - ------------------------------------ ;
[Title.Text1]
Meter=String
MeterStyle=Set.String:S
Text=Config hub
X=(#Set.P#+10*[Set.S])
Y=(#Set.H#*0.35)
Group=Scroll
FontSize=(20*[Set.S])
FontFace=#Set.Font#
StringAlign=LeftCenter
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 2.0
[Title.Text2]
Meter=String
MeterStyle=Set.String:S
Text=MosaicShell
X=(-10*[Set.S])r
Y=(60*[Set.s])r
FontSize=(60*[Set.S])
FontFace=#Set.Font#
FontWeight=(650)
StringAlign=LeftCenter
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0

[Text.1]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(78*[Set.S])r
Text=A set of tools and widgets designed to give you a better desktop experience
[Text.2]
Meter=String
MeterStyle=Set.String:S
X=r
Y=(10*[Set.S])R
Text=With this configuration hub, you can easily access settings, updates, and new releases
; ---------------------------------------------------------------------------- ;
;                                     Right                                    ;
; ---------------------------------------------------------------------------- ;

[ConvertedShape]
Meter=Shape
X=(#Set.W#-#Set.P#-295*[Set.S])
Y=(#Set.H#/2-(434/2*[Set.S]))
Container=BC
Shape=Path Path1 | stroke color 140, 140, 140 | StrokeWidth 0 | Scale [Set.S],[Set.S],0,0 | Fill LinearGradient This
This=GradientColor | 180 | #Set.Accent_Color#,51 ; 0.0 | #Set.Accent_Color_2#,51 ; 2.0
Path1=297.1, 94.9 | CurveTo 297.1, 116.1, 303.0, 100.7, 303.0, 110.2 | LineTo 227.5, 185.8 | LineTo 286.3, 244.6 | CurveTo 286.3, 265.8, 292.1, 250.4, 292.1, 259.9 | LineTo 116.1, 436.0 | CurveTo 94.9, 436.0, 110.2, 441.8, 100.7, 441.8 | LineTo 10.6, 351.7 | CurveTo 10.6, 330.4, 4.7, 345.8, 4.7, 336.3 | LineTo 80.2, 260.8 | LineTo 21.4, 202.0 | CurveTo 21.4, 180.7, 15.6, 196.1, 15.6, 186.6 | LineTo 191.6, 10.6 | CurveTo 212.8, 10.6, 197.5, 4.7, 207.0, 4.7 | ClosePath 1
DynamicVariables=1

[Logo]
Meter=Image
ImageName=#@#Images\CoreAssets\#Set.IconStyle#Logo256.png
X=(#Set.W#-#Set.P#-((256)*[Set.S]))
Y=(70*[Set.S])r
W=(256*[Set.S])
Container=BC
DynamicVariables=1
