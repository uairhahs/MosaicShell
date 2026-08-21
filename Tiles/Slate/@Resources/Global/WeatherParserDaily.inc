[Variables]
RegExpDaily=(?siU){"class":.*,"expire_time_gmt":.*,"fcst_valid":.*,"fcst_valid_local":(.*),"num":.*,"max_temp":(.*),"min_temp":(.*),"torcon":.*,"stormcon":.*,"blurb":.*,"blurb_author":.*,"lunar_phase_day":.*,"dow":.*,"lunar_phase":(.*),"lunar_phase_code":(.*),"sunrise":(.*),"sunset":(.*),"moonrise":(.*),"moonset":(.*),"qualifier_code":.*,"qualifier":.*,"narrative":(.*),"qpf":.*,"snow_qpf":.*,"snow_range":.*,"snow_phrase":.*,"snow_code":.*,"night":{"fcst_valid":.*,"fcst_valid_local":.*,"day_ind":.*,"thunder_enum":.*,"daypart_name":(.*),"long_daypart_name":.*,"alt_daypart_name":.*,"thunder_enum_phrase":.*,"num":.*,"temp":.*,"hi":.*,"wc":.*,"pop":(.*),"icon_extd":.*,"icon_code":(.*),"wxman":.*,"phrase_12char":.*,"phrase_22char":.*,"phrase_32char":(.*),"subphrase_pt1":.*,"subphrase_pt2":.*,"subphrase_pt3":.*,"precip_type":.*,"rh":(.*),"wspd":(.*),"wdir":(.*),"wdir_cardinal":(.*),"clds":(.*),"pop_phrase":.*,"temp_phrase":.*,"accumulation_phrase":.*,"wind_phrase":.*,"shortcast":.*,"narrative":.*,"qpf":.*,"snow_qpf":.*,"snow_range":.*,"snow_phrase":.*,"snow_code":.*,"vocal_key":.*,"qualifier_code":.*,"qualifier":.*,"uv_index_raw":.*,"uv_index":(.*),"uv_warning":.*,"uv_desc":(.*),"golf_index":.*,"golf_category":.*}(?(?=,"day"),"day":{"fcst_valid":.*,"fcst_valid_local":.*,"day_ind":.*,"thunder_enum":.*,"daypart_name":(.*),"long_daypart_name":.*,"alt_daypart_name":.*,"thunder_enum_phrase":.*,"num":.*,"temp":.*,"hi":.*,"wc":.*,"pop":(.*),"icon_extd":.*,"icon_code":(.*),"wxman":.*,"phrase_12char":.*,"phrase_22char":.*,"phrase_32char":(.*),"subphrase_pt1":.*,"subphrase_pt2":.*,"subphrase_pt3":.*,"precip_type":.*,"rh":(.*),"wspd":(.*),"wdir":(.*),"wdir_cardinal":(.*),"clds":(.*),"pop_phrase":.*,"temp_phrase":.*,"accumulation_phrase":.*,"wind_phrase":.*,"shortcast":.*,"narrative":.*,"qpf":.*,"snow_qpf":.*,"snow_range":.*,"snow_phrase":.*,"snow_code":.*,"vocal_key":.*,"qualifier_code":.*,"qualifier":.*,"uv_index_raw":.*,"uv_index":(.*),"uv_warning":.*,"uv_desc":(.*),"golf_index":.*,"golf_category":.*})}

SubstituteMoonPhaseIcons="^N$":"new-moon", "^WXC$":"waxing-crescent", "^FQ$":"first-quarter", "^WXG$":"waxing-gibbous", "^F$":"full-moon", "^WNG$":"waning-gibbous", "^LQ$":"third-quarter", "^WNC$":"waning-crescent", "^$":"unknown"

TimeFormat24HTime0=%#I:%M %p
TimeFormat24HTime1=%H:%M

[mDailyParent]
Group=Parent
Measure=WebParser

URL=https://api.weather.com/v1/geocode/[&mLocationLat]/[&mLocationLon]/forecast/daily/7day.json?units=[#WeatherUnits[#gVar.WeatherUnits]]&language=#gVar.Language#&apiKey=#gVar.WeatherAPIKey#
RegExp=(?siU)"forecasts":\[({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}}),({"class":.*"golf_category":.*}})\]
UserAgent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36
UpdateRate=1

