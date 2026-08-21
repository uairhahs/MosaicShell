[Rainmeter]
UpdateDivider=-1

[Variables]
@includeSecVar=#@#CacheVars\Configurator.inc
@includeSkinHotkeys=#SKINSPATH#..\CoreData\#Skin.Name#\HotKeys.inc

[mActions]
Measure=Plugin
Plugin=ActionTimer
ActionList1=StartAHK
StartAHK=[!CommandMeasure mSet "Start()"]
DynamicVariables=1

[Dum]
Meter=String

[mSet]
Measure=Script
ScriptFile=#SKINSPATH##Skin.Name#\Core\SetHotkey.lua
