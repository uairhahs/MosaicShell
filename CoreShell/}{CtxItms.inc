[Rainmeter]
RightMouseUpAction=[!UpdateMeasureGroup CurPos.XY][!CommandMeasure Ctx.Gen:M "StartCtx()"]

[CurPos.X]
Measure=Plugin
Plugin=MouseXY
Dimension=X
Group=CurPos.XY
UpdateDivider=-1
[CurPos.Y]
Measure=Plugin
Plugin=MouseXY
Dimension=Y
Group=CurPos.XY
UpdateDivider=-1



; [RefreshBlur]
; Measure=Calc
; Group=NUOL
; Disabled=1
; UpdateDivider=-1
; OnUpdateAction=[!Delay 1000][!CommandMeasure ImageBlur Update]

[Ctx.Gen:M]
Measure=Script
ScriptFile=#SKINSPATH##MosaicShell\@Resources\Lua\CtxHelpers\GenCtx.lua
UpdateDivider=-1
