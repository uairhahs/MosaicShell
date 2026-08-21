[Variables]
WeatherUnitsMetric=m
WeatherUnitsImperial=e
Substitute="^null$":"", '^"(.*)"$':"\1", "^\\1$":""
SubstituteIcons="^0$":"storm", "^1$":"storm", "^2$":"storm", "^3$":"storm", "^4$":"storm", "^5$":"sleet", "^6$":"sleet", "^7$":"sleet", "^8$":"sleet", "^9$":"rain", "^10$":"sleet", "^11$":"rain", "^12$":"rain", "^13$":"snow", "^14$":"snow", "^15$":"snow", "^16$":"snow", "^17$":"sleet", "^18$":"sleet", "^19$":"fog", "^20$":"fog", "^21$":"fog", "^22$":"fog", "^23$":"windy", "^24$":"windy", "^25$":"snow", "^26$":"cloudy", "^27$":"cloudy-night", "^28$":"cloudy-day", "^29$":"cloudy-night", "^30$":"cloudy-day", "^31$":"clear-night", "^32$":"clear-day", "^33$":"partly-cloudy-night", "^34$":"partly-cloudy-day", "^35$":"sleet", "^36$":"clear-day", "^37$":"storm-day", "^38$":"storm-day", "^39$":"rain", "^40$":"rain", "^41$":"snow-day", "^42$":"snow", "^43$":"snow", "^44$":"unknown", "^45$":"rain-night", "^46$":"snow-night", "^47$":"storm-night", "^na$":"", "":"unknown"

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

URL=https://nominatim.openstreetmap.org/search?q=#gVar.WeatherLocation#&format=geocodejson&limit=1&addressdetails=1&accept-language=#gVar.Language#
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

FinishAction=[!EnableMeasureGroup Current] [!UpdateMeasureGroup Current] [!UpdateMeter *] [!UpdateMeter *] [!Redraw] [!Log "Finished parsing current weather data." Notice]
OnConnectErrorAction=[!Log "Unable to connect to Weather.com." Error]
OnRegExpErrorAction=[!Log "Something went wrong." Error]

Disabled=1
DynamicVariables=1

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