FinishAction=[!EnableMeasureGroup Daily] [!UpdateMeasureGroup Daily] [!UpdateMeter *] [!UpdateMeter *] [!Redraw] [!Log "Finished parsing daily weather data." Notice]
OnConnectErrorAction=[!EnableMeasureGroup Daily] [!UpdateMeasureGroup Daily] [!UpdateMeter *] [!UpdateMeter *] [!Redraw] [!Log "Unable to connect to Weather.com." Error]
OnRegExpErrorAction=[!EnableMeasureGroup Daily] [!UpdateMeasureGroup Daily] [!UpdateMeter *] [!UpdateMeter *] [!Redraw] [!Log "Something went wrong." Error]

DynamicVariables=1
UpdateDivider=-1
Disabled=1

; ================================================

[mCheckDay0DayOrNight]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=24
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", ".+":"Day", "":"Night"

; ================================================

[mDay0WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay0Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay0WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay0WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay0TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay0MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay0SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay0Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay0Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay0Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay0Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay0Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay0Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay0Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay0Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay0Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay0Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay0Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay0Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay0Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay0Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay0Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay0Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay0Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay0Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay0Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay0Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay0NightDaypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=11

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0NightPrecipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=12

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay0NightIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=13

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay0NightPhrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=14

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay0NightHumidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=15

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay0NightWindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=16

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0NightWindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=17

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay0NightWindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay0NightWindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay0NightWindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=18

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0NightCloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=19

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay0NightUVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=20

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0NightUVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=21

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay0DayDaypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=22
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0DayPrecipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=23
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay0DayIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=24
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay0DayPhrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=25
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay0DayHumidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=26
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay0DayWindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=27
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0DayWindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=28
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay0DayWindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay0DayWindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay0DayWindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=29
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0DayCloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=30
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay0DayUVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=31
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay0DayUVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=1
RegExp=#RegExpDaily#
StringIndex2=32
LogSubstringErrors=0

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay0Daypart]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]Daypart]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0Precipitation]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]Precipitation]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0Icon]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]Icon]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0Phrase]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]Phrase]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0Humidity]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]Humidity]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0WindSpeed]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]WindSpeed]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0WindDirection]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]WindDirection]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0WindDirectionIcon]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]WindDirectionIcon]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0WindDirectionCardinal]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]WindDirectionCardinal]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0CloudCoverage]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]CloudCoverage]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0UVIndex]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]UVIndex]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay0UVDescription]
Group=Daily
Measure=String

String=[&mDay0[&mCheckDay0DayOrNight]UVDescription]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

; ================================================

