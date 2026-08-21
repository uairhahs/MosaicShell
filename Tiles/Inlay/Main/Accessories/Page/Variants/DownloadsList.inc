[Rainmeter]
MouseScrollUpAction=[!CommandMeasure mPath "PageUp"][!CommandMeasure Func "PageChange(-1)"][!UpdateMeasure mPath]
MouseScrollDownAction=[!CommandMeasure mPath "Pagedown"][!CommandMeasure Func "PageChange(1)"][!UpdateMeasure mPath]

[Variables]
IconSize=Medium
Sec.page=1

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[Icon:Style]
X=(22*#Scale#)
Y=(5*#scale#)r
W=(32*#Scale#)
H=(32*#Scale#)
AntiAlias=1
MeasureName=m#CURRENTSECTION#
Group=List
Hidden=1

[Index:Style]
SolidColor=0,0,0,1
X=(17*#scale#)
Y=(26*#scale#)r
DynamicVariables=1
Shape=Rectangle 0,0,(#W#-17*2*#Scale#),((32+10)*#Scale#),(#R#*#Scale#/2) | StrokeWidth (#Stroke#*2) | Extend Fill
Fill=Stroke Color 0,0,0,1 | Fill Color 0,0,0,1
MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color #BoxBorder# | Fill Color #BoxColor#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Stroke Color 0,0,0,1 | Fill Color 0,0,0,1"][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!CommandMeasure "mIndex#CurrentSection#Name" "FollowPath"][!UpdateMeasure mPath][!UpdateMeasureGroup Children][!UpdateMeter *][!Redraw]
RightMouseUpAction=[!CommandMeasure "mIndex#CurrentSection#Name" "ContextMenu"]
Group=List
Hidden=1

[Name:Style]
X=(5*#Scale#)R
Y=(32/2*#scale#)r
W=(#W#-80*#scale#)
CLipstring=1
Text=%1
StringAlign=LeftCenter
MeasureName=m#CURRENTSECTION#
Group=List
Hidden=1

[TopButton:Style]
X=(-32*#scale#)r
Y=r
StringAlign=Right
FontSize=((#scale#*20)*#FontSizeModifier#)
FontColor=#Font_Color#,150
MouseOverAction=[!SetOption #CURRENTSECTION# Font_Color "#Font_Color#"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# Font_Color "#Font_Color#,150"][!UpdateMeter #CURRENTSECTION#][!Redraw]
Fontface=Material Icons Round
AntiAlias=1

[Overlay:Style]
X=(#W#-22*#Scale#)
StringAlign=Right
FontColor=#Font_Color#,150
DynamicVariables=1

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;
[WinIcon]
ImageName=#@#Images\Orbs\Downloads.png
[Start]
MeasureName=mFileCount
MeasureName2=mFolderSizeCalc
Text=Downloads folder  x%1, %2 MB
W=(#W#-120*#scale#)
ClipString=1
InlinePattern=\s\s.*
InlineSetting=Size | (10*#Scale#)
InlinePattern2=\s\s.*
InlineSetting2=Weight | 400
InlinePattern3=\s\s.*
InlineSetting3=color | #Dark_Color#

[OpenFolder]
Meter=String
X=(#W#-22*#Scale#)
Y=r
Text=[\xe2c7]
MeterStyle=TopButton:Style
LeftMouseUpAction=["%USERPROFILE%\Downloads"]
[PageDown]
Meter=String
Text=[\xe5cf]
MeterStyle=TopButton:Style
Hidden=([&Func:SeekPage(-1)] = 0 ? 1 : 0)
LeftMouseUpAction=[!CommandMeasure mPath "Pagedown"][!CommandMeasure Func "PageChange(1)"][!UpdateMeasure mPath]
[PageUp]
Meter=String
Text=[\xe5ce]
MeterStyle=TopButton:Style
Hidden=([&Func:SeekPage(1)] = 0 ? 1 : 0)
LeftMouseUpAction=[!CommandMeasure mPath "PageUp"][!CommandMeasure Func "PageChange(-1)"][!UpdateMeasure mPath]

[1]
Y=(40*#Scale#)r

@includeGenerated=#ROOTCONFIGPATH#Main\Accessories\Page\Variants\DownloadsListCache.inc

[Anchor]
Meter=String
X=0
Y=(25*#scale#)R

; ---------------------------------------------------------------------------- ;
;                                    Overlay                                   ;
; ---------------------------------------------------------------------------- ;

[Overlay.Top]
Meter=String
Y=[1:Y]
Text=↑ [&Func:SeekPage(1)] items...
Hidden=([&Func:SeekPage(1)] = 0 ? 1 : 0)
MeterStyle=RegularText | Overlay:Style

[Overlay.Bottom]
Meter=String
Y=[#Sec.rows#:YH]
StringAlign=Rightbottom
Text=↓ [&Func:SeekPage(-1)] items...
Hidden=([&Func:SeekPage(-1)] = 0 ? 1 : 0)
MeterStyle=RegularText | Overlay:Style
