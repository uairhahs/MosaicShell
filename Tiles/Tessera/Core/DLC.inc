; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

; ---------------------------------------------------------------------------- ;
;                                     Style                                    ;
; ---------------------------------------------------------------------------- ;
[Item.Shape:S]
X=(20*[Set.S])
Y=(40*[SEt.S])r
Shape=Rectangle 0,0,([#Sec.Container#:W]-#Set.P#),(50*[SEt.S]),(2*[Set.S]) | StrokeWidth 0 | Extend Fill
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=#Sec.Container#
[Item.StringIcon:S]
X=(50*[Set.S]/2)r
Y=(50*[Set.S]/2)r
FontSize=((16*[SEt.S]))
StringAlign=leftCenter
FontFace=Material Icons Round
Container=#Sec.Container#
[Item.String:S]
X=(#Set.P#/2)R
Y=r
W=([#Sec.Container#:W]-#Set.P#-85*[SEt.S])
ClipString=2
StringAlign=LeftCenter
Container=#Sec.Container#
[Item.Button.Shape:S]
X=([#Sec.Container#:W]-#Set.P#-30*[SEt.S])
Y=(-15*[Set.S])r
Shape=Rectangle 0,0,(30*[Set.S]),(30*[Set.S]),(5*[Set.S]) | StrokeWidth 0 | Extend Stroke
Stroke=Fill Color #Set.Positive_color#,100
MouseOverAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color #Set.Positive_color#,200"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Stroke "Fill Color #Set.Positive_color#,100"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
Container=#Sec.Container#
[Item.Button.StringIcon:S]
X=(30*[Set.S]/2)r
Y=(30*[Set.S]/2)r
StringAlign=CenterCEnter
Text=[\xe5ce]
FontFace=Material Icons Round
Container=#Sec.Container#

[Product.Shape:S]
X=([ContentContainer:W]/2-20*[Set.S])r
Y=(-([ContentContainer:W]/2-20*[Set.S]-#Set.P#)/1.625-#Set.P#)r
Shape=Rectangle 0,0,([ContentContainer:W]/2-20*[Set.S]),(([ContentContainer:W]/2-20*[Set.S])*0.8),(#Set.R#) | StrokeWidth (2*[Set.S]) | Extend Fill | Stroke LinearGradient CoreGrad
Fill=Fill Color #Set.Sec_Color#
CoreGrad=45 | #Set.Accent_Color# ; 1 | #Set.Accent_Color_2# ;0
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=["https://ko-fi.com/mosaicshell/shop"]
DynamicVariables=1
Container=#Sec.Container#
[Product.Image:S]
X=(#Set.P#/2)r
Y=(#Set.P#/2)r
W=([ContentContainer:W]/2-20*[Set.S]-#Set.P#)
ImageName=#SKINSPATH##Skin.Name#\@Resources\Images\DLC\[&Func:trim('#CURRENTSECTION#', '.Image:S', '')].png
MaskImageName=#SKINSPATH##Skin.Name#\Core\Layout\Mask.png
DynamicVariables=1
Container=#Sec.Container#
[Product.String:S]
X=r
Y=(#set.P#/2)R
W=([ContentContainer:W]/2-#Set.P#*2.5)
Text=[&Func:trim('#CURRENTSECTION#', '.String:S', '')] for Tessera
FontWeight=650
FontSize=(20*[Set.S])
ClipString=2
InlinePattern= for Tessera
InlineSetting=Size | (10*[Set.S])
InlinePattern2= for Tessera
InlineSetting2=Weight | 400
Container=#Sec.Container#

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Bg.Img]
Meter=Image
X=0
Y=#Scroll#
W=[ContentContainer:W]
H=(#Set.H#*0.6)
DynamicVariables=1
ImageAlpha=230
ImageName="#SKINSPATH##SKin.Name#\@Resources\Images\DLC\SleekHero.png"
MaskImageName="#SKINSPATH##SKin.Name#\@Resources\Images\DLC\Mask.png"
PreserveAspectRatio=2
Container=ContentContainer

[Header]
Meter=String
X=(20*[Set.S])
Y=(30*[Set.S]+#Scroll#)
DynamicVariables=1
Text=#SKIN.NAME# Downloadable contents
FontWeight=300
FontSize=((12*[Set.S]))
MeterStyle=Set.String:S


[Spotlight.Title.String]
Meter=String
Text="Sleek"
X=r
Y=(#Set.P#*1.5)r
FontSize=((80*[Set.S]))
InlineSetting=GradientColor | 180 | #Set.Accent_Color# ; 0.0 | #Set.Accent_Color_2# ; 1.0
FontWEight=800
MeterStyle=Set.String:S | SEc.String:S
[Spotlight.Des.String]
Meter=String
Text=A flat-designed set of modules, inspired by multiple unix rices.
X=r
Y=R
W=(#SEt.W#-#set.L#-#set.P#*2)
ClipString=2
MeterStyle=Set.String:S | SEc.String:S
[Spotlight.Price.String]
Meter=String
Text=Get it for $0+ pay what you want!#CRLF#But a payment of $3 or more will go toward ongoing support and future updates :)
X=r
Y=(#Set.P#/2)R
W=(#SEt.W#-#set.L#-#set.P#*2-400*[Set.S])
ClipString=2
FontWeight=300
InlinePattern=\$0\+ pay what you want|\$3 or more
InlineSetting=Color | #Set.Accent_Color_2#
InlinePattern2=\$0\+ pay what you want|\$3 or more
InlineSetting2=Weight | 500
MeterStyle=Set.String:S | SEc.String:S
[Spotlight.Button1.Shape]
Meter=Shape
X=r
Y=(#set.P#/2)R
OverColor=#Set.Text_Color#,150
LeaveColor=#Set.Text_Color#,50
DynamicVariables=1
Shape=Rectangle 0,0,200,40,#set.r# | StrokeWidth 0 | Extend Fill | Scale [Set.S],[Set.S],0,0
Fill=Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')],#Set.Alpha#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'OverColor')],#Set.Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar('#CURRENTSECTION#', 'LeaveColor')],#Set.Alpha#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=["https://ko-fi.com/mosaicshell/shop"]
Container=#Sec.Container#
[Spotlight.Button1.String]
Meter=String
X=(200*[Set.S]/2)r
Y=(40*[Set.S]/2)r
StringAlign=CenterCenter
Text=See details
MeterStyle=Set.String:S | SEc.String:S

[Div]
Meter=Shape
X=(20*[Set.S])
Y=(#Set.H#*0.6+#Set.P#/2+#Scroll#)
MeterStyle=Set.DIv:S

[Vekl.Shape:S]
Meter=Shape
X=(20*[Set.S])
Y=(40*[SEt.S])r
MeterStyle=Product.Shape:S
[Vekl.Image:S]
Meter=Image
MeterStyle=Product.Image:S
[Vekl.String:S]
Meter=String
MeterStyle=Set.String:S | Product.String:S

[Sleek.Shape:S]
Meter=Shape
MeterStyle=Product.Shape:S
[Sleek.Image:S]
Meter=Image
MeterStyle=Product.Image:S
[Sleek.String:S]
Meter=String
MeterStyle=Set.String:S | Product.String:S

[Item1.Shape]
Meter=Shape
Y=(100*[Set.S])r
MeterStyle=Item.Shape:S
[Item1.StringIcon]
Meter=String
Text=[\xe65f]
Fontcolor=FECC50
MeterStyle=Set.String:S | Item.StringIcon:S
[Item1.String]
Meter=String
Text=Get deluxe exclusive contents to work with the base module!
MeterStyle=Set.String:S | Item.String:S
