[PageList.Configuration]
Meter=String
MeterStyle=Set.String:S | PageList.Category:S
Text=CONFIGURATION

[Info.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[Info.Icon.String]
Meter=String
Text="[\xe88e]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[Info.String]
Meter=String
Text=Info
MeterStyle=Set.String:S | PageList.Item.String:S

[DLC.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[DLC.Icon.String]
Meter=String
Text="[\xe854]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[DLC.String]
Meter=String
Text=DLC
MeterStyle=Set.String:S | PageList.Item.String:S

[General.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[General.Icon.String]
Meter=String
Text="[\xe8b8]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[General.String]
Meter=String
Text=General
MeterStyle=Set.String:S | PageList.Item.String:S

[Position.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[Position.Icon.String]
Meter=String
Text="[\xe871]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[Position.String]
Meter=String
Text=Position
MeterStyle=Set.String:S | PageList.Item.String:S

[NewMedia.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[NewMedia.Icon.String]
Meter=String
Text="[\xe405]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[NewMedia.String]
Meter=String
Text=Media
MeterStyle=Set.String:S | PageList.Item.String:S

[PageList.looknfeel]
Meter=String
MeterStyle=Set.String:S | PageList.Category:S
Text=LOOK N' FEEL

[Layout.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyvalue Variables Page.page 1 "#SKINSPATH##Skin.Name#\Core\Layout.inc"][!WriteKeyvalue Variables "Skin.Set_Page" "[&Func:trim('#CURRENTSECTION#', '.Shape', '')]" "#@#CacheVars\Configurator.inc"][!Refresh]
MeterStyle=PageList.Item.Shape:S
[Layout.Icon.String]
Meter=String
Text="[\xe936]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[Layout.String]
Meter=String
Text=Layout
MeterStyle=Set.String:S | PageList.Item.String:S

[Appearance.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[Appearance.Icon.String]
Meter=String
Text="[\xe243]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[Appearance.String]
Meter=String
Text=Appearance
MeterStyle=Set.String:S | PageList.Item.String:S

[Animation.Shape]
Meter=Shape
MeterStyle=PageList.Item.Shape:S
[Animation.Icon.String]
Meter=String
Text="[\xe71c]"
MeterStyle=Set.String:S | PageList.Item.Icon.String:S
[Animation.String]
Meter=String
Text=Animation
MeterStyle=Set.String:S | PageList.Item.String:S
