; ============================================================================
; Scale contract is a host-agnostic UI scale for MosaicShell
; UiScale = DpiScale × UserScale
; Design space: logical px at 96 DPI / 100%. Multiply layout by #UiScale#.
; Set.W/Set.H are resized with UiScale via LastUiScale in ScaleContract.ps1.
; Aliases during Rainmeter era: Set.S, Sec.S, Ctx.S, Scale, Style.Scale → UiScale
;
; Prefer including ScaleVars.inc before this file when #@# is MosaicShell's
; @Resources. The @include below is for tiles that only know #SKINSPATH#.
; ============================================================================
[Variables]
@includeScaleVars=#SKINSPATH##MosaicShell\@Resources\ScaleVars.inc
UiScale=(#DpiScale#*#UserScale#)
