[ActionTimer]
Measure=Plugin
Plugin=ActionTimer
DynamicVariables=1
ActionList1=Show | Repeat In,2,#AniSteps#
ActionList2=Repeat Out,2,#AniSteps# | Hide
Show=[!CommandMeasure Func "InitActions(1)"]
Hide=[!CommandMeasure Func "InitActions(-1)"]
In=[!CommandMeasure Func "TweenAnimation('in')"]
Out=[!CommandMeasure Func "TweenAnimation('out')"]


; ---------------------------------------------------------------------------- ;
;                                     Load                                     ;
; ---------------------------------------------------------------------------- ;

[ACTIONLOAD]
Measure=String
OnUpdateAction=[!CommandMeasure ActionTimer "Execute 1"]
Disabled=1
Group=NUOL

[ACTIONUNLOAD]
Measure=String
OnUpdateAction=[!CommandMeasure ActionTimer "Execute 2"][!DisableMeasure #CURRENTSECTION#]
Disabled=1
Group=NUOL
