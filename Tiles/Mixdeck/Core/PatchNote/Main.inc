[Variables]
@includeWelcomeVar=#SKINSPATH##Skin.Name#\@Resources\WelcomeVar.inc
[ImageContainer]
Hidden=1
[Image]
Hidden=1

@includeContent=#SKINSPATH##Skin.Name#\Core\PatchNote\Main#Core.WelcomeCheck#.inc

[Anchor]
Meter=String
X=0
Y=(#Sec.P#)R
