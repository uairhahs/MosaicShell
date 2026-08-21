[ActionTimer]
Measure=Plugin
Plugin=ActionTimer
ActionList1=Showfade
ActionList2=actionUnload | Hidefade
Showfade=[!SHowFade]
HideFade=[!HideFade]
actionUnload=[!CommandMeasure Func "actionUnload()"]
