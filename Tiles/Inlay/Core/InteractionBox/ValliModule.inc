﻿[Rainmeter]
SkinWidth=#Sec.W#

[Variables]
Sec.W=(#SCREENAREAWIDTH#-40*#Sec.S#)
Sec.H=(#SCREENAREAHEIGHT#-240*#Sec.S#)
Sec.Box.W=(445*#Sec.S#)
Sec.Box.H=(250*#Sec.S#)
Sec.Box.P=(70*#Sec.S#)
AniDir=Left
@includeSecVars=#@#SecVar.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\@Resources\Vars.inc
; @includeSkinvars=#SKINSPATH##Skin.Name#\@Resources\Vars.inc

[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#-20)
[anchorX]
Formula=50
[anchorY]
Formula=100

; ---------------------------------------------------------------------------- ;
;                                  MeterStyle                                  ;
; ---------------------------------------------------------------------------- ;

[Module.Shape:S]
X=(#Sec.P#*3+20*#Sec.S#)R
Y=(-#Sec.Box.H#+#Sec.P#+40*#Sec.S#)r
Shape=Rectangle 0,0,(#SEc.Box.W#),(#Sec.Box.H#),(#Sec.R#/4) | StrokeWidth 0 | Extend Fill
Shape2=Line (#Set.P#*0.5),(#Sec.Box.H#-#Sec.Box.P#),(#SEc.Box.W#-#Sec.P#),(#Sec.Box.H#-#Sec.Box.P#) | STrokeWidth 2 | Extend Stroke
Fill=Fill Color #Set.Sec_Color#
Stroke=Stroke Color #Set.Ter_Color#
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Ter_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Subtext_Color#,50"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color #Set.Sec_Color#"][!SetOption #CURRENTSECTION# Stroke "Stroke Color #Set.Ter_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyValue Variables #Sec.Arg1# [&Func:trim('#CURRENTSECTIOn#','.Shape','')] "#SKINSPATH#valliStart\@Resources\Vars.inc"][!SetVariable #Sec.Arg1# [&Func:trim('#CURRENTSECTIOn#','.Shape','')] "#MosaicShell\Main"][!UpdateMeter * "#MosaicShell\Main"][!Redraw "#MosaicShell\Main"][!UpdateMeasure Auto_Refresh:M "#MosaicShell\Main"][!CommandMeasure ActionTimer "Execute 2"]
DynamicVariables=1
[Module.Image:S]
X=(10*#Sec.S#)r
Y=(10*#Sec.S#)r
W=(#SEc.Box.W#-20*#Sec.S#)
H=(#Sec.Box.H#-#Sec.Box.P#-#Sec.P#)
ImageName=#SKINSPATH##Skin.Name#\Core\Module\[&Func:trim('#CURRENTSECTIOn#','.Image','')].png
DynamicVariables=1
MaskImageName=#SKINSPATH##Skin.Name#\Core\Module\#ModuleMask.png
[Module.String:S]
X=(#Sec.P#/2)r
Y=(-#Sec.P#/2)R
Text=[&Func:trim('#CURRENTSECTIOn#','.String','')]
StringAlign=LeftBottom
FontSize=((20*#Sec.S#)*#FontSizeModifier#)
FontWeight=650
DynamicVariables=1
[Module.Description.String:S]
X=r
Y=(#Sec.P#*1.5)r
W=(#Sec.Box.W#-#Sec.P#*2-40*#Sec.S#)
H=(#Sec.Box.P#-#Sec.P#)
FontSize=((12*#Sec.S#)*#FontSizeModifier#)
ClipString=2

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[GenericHeader.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#)
MeterStyle=Module.Shape:S
[GenericHeader.Image]
Meter=Image
MeterStyle=Module.Image:S
[GenericHeader.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[GenericHeader.Description.String]
Meter=String
Text=The default header module for Inlay.
MEterStyle=Sec.String:S | Module.Description.STring:S

[Win11Row.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Win11Row.Image]
Meter=Image
MeterStyle=Module.Image:S
[Win11Row.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Win11Row.Description.String]
Meter=String
Text=A gallery of shortcuts which is visually similar to the Windows 11 pinned shortcuts
MEterStyle=Sec.String:S | Module.Description.STring:S

[Connection.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Connection.Image]
Meter=Image
MeterStyle=Module.Image:S
[Connection.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Connection.Description.String]
Meter=String
Text=Connection module to toggle Wifi, Bluetooth and Internet access
MEterStyle=Sec.String:S | Module.Description.STring:S

[SearchBar.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[SearchBar.Image]
Meter=Image
MeterStyle=Module.Image:S
[SearchBar.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[SearchBar.Description.String]
Meter=String
Text=A button which sends a hotkey, which the default key is to open the search menu.
MEterStyle=Sec.String:S | Module.Description.STring:S

[DoubleRow.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*3+20*#Sec.S#)r
MeterStyle=Module.Shape:S
[DoubleRow.Image]
Meter=Image
MeterStyle=Module.Image:S
[DoubleRow.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[DoubleRow.Description.String]
Meter=String
Text=2 Rows of 5 Boxes of shortcuts.
MEterStyle=Sec.String:S | Module.Description.STring:S

[Mon.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Mon.Image]
Meter=Image
MeterStyle=Module.Image:S
[Mon.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Mon.Description.String]
Meter=String
Text=Change your monitor configuration on the fly
MEterStyle=Sec.String:S | Module.Description.STring:S

[Music.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Music.Image]
Meter=Image
MeterStyle=Module.Image:S
[Music.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Music.Description.String]
Meter=String
Text=Music control which can switch between different types of media players, and has the album art as the background
MEterStyle=Sec.String:S | Module.Description.STring:S

[Resource.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Resource.Image]
Meter=Image
MeterStyle=Module.Image:S
[Resource.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Resource.Description.String]
Meter=String
Text=Shows the percentage of usage for CPU, RAM and GPU
MEterStyle=Sec.String:S | Module.Description.STring:S

[SingleRow.Shape]
Meter=Shape
X=(#SEc.P#)
Y=(#Sec.P#*3+20*#Sec.S#)r
MeterStyle=Module.Shape:S
[SingleRow.Image]
Meter=Image
MeterStyle=Module.Image:S
[SingleRow.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[SingleRow.Description.String]
Meter=String
Text=1 Row of 5 shortcuts
MEterStyle=Sec.String:S | Module.Description.STring:S

[Volume.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Volume.Image]
Meter=Image
MeterStyle=Module.Image:S
[Volume.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Volume.Description.String]
Meter=String
Text=Change the volume by sliding the slider or scrolling, and change your playback device
MEterStyle=Sec.String:S | Module.Description.STring:S

[Weather.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[Weather.Image]
Meter=Image
MeterStyle=Module.Image:S
[Weather.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[Weather.Description.String]
Meter=String
Text=Shows Today's and Tomorrow's weather and temperature
MEterStyle=Sec.String:S | Module.Description.STring:S

[None.Shape]
Meter=Shape
MeterStyle=Module.Shape:S
[None.Image]
Meter=Image
MeterStyle=Module.Image:S
[None.String]
Meter=String
MEterStyle=Sec.String:S | Module.STring:S
[None.Description.String]
Meter=String
Text=Remove this slot's module
MEterStyle=Sec.String:S | Module.Description.STring:S
