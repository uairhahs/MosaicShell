[Variables]
; --------------------------- Customizing variables -------------------------- ;
MinWindowW=(500*#UiScale#)
MinWindowH=(375*#UiScale#)
MaxWindowW=(1920*#UiScale#)
MaxWindowH=(1080*#UiScale#)
ScaleWindowW=(1000*#UiScale#)
Set.TitleBarH=(34*#UiScale#)
; ScaleWindowH=740
; ---------------------------------------------------------------------------- ;
;                             CacheWindowVariables                             ;
; ---------------------------------------------------------------------------- ;
WindowPosX=2046
WindowPosY=149
; ---------------------------- Important variables --------------------------- ;
WindowDragMarginSize=(6*#UiScale#)

; ---------------------------------------------------------------------------- ;
;                                   Measures                                   ;
; ---------------------------------------------------------------------------- ;

[WindowHandler]
Measure=Script
ScriptFile=#@#Lua\WindowResizeHelpers\WindowHandler#Set.LegacyWindowResizing#.lua
DynamicVariables=1

@IncludeMouse=#@#Includes\WindowDragMargins#Set.PerformanceMode#.inc
