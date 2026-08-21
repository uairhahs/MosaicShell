﻿; ---------------------------------------------------------------------------- ;
;                                   Function                                   ;
; ---------------------------------------------------------------------------- ;
[Variables]
@includeModuleVariable=Vars\Weather.inc
@includegVar2=#SKINSPATH##MosaicShell\@Resources\gVars\Weather.inc
@includegVar3=#SKINSPATH##MosaicShell\@Resources\gVars\WeatherAPIKey.inc
WeatherHeight=165
WeatherUnitsMetric=m
WeatherUnitsImperial=e

Substitute="^null$":"", '^"(.*)"$':"\1", "^\\1$":""
SubstituteIcons="^0$":"storm", "^1$":"storm", "^2$":"storm", "^3$":"storm", "^4$":"storm", "^5$":"sleet", "^6$":"sleet", "^7$":"sleet", "^8$":"sleet", "^9$":"rain", "^10$":"sleet", "^11$":"rain", "^12$":"rain", "^13$":"snow", "^14$":"snow", "^15$":"snow", "^16$":"snow", "^17$":"sleet", "^18$":"sleet", "^19$":"fog", "^20$":"fog", "^21$":"fog", "^22$":"fog", "^23$":"windy", "^24$":"windy", "^25$":"snow", "^26$":"cloudy", "^27$":"cloudy-night", "^28$":"cloudy-day", "^29$":"cloudy-night", "^30$":"cloudy-day", "^31$":"clear-night", "^32$":"clear-day", "^33$":"partly-cloudy-night", "^34$":"partly-cloudy-day", "^35$":"sleet", "^36$":"clear-day", "^37$":"storm-day", "^38$":"storm-day", "^39$":"rain", "^40$":"rain", "^41$":"snow-day", "^42$":"snow", "^43$":"snow", "^44$":"unknown", "^45$":"rain-night", "^46$":"snow-night", "^47$":"storm-night", "^na$":"", "":"unknown"
RegExpDaily=(?siU){"class":.*,"expire_time_gmt":.*,"fcst_valid":.*,"fcst_valid_local":(.*),"num":.*,"max_temp":(.*),"min_temp":(.*),"torcon":.*,"stormcon":.*,"blurb":.*,"blurb_author":.*,"lunar_phase_day":.*,"dow":.*,"lunar_phase":(.*),"lunar_phase_code":(.*),"sunrise":(.*),"sunset":(.*),"moonrise":(.*),"moonset":(.*),"qualifier_code":.*,"qualifier":.*,"narrative":(.*),"qpf":.*,"snow_qpf":.*,"snow_range":.*,"snow_phrase":.*,"snow_code":.*,"night":{"fcst_valid":.*,"fcst_valid_local":.*,"day_ind":.*,"thunder_enum":.*,"daypart_name":(.*),"long_daypart_name":.*,"alt_daypart_name":.*,"thunder_enum_phrase":.*,"num":.*,"temp":.*,"hi":.*,"wc":.*,"pop":(.*),"icon_extd":.*,"icon_code":(.*),"wxman":.*,"phrase_12char":.*,"phrase_22char":.*,"phrase_32char":(.*),"subphrase_pt1":.*,"subphrase_pt2":.*,"subphrase_pt3":.*,"precip_type":.*,"rh":(.*),"wspd":(.*),"wdir":(.*),"wdir_cardinal":(.*),"clds":(.*),"pop_phrase":.*,"temp_phrase":.*,"accumulation_phrase":.*,"wind_phrase":.*,"shortcast":.*,"narrative":.*,"qpf":.*,"snow_qpf":.*,"snow_range":.*,"snow_phrase":.*,"snow_code":.*,"vocal_key":.*,"qualifier_code":.*,"qualifier":.*,"uv_index_raw":.*,"uv_index":(.*),"uv_warning":.*,"uv_desc":(.*),"golf_index":.*,"golf_category":.*}(?(?=,"day"),"day":{"fcst_valid":.*,"fcst_valid_local":.*,"day_ind":.*,"thunder_enum":.*,"daypart_name":(.*),"long_daypart_name":.*,"alt_daypart_name":.*,"thunder_enum_phrase":.*,"num":.*,"temp":.*,"hi":.*,"wc":.*,"pop":(.*),"icon_extd":.*,"icon_code":(.*),"wxman":.*,"phrase_12char":.*,"phrase_22char":.*,"phrase_32char":(.*),"subphrase_pt1":.*,"subphrase_pt2":.*,"subphrase_pt3":.*,"precip_type":.*,"rh":(.*),"wspd":(.*),"wdir":(.*),"wdir_cardinal":(.*),"clds":(.*),"pop_phrase":.*,"temp_phrase":.*,"accumulation_phrase":.*,"wind_phrase":.*,"shortcast":.*,"narrative":.*,"qpf":.*,"snow_qpf":.*,"snow_range":.*,"snow_phrase":.*,"snow_code":.*,"vocal_key":.*,"qualifier_code":.*,"qualifier":.*,"uv_index_raw":.*,"uv_index":(.*),"uv_warning":.*,"uv_desc":(.*),"golf_index":.*,"golf_category":.*})}
;======================

