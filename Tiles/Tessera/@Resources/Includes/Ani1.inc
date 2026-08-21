[ActionTimer]
Measure=Plugin
Plugin=ActionTimer
DynamicVariables=1
ActionList1=Show | Repeat In,2,#AniSteps#
ActionList2=actionUnload | Repeat Out,2,#AniSteps# | Hide
Show=[!Show]
Hide=[!Hide]
actionUnload=[!CommandMeasure Func "actionUnload()"]
In=[!CommandMeasure Func "tweenAnimation('in')"]
Out=[!CommandMeasure Func "tweenAnimation('out')"]
