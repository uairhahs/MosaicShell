﻿[Variables]
Page.Subpage=1
Page.Complete_Reinstallation=0
Sec.AnchorName=AnchorSuppli
Page.Reinstallation_isSingle=True
; [Image]
; Meter=Image
; W=(#Set.W#)
; H=(#Set.H#*0.5)
; Group=Scroll
; ImageAlpha=(-#Scroll# > 0 ? 0 : 25)
; PreserveAspectRatio=2
; DynamicVariables=1
; ImageName=#@#Images\Cards\GithubBanner.png
; MaskImageName=#@#Images\FadeMask.png
; Container=BC

; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[Header.String]
Text=Shop

[Header.Button.Icon.String]
Meter=String
MeterStyle=Set.String:S
FontFace=Material Icons Round
X=(#Set.W#-#HomePage.P#)
Y=r
Text=[\xf091]
InlineSetting=GradientColor | 45 | #SEt.Accent_Color_2# ; 1 | #Set.Accent_color# ;0
StringAlign=RightCenter
FontSize=(32*[Set.S])
LeftMouseUpAction=[!WriteKeyValue Variables Page.SubPage "2" "#ROOTCONFIGPATH#CoreShell\Home\Page2.inc"][!Refresh]
[Header.Button.Text.String]
Meter=String
X=(-[Header.Button.Icon.String:W]-10*[Set.S])r
Y=r
Text=Installed DLCs
FontColor=#Set.Subtext_Color#
StringAlign=RightCenter
MeterStyle=Set.String:S

; ------------------------------ Include subpage ----------------------------- ;

@includeSubpage=Assets\Page2\Subpage#Page.subpage#.inc


[AnchorSuppli]
Meter=String
Container=BC
x=r
Y=(50*[Set.S])r
DynamicVariables=1
