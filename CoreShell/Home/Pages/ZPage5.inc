﻿; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;


[Page.Paragraph:S]
X=#HomePage.P#
W=(#Set.W#-#HomePage.P#*2)
ClipSTring=2

[Table.VerticalDivider.Shape:S]

[Table.Divider.Shape:S]
Shape=Line 0,0,(#Set.W#-#HomePage.P#*2),0 | StrokeWidth 2 | Stroke Color #Set.Ter_Color# | StrokeStartCap Round | StrokeEndCap Round
DynamicVariables=1
Container=#Sec.Container#

[Table.Column1.String:S]

[Table.Column2.String:S]

[Table.Column3.String:S]

[Table.Group.String:S]
X=r
Y=(10*[Set.S])R
FontSize=(24*[Set.S])
FontWeight=650
InlineSetting=GradientColor | 45 | #Set.Accent_Color_2# ; 1 | #Set.Accent_Color# ;0
; ---------------------------------------------------------------------------- ;
;                                    Meters                                    ;
; ---------------------------------------------------------------------------- ;

[Header.String]
Text=More cool apps

[Page.Description]
Meter=String
Y=(120*[Set.S])
Text=You can find a list of awesome applications to use on Windows here. These are all subjective opinions of the person(s) involved.
MeterStyle=Set.String:S | Page.Paragraph:S

[Table.Group1.String]
Meter=String
Text=Coming soon!
MeterStyle=Set.String:S | Table.Group.String:S

[Table.Divider1.Shape]
Meter=Shape
X=r
Y=(10*[Set.S])R
MeterStyle=Table.Divider.Shape:S

[Table.Row1.Column1.String]
Meter=String
MeterStyle=Set.String:S | Table.Column1.String:S
