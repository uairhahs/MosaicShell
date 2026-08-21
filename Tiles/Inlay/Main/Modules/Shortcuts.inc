﻿[Variables]
@includeModuleVariable=Vars\Shortcuts.inc
module_shortcuts.shortcut_shape_w=((#W#-50*#scale#-#module_shortcuts.items.p#*(#module_shortcuts.rowitems_count#-1))/#module_shortcuts.rowitems_count#)

; ---------------------------------------------------------------------------- ;
;                                  MeterStyles                                 ;
; ---------------------------------------------------------------------------- ;

@includeShortcutStyles=Includes\Shortcuts\#ShortcutBoxStyles#.inc

; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;

[Module_Shortcuts.Header]
Meter=String
X=(#W# >= 600 ? (#W#/2) : (25*#Scale#))
Y=(25*#Scale#)r
MeterStyle=RegularText
Text=Pinned applications
FontColor=#Dark_Color#,200
StringAlign=[&Func:returnCenterString(600)]Center
DynamicVariables=1

[Module_Shortcuts.Edit_Button.String]
Meter=String
X=(#W#-25*#Scale#)
Y=(1)r
FontSize=(12*#Scale#)
MeterStyle=RegularText
Text=[\xe710]
LeftMouseUpAction=[!CommandMeasure Func "StartEditMode()"]
ToolTipText=Edit shortcuts...
FontFace=Segoe Fluent Icons
FontColor=#Dark_Color#,200
StringAlign=RightCenter
DynamicVariables=1

[Shortcut1.Shape]
X=(#W#/2-(#module_shortcuts.shortcut_shape_w#*(Clamp(#module_shortcuts.totalitems_count#,0,#module_shortcuts.rowitems_count#))+#module_shortcuts.items.p#*(Clamp(#module_shortcuts.totalitems_count#,0,#module_shortcuts.rowitems_count#)-1))/2)
Y=(25*#scale#)r

@includeShortcuts=#SKINSPATH#..\CoreData\Inlay\Shortcuts.inc

[ShortcutsAnchor]
Meter=String
Y=((#module_shortcuts.items.content.p#/2)+25*#Scale#)r
DynamicVariables=1
