
[FeaturedTitle]
Meter=String
Text=Highlights
MeterStyle=Set.String:S | Set.OptionCat:S
[PageSwitch]
Meter=String
X=([ContentContainer:W])
Y=r
Text=Legacy Styles
FontColor=#Set.Subtext_Color#
StringAlign=Right
Container=ContentContainer
MeterStyle=Set.String:S
MouseOverAction=[!SetOption #CURRENTSECTION# InlineSetting Underline][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# InlineSetting ""][!UpdateMeter #CURRENTSECTION#][!Redraw]
LeftMouseUpAction=[!WriteKeyvalue Variables Page.page 2 "#SKINSPATH##Skin.Name#\Core\Layout.inc"][!Refresh]
FontSize=(10*[Set.S])
DynamicVariables=1

[Fluent]
Meter=Image
X=(20*[Set.S])
Y=(30*[Set.S])R
MeterStyle=Select:S
[FluentBox]
Meter=Shape
MeterStyle=SelectBox:S

[Win11]
Meter=Image
MeterStyle=Select:S
[Win11Box]
Meter=Shape
MeterStyle=SelectBox:S

[Gnome]
Meter=Image
X=(20*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Select:S
[GnomeBox]
Meter=Shape
MeterStyle=SelectBox:S

[Pixel]
Meter=Image
MeterStyle=Select:S
[PixelBox]
Meter=Shape
MeterStyle=SelectBox:S

[Plainext]
Meter=Image
X=(20*[Set.S])
Y=(20*[Set.S])R
MeterStyle=Select:S
[PlainextBox]
Meter=Shape
MeterStyle=SelectBox:S
