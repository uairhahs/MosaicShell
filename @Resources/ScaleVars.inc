[Variables]
; Host-agnostic scale contract persistence.
; UiScale = DpiScale * UserScale (see Includes\ScaleContract.inc)
; DpiScale: Windows display scale (1.0 = 100%, 1.5 = 150%)
; UserScale: user override multiplier (1.0 = match Windows)
; LastUiScale: UiScale that Set.W/Set.H were last sized for (1 = design space)
DpiScale=1
UserScale=1
LastUiScale=1