[mDay1WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay1Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay1WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

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

[mDay1MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay1MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay1SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay1Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay1Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay1Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay1Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay1Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay1SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay1Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay1Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay1Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay1Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay1Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay1MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay1Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay1Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay1Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay1Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay1Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay1MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay1Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay1Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay1Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay1Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay1Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay1Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay1Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay1Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay1Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay1Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay1Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay1WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay1WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay1WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay1WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay1WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay1CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay1UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay1UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=2
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay2WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay2Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay2WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay2WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay2WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay2TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay2TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay2MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay2MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay2SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay2Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay2Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay2Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay2Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay2Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay2SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay2Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay2Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay2Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay2Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay2Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay2MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay2Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay2Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay2Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay2Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay2Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay2MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay2Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay2Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay2Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay2Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay2Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay2Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay2Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay2Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay2Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay2Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay2Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay2WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay2WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay2WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay2WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay2WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay2CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay2UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay2UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=3
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay3WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay3Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay3WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay3WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay3WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay3TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay3TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay3MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay3MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay3SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay3Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay3Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay3Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay3Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay3Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay3SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay3Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay3Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay3Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay3Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay3Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay3MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay3Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay3Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay3Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay3Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay3Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay3MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay3Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay3Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay3Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay3Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay3Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay3Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay3Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay3Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay3Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay3Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay3Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay3WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay3WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay3WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay3WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay3WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay3CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay3UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay3UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=4
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay4WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay4Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay4WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay4WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay4WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay4TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay4TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay4MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay4MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay4SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay4Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay4Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay4Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay4Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay4Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay4SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay4Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay4Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay4Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay4Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay4Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay4MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay4Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay4Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay4Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay4Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay4Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay4MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay4Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay4Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay4Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay4Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay4Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay4Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay4Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay4Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay4Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay4Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay4Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay4WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay4WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay4WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay4WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay4WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay4CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay4UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay4UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=5
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay5WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay5Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay5WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay5WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay5WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay5TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay5TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay5MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay5MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay5SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay5Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay5Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay5Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay5Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay5Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay5SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay5Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay5Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay5Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay5Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay5Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay5MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay5Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay5Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay5Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay5Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay5Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay5MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay5Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay5Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay5Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay5Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay5Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay5Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay5Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay5Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay5Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay5Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay5Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay5WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay5WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay5WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay5WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay5WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay5CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay5UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay5UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=6
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay6WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay6Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay6WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay6WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay6WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay6TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay6TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay6MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay6MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay6SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay6Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay6Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay6Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay6Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay6Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay6SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay6Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay6Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay6Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay6Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay6Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay6MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay6Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay6Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay6Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay6Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay6Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay6MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay6Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay6Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay6Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay6Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay6Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay6Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay6Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay6Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay6Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay6Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay6Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay6WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay6WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay6WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay6WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay6WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay6CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay6UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay6UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=7
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

; ================================================

[mDay7WeekdayRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=1

RegExpSubstitute=1
Substitute="#Substitute#"

[mDay7Weekday]
Group=Daily
Measure=Time

TimeStamp=[mDay7WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%A
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay7WeekdayShort]
Group=Daily
Measure=Time

TimeStamp=[mDay7WeekdayRAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=%a
FormatLocale=#gVar.Language#

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay7TemperatureMax]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=2

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay7TemperatureMin]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=3

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1°", "":"--"

[mDay7MoonPhase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=4

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay7MoonPhaseIcon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=5

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteMoonPhaseIcons#"

[mDay7SunriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=6

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay7Sunrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay7Sunrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay7Sunrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay7Sunrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay7Sunrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay7SunsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=7

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay7Sunset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay7Sunset Format "N/A"]
IfNotMatchAction=[!SetOption mDay7Sunset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay7Sunset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay7Sunset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay7MoonriseRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=8

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay7Moonrise TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay7Moonrise Format "N/A"]
IfNotMatchAction=[!SetOption mDay7Moonrise TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay7Moonrise Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay7Moonrise]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay7MoonsetRAW]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=9

RegExpSubstitute=1
Substitute="#Substitute#"

IfMatch=^$
IfMatchAction=[!SetOption mDay7Moonset TimeStamp "2020-01-01T00:00:00+0000"] [!SetOption mDay7Moonset Format "N/A"]
IfNotMatchAction=[!SetOption mDay7Moonset TimeStamp "[*#CurrentSection#*]"] [!SetOption mDay7Moonset Format "[*#TimeFormat24HTime#24HTime#*]"]

[mDay7Moonset]
Group=Daily
Measure=Time

TimeStamp=[#CurrentSection#RAW]
TimeStampFormat=%Y-%m-%dT%H:%M:%S
Format=[#TimeFormat24HTime#24HTime#]

Disabled=1
UpdateDivider=-1
DynamicVariables=1

[mDay7Narrative]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=10

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

; ================================================

[mDay7Daypart]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=22

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay7Precipitation]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=23

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay7Icon]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=24

RegExpSubstitute=1
Substitute="#Substitute#", "#SubstituteIcons#"

[mDay7Phrase]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=25

RegExpSubstitute=1
Substitute="#Substitute#", "":"N/A"

[mDay7Humidity]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=26

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay7WindSpeed]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=27

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay7WindDirection]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=28

RegExpSubstitute=1
Substitute="#Substitute#", "":"0"

[mDay7WindDirectionIcon]
Group=Daily
Measure=Calc

Formula=(Round([mDay7WindDirection]/15)*15)%360

Disabled=1
UpdateDivider=-1
DynamicVariables=1

RegExpSubstitute=1
Substitute="(.*)":"from-\1"

[mDay7WindDirectionCardinal]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=29

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay7CloudCoverage]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=30

RegExpSubstitute=1
Substitute="#Substitute#", "(.+)":"\1%", "":"--"

[mDay7UVIndex]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=31

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"

[mDay7UVDescription]
Group=Daily
Measure=WebParser

URL=[mDailyParent]
StringIndex=8
RegExp=#RegExpDaily#
StringIndex2=32

RegExpSubstitute=1
Substitute="#Substitute#", "":"--"
