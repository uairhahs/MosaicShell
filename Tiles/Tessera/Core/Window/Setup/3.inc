[Header.Image]
Y=(50*[S])

[Header.String]
Text=Animations

[Description.String]
Text=How do you want your Flyouts animations?
InlinePattern=animations

[Box.1.Shape]
Meter=Shape
X=(#W#/2-(940*[S])/2)
Y=R
LeftMouseUpAction=[!WriteKeyValue Variables Ani "2" "#SKINSPATH#Tessera\@Resources\Vars.inc"][!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Box.Shape:S
[Box.1.String]
Meter=String
Text=Transitional+#CRLF#Provides the best visual experience
MeterStyle=Sec.String:S | Box.String:S

[Box.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Ani "1" "#SKINSPATH#Tessera\@Resources\Vars.inc"][!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Box.Shape:S
[Box.2.String]
Meter=String
Text=Slide + Fade#CRLF#Uses little resources. Makes activation more fluent.
MeterStyle=Sec.String:S | Box.String:S

[Box.3.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables Ani "0" "#SKINSPATH#Tessera\@Resources\Vars.inc"][!WriteKeyValue Variables Sec.arg2 "4" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Box.Shape:S
[Box.3.String]
Meter=String
Text=Fade only#CRLF#Uses least resources
MeterStyle=Sec.String:S | Box.String:S
