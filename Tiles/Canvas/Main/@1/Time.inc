[Variables]
@includegVar1=#SKINSPATH##MosaicShell\@Resources\gVars\Time.inc

[TimeDate]
Meter=String
MeasureName=MeasureTime
MeasureName2=MeasureDate
MeasureName3=MeasureAP
MEterStyle=Text | TimeDate:#gVar.2412#
[TimeDate:H]
Text=%1 - %2
[TimeDate:I]
Text=%1 %3 - %2

[PrductInfo]
Meter=String
MeasureName=MeasureProductName
MeasureName2=MeasureOSBits
MeasureName3=Measureversion
MeasureName4=MeasureCurrentBuild
MeasureName5=MeasureUBR
Text=%1 %2 bits %3 %4.%5
InlineSetting=Color | #BarColor#
InlinePattern=bits(.*)
MEterStyle=Text
UpdateDivider=-1