[mUnitsTemperature]
Measure=String
String=[#WeatherUnits[#gVar.WeatherUnits]]
RegExpSubstitute=1
Substitute="^m$":"C", "^e$":"F"
UpdateDivider=1

;======================

[mLocationParent]
Measure=WebParser

URL="https://nominatim.openstreetmap.org/search?q=#gVar.WeatherLocation#&format=geocodejson&limit=1&addressdetails=1&accept-language=#gVar.Language#"
RegExp=(?siU)"features":\[{"type":"Feature","properties":{"geocoding":{.*,"label":"(.*)",.*,"admin":{.*?"level\d+":"(.*)"}}},"geometry":{"type":"Point","coordinates":\[(.*),(.*)\]}}\]
UpdateRate=-1

FinishAction=[!UpdateMeasureGroup Location] [!EnableMeasureGroup Parent] [!UpdateMeasureGroup Parent]
UpdateDivider=1

[mLocationCity]
Group=Location
Measure=WebParser
URL=[mLocationParent]
StringIndex=2
Substitute="":"Earth"
UpdateDivider=1

[mLocationLat]
Group=Location
Measure=WebParser
URL=[mLocationParent]
StringIndex=4
UpdateDivider=1

[mLocationLon]
Group=Location
Measure=WebParser
URL=[mLocationParent]
StringIndex=3
UpdateDivider=1

;======================

[mCurrentParent]
Group=Parent
Measure=WebParser

URL=https://api.weather.com/v3/wx/observations/current?geocode=[&mLocationLat],[&mLocationLon]&units=[#WeatherUnits[#gVar.WeatherUnits]]&language=#gVar.Language#&format=json&apiKey=#gVar.WeatherAPIKey#
RegExp=(?siU){"cloudCeiling":.*,"cloudCoverPhrase":.*,"dayOfWeek":.*,"dayOrNight":.*,"expirationTimeUtc":.*,"iconCode":(.*),"iconCodeExtend":.*,"obsQualifierCode":.*,"obsQualifierSeverity":.*,"precip1Hour":.*,"precip6Hour":.*,"precip24Hour":.*,"pressureAltimeter":(.*),"pressureChange":.*,"pressureMeanSeaLevel":.*,"pressureTendencyCode":.*,"pressureTendencyTrend":.*,"relativeHumidity":(.*),"snow1Hour":.*,"snow6Hour":.*,"snow24Hour":.*,"sunriseTimeLocal":.*,"sunriseTimeUtc":.*,"sunsetTimeLocal":.*,"sunsetTimeUtc":.*,"temperature":(.*),"temperatureChange24Hour":.*,"temperatureDewPoint":.*,"temperatureFeelsLike":.*,"temperatureHeatIndex":.*,"temperatureMax24Hour":.*,"temperatureMaxSince7Am":.*,"temperatureMin24Hour":.*,"temperatureWindChill":.*,"uvDescription":(.*),"uvIndex":(.*),"validTimeLocal":.*,"validTimeUtc":.*,"visibility":(.*),"windDirection":(.*),"windDirectionCardinal":(.*),"windGust":.*,"windSpeed":(.*),"wxPhraseLong":(.*),"wxPhraseMedium":.*,"wxPhraseShort":.*}
UserAgent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36
UpdateRate=(60*15)

FinishAction=[!EnableMeasureGroup Current] [!UpdateMeasureGroup Current] [!UpdateMeter *][!Redraw]
OnConnectErrorAction=[!Log "Unable to connect to Weather.com." Error]
OnRegExpErrorAction=[!Log "Something went wrong." Error]

Disabled=1
DynamicVariables=1
UpdateDivider=1

[mCurrentIcon]
Group=Current
Measure=WebParser
URL=[mCurrentParent]
StringIndex=1
RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"
UpdateDivider=1


[mCurrentTemperature]
Group=Current
Measure=WebParser
URL=[mCurrentParent]
StringIndex=4
RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"
UpdateDivider=1


[mCurrentPhrase]
Group=Current
Measure=WebParser
URL=[mCurrentParent]
StringIndex=11
RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

;======================

[mDailyParent]
Group=Parent
Measure=WebParser

URL=https://api.weather.com/v1/geocode/[&mLocationLat]/[&mLocationLon]/forecast/daily/7day.json?units=[#WeatherUnits[#gVar.WeatherUnits]]&language=#gVar.Language#&apiKey=#gVar.WeatherAPIKey#
RegExp=(?siU)"forecasts":\[({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}})\]
UserAgent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36
UpdateRate=1

