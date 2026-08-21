[Variables]
; --------------------------- Customizing variables -------------------------- ;
MinWindowW=500
MinWindowH=375
MaxWindowW=1920
MaxWindowH=1080
ScaleWindowW=1000
Set.TitleBarH=34
; ScaleWindowH=740
; ---------------------------------------------------------------------------- ;
;                             CacheWindowVariables                             ;
; ---------------------------------------------------------------------------- ;
WindowPosX=2046
WindowPosY=149
; ---------------------------- Important variables --------------------------- ;
WindowDragMarginSize=6

; ---------------------------------------------------------------------------- ;
;                                   Measures                                   ;
; ---------------------------------------------------------------------------- ;

[WindowHandler]
Measure=Script
ScriptFile=#@#Lua\WindowResizeHelpers\WindowHandler#Set.LegacyWindowResizing#.lua
DynamicVariables=1

@IncludeMouse=#@#Includes\WindowDragMargins#Set.PerformanceMode#.inc
