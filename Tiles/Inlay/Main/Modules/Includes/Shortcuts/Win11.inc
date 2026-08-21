[Variables]
module_shortcuts.items.approx_w=90
module_shortcuts.items.p=(10*#Scale#)
module_shortcuts.items.content.p=(25*#Scale#)

[Shortcut.Shape:S]
X=(#module_shortcuts.shortcut_shape_w#/2+#module_shortcuts.items.p#)r
Y=(-#module_shortcuts.shortcut_shape_w#+#module_shortcuts.items.content.p#-#module_shortcuts.items.content.p#/2)r
Shape=Rectangle 0,0,#module_shortcuts.shortcut_shape_w#,#module_shortcuts.shortcut_shape_w#,(#R#*#Scale#) | StrokeWidth 0 | Extend This
This=Fill Color #Dark_Color#,(#Alpha#*0.0)
MouseOverAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.3)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
MouseLeaveAction=[!SetOption #CURRENTSECTION# This "Fill Color #Dark_Color#,(#Alpha#*0.0)"][!UpdateMeter #CURRENTSECTION#][!Redraw]
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
Y=(#module_shortcuts.items.content.p#/2)R
W=(#module_shortcuts.shortcut_shape_w#)
ClipString=1
StringAlign=CenterCenter
FontSize=((9*#Scale#)*#FontSizeModifier#)
[Shortcut.Shape.NewLine:S]
X=(25*#scale#)
Y=(#module_shortcuts.items.content.p#-10*#Scale#+#module_shortcuts.items.p#)r
