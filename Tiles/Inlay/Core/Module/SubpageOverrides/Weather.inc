[Variables]
@includeModulevars=#SKINSPATH##Skin.Name#\Main\Modules\Vars\#Page.SubpageModule#.inc
Sec.SaveLocation=#SKINSPATH##Skin.Name#\Main\Modules\Vars\#Page.SubpageModule#.inc

[Item1.String]
Text=Configure weather settings in core's global setting
[Item1.Button]
Meter=Shape
MeterStyle=Item.Button.Shape:S
[Item1.Button.StringIcon]
Meter=String
Text=[\xe89e]
LeftMouseUpAction=[!WriteKeyvalue Variables Skin.Name """#MosaicShell""" "#@#CacheVars\Configurator.inc"][!WriteKeyvalue Variables Skin.Set_Page GlobalVars "#@#CacheVars\Configurator.inc"][!Refresh]
MeterStyle=Set.String:S | Item.Button.StringIcon:S

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[HotkeysTitle]
Meter=String
Text=Appearance
MeterStyle=Set.String:S | Set.OptionCat:S

[Option01]
Meter=String
Text=Hide weather location
MeterStyle=Set.String:S | Set.OptionName:S
[Divider01]
Meter=Shape
MeterStyle=Set.Div:S
[Option02]
Meter=String
Text=Tint weather icons to accent color
MeterStyle=Set.String:S | Set.OptionName:S

; ---------------------------------------------------------------------------- ;
;                                     Input                                    ;
; ---------------------------------------------------------------------------- ;

[HideLocation]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option01:Y]-#Set.P#+(-20/2+8)*[Set.S])
[TintIcons]
Meter=Shape
MeterStyle=Set.Bool:S
Y=([Option02:Y]-#Set.P#+(-20/2+8)*[Set.S])
