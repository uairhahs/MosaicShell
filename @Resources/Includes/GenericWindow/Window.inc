[Variables]
; --------------------------- Customizing variables -------------------------- ;
MinWindowW=600
MinWindowH=375
MaxWindowW=1920
MaxWindowH=1080
ScaleWindowW=1000
; ScaleWindowH=740
; ---------------------------------------------------------------------------- ;
;                             CacheWindowVariables                             ;
; ---------------------------------------------------------------------------- ;
WindowPosX=147
WindowPosY=138
; ---------------------------- Important variables --------------------------- ;
WindowDragMarginSize=6


; ---------------------------------------------------------------------------- ;
;                                   Measures                                   ;
; ---------------------------------------------------------------------------- ;

[WindowHandler]
Measure=Script
ScriptFile=#@#Includes\GenericWindow\WindowHandler.lua
DynamicVariables=1

; ---------------------------------------------------------------------------- ;
;                                 Drag margins                                 ;
; ---------------------------------------------------------------------------- ;

[DragMarginStyle]
ShapeStyle=StrokeWidth 0 | Fill Color 0,0,0,1
ShapeStyle2=StrokeWidth 0 | Fill Color 0,0,0,1
LeftMouseDownAction=[!CommandMeasure WindowHandler ResizeWindow('#CurrentSection#',$MouseX$,$MouseY$)][!EnableMeasure MousePosition][!CommandMeasure MousePosition "Start"]
; LeftMouseUpAction=
; MouseOverAction=
MouseActionCursor=1
DynamicVariables=1
Group=Window | WindowDragMargin

[DragMarginTop]
Meter=Shape
X=0
Y=0
Shape=Rectangle 0, 0, #W#, #WindowDragMarginSize# | Extend ShapeStyle
MeterStyle=DragMarginStyle
MouseActionCursorName=topbottom.cur

[DragMarginLeft]
Meter=Shape
X=0
Y=0
Shape=Rectangle 0, 0, #WindowDragMarginSize#, #H# | Extend ShapeStyle
MeterStyle=DragMarginStyle
MouseActionCursorName=leftright.cur

[DragMarginBottom]
Meter=Shape
X=0
Y=(#H#- #WindowDragMarginSize#)
Shape=Rectangle 0, 0, #W#, #WindowDragMarginSize# | Extend ShapeStyle
MeterStyle=DragMarginStyle
MouseActionCursorName=topbottom.cur

[DragMarginRight]
Meter=Shape
X=(#W# - #WindowDragMarginSize#)
Shape=Rectangle 0, 0, #WindowDragMarginSize#, #H# | Extend ShapeStyle
MeterStyle=DragMarginStyle
MouseActionCursorName=leftright.cur

[DragMarginTopLeft]
Meter=Shape
X=0
Y=0
Shape=Rectangle 0, 0, #WindowDragMarginSize#, #WindowDragMarginSize# | Extend ShapeStyle2
MeterStyle=DragMarginStyle
MouseActionCursorName=topleftbottomright.cur

[DragMarginBottomLeft]
Meter=Shape
X=0
Y=(#H# - #WindowDragMarginSize#)
Shape=Rectangle 0, 0, #WindowDragMarginSize#, #WindowDragMarginSize# | Extend ShapeStyle2
MeterStyle=DragMarginStyle
MouseActionCursorName=toprightbottomleft.cur

[DragMarginTopRight]
Meter=Shape
X=(#W# - #WindowDragMarginSize#)
Y=0
Shape=Rectangle 0, 0, #WindowDragMarginSize#, #WindowDragMarginSize# | Extend ShapeStyle2
MeterStyle=DragMarginStyle
MouseActionCursorName=toprightbottomleft.cur

[DragMarginBottomRight]
Meter=Shape
X=(#W#- #WindowDragMarginSize#)
Y=(#H#- #WindowDragMarginSize#)
Shape=Rectangle 0, 0, #WindowDragMarginSize#, #WindowDragMarginSize# | Extend ShapeStyle2
MeterStyle=DragMarginStyle
MouseActionCursorName=topleftbottomright.cur
