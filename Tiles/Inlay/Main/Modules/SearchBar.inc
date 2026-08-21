; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Variables]
SearchBarHeight=(40+#Pad#*2)

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Searchbar.Shape]
Meter=shape
X=(25*#Scale#)
Y=(25*#scale#)r
Shape=Rectangle 0,0,(#W#-50*#Scale#),(40*#Scale#),(#R#*#Scale#) | StrokeWidth (#Stroke#*2) | Extend This
This=Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.4) | Stroke Color #Font_Color#,(#Alpha#*0.9)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=["#@#Actions\AHKv1.exe" "#@#Actions\Source Code\Search.ahk"]
[Searchbar.StringIcon]
Meter=String
FontFace=Segoe Fluent Icons
Text=[\xe721]
StringAlign=CenterCenter
X=(20*#Scale#)r
Y=(20*#Scale#)r
FontSize=((12*#Scale#)*#FontSizeModifier#)
MeterStyle=regularText
[Searchbar.String]
Meter=String
Text=Click here to search
StringAlign=LeftCenter
FontColor=#Dark_Color#
X=(20*#Scale#)r
Y=r
MeterStyle=regularText


[SearchBarAnchor]
Meter=String
Y=([SearchBar.Shape:YH]+#P#*#Divider#)
DynamicVariables=1