FinishAction=[!EnableMeasureGroup Daily] [!UpdateMeasureGroup Daily] [!UpdateMeter *][!Redraw]

DynamicVariables=1
UpdateDivider=-1
Disabled=1

; ----------------------------------- Day0 ----------------------------------- ;

[mDay1WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay1WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay1WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay1TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay1TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay1Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"
; ---------------------------------------------------------------------------- ;
;                                    Content                                   ;
; ---------------------------------------------------------------------------- ;

[WeatherIcon]
Meter=Image
X=(25*#scale#)
Y=(35*#scale#)r
W=(100*#Scale#)
H=(100*#Scale#)
ImageName=#@#Icons\JD Weather Icons\[mCurrentIcon].png
DynamicVariables=1
UpdateDivider=1
MeterStyle=WeatherIcon:#TintIcons#
[WeatherIcon:1]
Greyscale=1
ImageTint=#Accent_Color#

[WeatherText]
Meter=String
MeterStyle=RegularText | WeatherText:#HideLocation#
X=(25*#scale#)R
Y=(100*#scale#/2)r
FontSize=((20*#Scale#)*#FontSizeModifier#)
InlinePattern=#CRLF#.*
InlineSetting=Size | (12*#scale#)
DynamicVariables=1
W=(#W#-#P#*2-200*#Scale#)
MeasureName=mCurrentTemperature
Measurename2=mUnitsTemperature
MeasureName3=mCurrentPhrase
UpdateDivider=1
StringAlign=LeftCenter
ClipString=2
[WeatherText:0]
Text=%1%2#CRLF#Weather's %3 in #gVar.WeatherLocation#
[WeatherText:1]
Text=%1%2#CRLF#Weather's currently %3

; --------------------------------- forecast --------------------------------- ;

[ForecastIcon]
Meter=Image
X=(#W#-90*#scale#)
Y=(-25*#scale#)r
W=(50*#Scale#)
H=(50*#Scale#)
ImageName=#@#Icons\JD Weather Icons\[mDay1Icon].png
DynamicVariables=1
UpdateDivider=1
MeterStyle=WeatherIcon:#TintIcons#
[ForcastText]
Meter=String
X=(25*#scale#)r
Y=r
Text=[mDay1WeekdayShort]
FontSize=((12*#scale#)*#FontSizeModifier#)
DynamicVariables=1
MeterStyle=RegularText
StringAlign=CenterBottom
UpdateDivider=1
[ForcastTemperature]
Meter=String
X=r
Y=(50*#scale#)r
Text=[mDay1TemperatureMax] [mDay1TemperatureMin]
FontSize=((12*#scale#)*#FontSizeModifier#)
DynamicVariables=1
MeterStyle=RegularText
StringAlign=Center
UpdateDivider=1

[WeatherAnchor]
Meter=String
Y=(55*#Scale#)r
DynamicVariables=1

; Weather parser is from Jelle Dekker's weather skin. Check them out here! https://github.com/Jelle-Dekkers/JDWS-02
