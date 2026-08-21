[Variables]
module_shortcuts.items.approx_w=110
module_shortcuts.items.p=(10*#Scale#)
module_shortcuts.items.content.p=(#BorderedPadding#*#Scale#)

[Shortcut.Shape:S]
X=(#module_shortcuts.shortcut_shape_w#/2+#module_shortcuts.items.p#)r
Y=(-#module_shortcuts.shortcut_shape_w#+#module_shortcuts.items.content.p#-#module_shortcuts.items.content.p#/2)r
Shape=Rectangle 0,0,#module_shortcuts.shortcut_shape_w#,#module_shortcuts.shortcut_shape_w#,(#R#*#Scale#) | StrokeWidth (#Stroke#*2) | Extend This
This=Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.4) | Stroke Color #Font_Color#,(#Alpha#*0.9)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.2) | Stroke Color 0,0,0,0"][!UpdateMeter #CURRENTSECTION#][!Redraw]
DynamicVariables=1
[Shortcut.Image:S]
X=(#module_shortcuts.items.content.p#)r
Y=(#module_shortcuts.items.content.p#)r
W=(#module_shortcuts.shortcut_shape_w#-#module_shortcuts.items.content.p#*2)
H=(#module_shortcuts.shortcut_shape_w#-#module_shortcuts.items.content.p#*2)
DynamicVariables=1
[Shortcut.String:S]
Text=N/A
X=(#module_shortcuts.shortcut_shape_w#/2-#module_shortcuts.items.content.p#)r
Y=(#module_shortcuts.items.content.p#/2+(#module_shortcuts.shortcut_shape_w#-#module_shortcuts.items.content.p#*2))r
W=(#module_shortcuts.shortcut_shape_w#)
ClipString=1
Hidden=1
[Shortcut.Shape.NewLine:S]
X=(25*#scale#)
Y=((#module_shortcuts.items.content.p#/2)+#module_shortcuts.items.p#)r
