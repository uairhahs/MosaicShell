$skinName = $RmAPI.VariableStr('Arg.1')
$skinVer = $RmAPI.VariableStr('Arg.2')
$resources = $RmAPI.VariableStr('@')

# $RmAPI.Log($RmAPI.VariableStr('LastRollBackSkin'))
# $RmAPI.Log("$skinName|$skinVer")
# if (-not ($RmAPI.VariableStr('LastRollBackSkin') -contains "$skinName|$skinVer")) {
if (-not ($RmAPI.VariableStr('Animation_Steps') -match '1')) {

    # ----------------------------- Clear error cache ---------------------------- #
    $RmAPI.Bang('[!HideMeterGroup List][!Redraw]')
    $error.clear()
    # --------------------------------- Variables -------------------------------- #
    $exportTo = $resources + 'Includes\RollbackList.inc'
    # $APIDump = $resources + 'Includes\APIDump.txt'
    $url = 'https://api.github.com/repos/MosaicShell/' + $skinName + '/releases'
    # --------------------------------- Parse API -------------------------------- #
    try {
        $a = Invoke-WebRequest -Uri $url -UseBasicParsing | ConvertFrom-Json
    }
    catch {
        # $RmAPI.Log('Please run internet explorer for the first time first.')
        $RmAPI.Bang('[!SetOption Header.String MeterStyle "Sec.String:S | Header.String:S"][!SetOption Header.Description MeterStyle "Sec.String:S | Header.Description:S"][!UpdateMeter *][!Redraw]')
    }
    # $skinJson = Get-Content -Path $APIDump
    # $a = $skinJson | convertfrom-json

    # --------------------------------- Generate --------------------------------- #
    $exportString = ''
    for ($i = 0; $i -lt $a.tag_name.Count; $i++) {
        $exportString += @"
[List.Item$i.Shape]
Meter=Shape
LeftMouseDownAction=[!DeactivateConfig "$skinName\Main"][!CommandMeasure CoreInstallHandler "Install $skinName $($a.tag_name[$i])"][!DeactivateConfig]
MeterStyle=List.Item.Shape:S
[List.Item$i.Title.String]
Meter=String
Text=$($a.name[$i])
MeterStyle=Sec.String:S | List.Item.Title.String:S
[List.Item$i.Date.String]
Meter=String
Text=$($a.published_at[$i] -replace '.{10}$','')
MeterStyle=Sec.String:S | List.Item.Date.String:S
[List.Item$i.Version.String]
Meter=String
Text=$($a.tag_name[$i])
MeterStyle=Sec.String:S | List.Item.Version.String:S

"@
    }

    $exportString | Out-File -filePath $exportTo -Force -Encoding unicode
    $RmAPI.Bang('[!WriteKeyValue Variables Animation_Steps "1" "' + $RmAPI.VariableStr('ROOTCONFIGPATH') + 'Accessories\GenericInteractionBox\Variants\Rollback.inc"][!Delay 1000][!Refresh]')
} else {
    $RmAPI.Bang('[!WriteKeyValue Variables Animation_Steps "25" "' + $RmAPI.VariableStr('ROOTCONFIGPATH') + 'Accessories\GenericInteractionBox\Variants\Rollback.inc"]')
    # [!WriteKeyValue Variables LastRollBackSkin "'+$skinName+'|'+$skinVer+'" "'+$resources+'Vars.inc"]
}
# }
