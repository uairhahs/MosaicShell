[Rainmeter]
UpdateDivider=-1

[Variables]
@includeCacheVarsConfigurator=#@#CacheVars\Configurator.inc
@includeSkinHotkeys=#SKINSPATH##Skin.Name#\@Resources\Actions\HotKeys.ini

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
ScriptFile=#@#Lua\SetHotkey.lua
