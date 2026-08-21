[Variables]
Sec.W=(400*#Sec.S#)
Sec.H=(800*#Sec.S#)
Log=
@includeCoreWebResources=#ROOTCONFIGPATH#CoreWebResources.inc

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[PSRM]
Measure=Plugin
Plugin=PowershellRM
DynamicVariables=1
ScriptFile=#@#Powershell\Translate.ps1
ExecutionPolicy=Unrestricted

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HeaderImage]
Meter=Image
ImageName=#@#Icons\Translation.png
X=(#Sec.W#/2-96*#Sec.S#/2)
Y=(60*#Sec.S#)
W=(96*#Sec.S#)
H=(96*#Sec.S#)

[HeaderString]
Meter=String
X=(#Sec.W#/2)
Y=(20*#Sec.S#)R
StringAlign=Center
Text=Translate to: #Arg.1#
MeterStyle=Sec.String:S
FontSize=(14*#sec.s#)
FontWeight=650

[ShapeStart]
Meter=Shape
X=((#Sec.W#)/2-(150*2*#Sec.S#+#sec.P#)/2)
Y=#Sec.P#R
ColorLeave=0,100,0,150
ColorOver=0,100,0,250
LeftMouseUpAction=[!WriteKeyValue Variables Set.Lang "#Arg.1#" "#@#Vars.inc"][!CommandMeasure PSRM """Start-Translation -skin '#Arg.2#' -TargetLanguage "#Arg.1#" """]
MeterStyle=Sec.Button:S
[TextStart]
Meter=String
Text=Start
MEterStyle=Sec.String:S | Sec.Text:S

[ShapeExport]
Meter=Shape
Leftmouseupaction=[!WriteKeyValue Variables Set.Lang "#Arg.1#" "#@#Vars.inc"][!CommandMeasure PSRM """Export-LangFile -skin '#Arg.2#' -TargetLanguage "#Arg.1#" -directChange 0"""]
MeterStyle=Sec.Button:S
[TextExport]
Meter=String
Text=Export JSON
MEterStyle=Sec.String:S | Sec.Text:S

[Log]
Meter=String
X=(#Sec.P#*2)
Y=#Sec.P#R
Text=#Log#
H=(#Sec.H#-#Sec.P#-[Log:Y])
FontCOlor=#Set.Subtext_Color#
ClipString=2
DynamicVariables=1
InlinePattern=#CRLF#.*\.\.\.
InlineSetting=Color | #Set.Text_Color#
InlinePattern2=#CRLF#?(.*!#CRLF#)
InlineSetting2=Color | #Set.Accent_Color#
InlinePattern3=->
InlineSetting3=Color | #Set.Accent_Color_2#
MeterStyle=Sec.String:S
