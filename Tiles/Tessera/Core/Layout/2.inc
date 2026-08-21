
[OtherTitle]
Meter=String
Text=Legacy
MeterStyle=Set.String:S | Set.OptionCat:S
[PageSwitch]
Meter=String
X=([ContentContainer:W])
Y=r
Text=Return
FontColor=#Set.Subtext_Color#
StringAlign=Right
Container=ContentContainer
MeterStyle=Set.String:S
MouseOverAction=[!SetOption #CURRENTSECTION# InlineSetting Underline][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# InlineSetting ""][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyvalue Variables Page.page 1 "#SKINSPATH##Skin.Name#\Core\Layout.inc"][!Refresh]
FontSize=(10*[Set.S])
DynamicVariables=1

[Simple]
Meter=Image
X=(20*[Set.S])
Y=(30*[Set.S])R
MeterStyle=Select:S
[SimpleBox]
Meter=Shape
MeterStyle=SelectBox:S

[Amber]
Meter=Image
MeterStyle=Select:S
[AmberBox]
Meter=Shape
MeterStyle=SelectBox:S

[CoreUI]
Meter=Image
X=(20*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Select:S
[CoreUIBox]
Meter=Shape
MeterStyle=SelectBox:S

[Center]
Meter=Image
MeterStyle=Select:S
[CenterBox]
Meter=Shape
MeterStyle=SelectBox:S

[Modern]
Meter=Image
X=(20*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Select:S
[ModernBox]
Meter=Shape
MeterStyle=SelectBox:S
