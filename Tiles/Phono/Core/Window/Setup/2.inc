﻿[Header.Image]
Y=(50*[S])

[Header.String]
Text=Music info

[Description.String]
Text=Phono can work with both NowPlaying or the Windows API. Which one do you want to use?
InlinePattern=NowPlaying|Windows API

[Box.1.Shape]
Meter=Shape
X=(#W#/2-(620*[S])/2)
Y=R
LeftMouseUpAction=[!WriteKeyValue Variables MediaType "Modern" "#SKINSPATH#Phono\@Resources\Vars.inc"][!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Pos_Box.Shape:S
[Box.2.String]
Meter=String
Text=Windows API#CRLF#Expected to work for all players with no setup. Slower update speed but most reliable
MeterStyle=Sec.String:S | Box.String:S

[Box.2.Shape]
Meter=Shape
LeftMouseUpAction=[!WriteKeyValue Variables MediaType "Auto" "#SKINSPATH#Phono\@Resources\Vars.inc"][!WriteKeyValue Variables Sec.arg2 "3" "#CURRENTPATH#Main.ini"][!Refresh]
MeterStyle=Box.Shape:S
[Box.1.String]
Meter=String
Text=NowPlaying#CRLF#Recommended if you've used AUTO media method in other modules. Fully supports a selection of players.
MeterStyle=Sec.String:S | Box.String:S
