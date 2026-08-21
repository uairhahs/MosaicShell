[Rainmeter]
SkinWidth=#Sec.W#

[Variables]
Sec.W=(800*#Sec.S#)
Sec.H=(300*#Sec.S#)
Sec.Size1=(180*#Sec.S#)
Sec.Size2=0
AniDir=Bottom
Sec.SaveLocation=#SKINSPATH#..\CoreData\Keylaunch\Act.inc
@includeSkinActions=#SKINSPATH#..\CoreData\Keylaunch\Act.inc


[moveX]
Formula=(#SCREENAREAWIDTH#/2)
[moveY]
Formula=(#SCREENAREAHEIGHT#/2)
[anchorX]
Formula=50
[anchorY]
Formula=50

[ActionTimer]
Custom1=

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Choose:M]
Measure=Plugin
Plugin=FileChoose
UseNewStyle=0
GetTarget=0
AllowNonstorageFolder=1
CopyLink=1
GetIcon=1
IconCache=#SKINSPATH#..\CoreData\Keylaunch\IconCache\
IconSize=3
ReturnValue=WholePath
FileInitialDirectory=
ImageInitialDirectory=#CurrentPath#IconCache\
DynamicVariables=1
Command1=[!UpdateMEasure #CURRENTSECTION#][!CommandMeasure GenPs1 """WriteTo -index "#Sec.arg1#" -param1 '$Name$' -param2 '["$Path$"]' -param3 '$Name$_$Ext$'""" "#MosaicShell\Main"]
Command2=[!UpdateMEasure #CURRENTSECTION#][!CommandMeasure GenPs1 """WriteTo -index "#Sec.arg1#" -param1 '$Name$' -param2 '["$Path$"]'' """ "#MosaicShell\Main"]

[Input:M]
Measure=Plugin
Plugin=InputText
SolidColor=#Set.Pri_Color#
FontColor=#Set.Text_Color#
FontFace=Consolas
FontSize=(10*#Sec.S#)
StringAlign=Center
X=(#Sec.P#)
Y=(#Sec.H#-#Sec.P#-[KeyLabel:H])
W=(#Sec.W#-#sec.P#*2)
H=([KeyLabel:H])
DynamicVariables=1
DefaultValue="[##Editing#]"
Command1=[!CommandMeasure GenPs1 """WriteTo -index "#Sec.arg1#" -param1 '$UserInput$' """ "#MosaicShell\Main"]
Command2=[!CommandMeasure GenPs1 """WriteTo -index "#Sec.arg1#" -param2 '$UserInput$' """ "#MosaicShell\Main"]
Command3=[!CommandMeasure GenPs1 """WriteTo -index "#Sec.arg1#" -param3 '$UserInput$' """ "#MosaicShell\Main"]

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

[Button.String:S]
X=(10*#Sec.S#)R
Y=r
W=
FontColor=0,50,255
MouseOverAction=[!SetOption #CURRENTSECTION# InlineSetting "Underline"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# InlineSetting ""][!UpdateMeter #CURRENTSECTION#][!Redraw]

[Path:M]
Measure=String
String=
; RegexpSubstitute=1
Substitute="\":"\\"

[Sec.String:S]
FontFace=Consolas
W=(#Sec.W#-#Sec.S#-(#Sec.H#-#Sec.P#-30*#Sec.S#))
ClipString=2

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;


[KeyLabel]
Meter=String
X=(#Sec.P#)
Y=(#Sec.P#)
W=(#Sec.W#-#Sec.P#*2)
ClipString=2
FontSize=(10*#SEc.S#)
FontColor=#Set.Subtext_Color#
Text=Editing #Sec.arg1#
MeterStyle=Sec.String:S

[Image]
MEter=Image
X=r
Y=(30*#Sec.S#)
W=(#Sec.H#-#Sec.P#-30*#Sec.S#)
H=(#Sec.H#-#Sec.P#-30*#Sec.S#)
DynamicVariables=1
ImageName=#SKINSPATH#..\CoreData\Keylaunch\IconCache\[##Sec.arg1#Icon].png

[Name]
Meter=String
X=(#Sec.P#)R
Y=r
FontSize=(20*#Sec.S#)
FontWeight=(650)
Text=[##Sec.arg1#Name]
Prefix="Name = "
Postfix=""
Leftmouseupaction=[!SetVariable Editing "#Sec.arg1#Name"][!CommandMeasure Input:M "ExecuteBatch 1"]
MeterStyle=Sec.String:S

[Act]
Meter=String
X=r
Y=(10*#Sec.S#)R
Prefix="Action = "
Postfix=""
Text=[##Sec.arg1#Action]
Leftmouseupaction=[!SetVariable Editing "#Sec.arg1#Action"][!CommandMeasure Input:M "ExecuteBatch 2"]
MeterStyle=Sec.String:S

[ImageName]
Meter=String
X=r
Y=(10*#Sec.S#)R
Prefix="Icon.FileName = "
Postfix=""
Text=[##Sec.arg1#Icon]
Leftmouseupaction=[!SetVariable Editing "#Sec.arg1#Icon"][!CommandMeasure Input:M "ExecuteBatch 3"]
MeterStyle=Sec.String:S

; ---------------------------------- Button ---------------------------------- ;

[Button1.String]
Meter=String
X=r
Y=(20*#Sec.S#)R
Text=Select file
LeftMouseUpAction=[!CommandMeasure Choose:M "ChooseFile 1"]
MEterStyle=Sec.String:S | Button.String:S

[Button2.String]
Meter=String
Text=Select folder
LeftMouseUpAction=[!CommandMeasure Choose:M "ChooseFolder 2"]
MEterStyle=Sec.String:S | Button.String:S

[Button3.String]
Meter=String
Text=Open icons directory
Leftmouseupaction=["#SKINSPATH#..\CoreData\Keylaunch\IconCache\"]
MEterStyle=Sec.String:S | Button.String:S

[Button4.String]
Meter=String
X=([Button1.String:X])
Y=(10*#Sec.S#)R
Text=Help with custom actions
Leftmouseupaction=["https://docs.mosaicshell.io/core/getting-started/faqs/modules-faqs/keylaunch#custom-actions-advanced-usage"]
FontColor=#Set.Subtext_Color#
MEterStyle=Sec.String:S | Button.String:S

[Button5.String]
Meter=String
Text=Apply changes
FontColor=#Set.Positive_color#
Leftmouseupaction=[!Refresh "#MosaicShell\Main"][!Refresh "#Skin.Name#\Main"][!CommandMeasure ActionTimer "Execute 2"]
MEterStyle=Sec.String:S | Button.String:S
