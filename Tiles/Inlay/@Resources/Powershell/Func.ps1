function Update {

}

function GenerateProgramList {
    param (
    [int]$rows = 1
    )
    $SaveLocation = "$($RmAPI.VariableStr('ROOTCONFIGPATH'))Main\Accessories\Page\Variants\ProgramListCache.inc"
    $Path = $RmAPI.VariableStr('ProgramListDirectory')
    $Folders = $RmAPI.VariableStr('ListShowFolders')
    $Icons = $RmAPI.VariableStr('ListShowIcon')

    for (($i = 1); ($i -le $rows) ; $i++) {
        $fileContent += @"

[$($i)]
Meter=Shape
MeterStyle=Index:Style
[Index$($i)Icon]
Meter=Image
MeterStyle=Icon:Style | Icon$($Icons):Style
[Index$($i)Name]
Meter=String
MeterStyle=RegularText | Name:Style

"@

    }

    $fileContent += @"
[mPath]
Measure=Plugin
Plugin=FileView
Path="$Path"
Count=$rows
ShowFolder=$Folders
ShowDotDot=$Folders
HideExtensions=1
FinishAction=[!ShowMeterGroup List][!UpdateMeasureGroup Children][!UpdateMeter *][!Redraw]
[mFileCount]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileCount
Group=Children

"@

    for (($i = 1); ($i -le $rows) ; $i++) {
        $fileContent += @"

[mIndex$($i)Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=$($i)
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

"@
    }
    if ($Icons -eq 1) {
        for (($i = 1); ($i -le $rows) ; $i++) {
            $fileContent += @"

[mIndex$($i)Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=$($i)
Group=Children

"@
        }
    }

    $fileContent | Out-File -FilePath $SaveLocation -Encoding unicode
}

function GenerateDownloadsList {
    param (
    [int]$rows = 1
    )
    $SaveLocation = "$($RmAPI.VariableStr('ROOTCONFIGPATH'))Main\Accessories\Page\Variants\DownloadsListCache.inc"
    $Folders = $RmAPI.VariableStr('ListShowFolders')
    $Icons = $RmAPI.VariableStr('ListShowIcon')

    for (($i = 1); ($i -le $rows) ; $i++) {
        $fileContent += @"

[$($i)]
Meter=Shape
MeterStyle=Index:Style
[Index$($i)Icon]
Meter=Image
MeterStyle=Icon:Style | Icon$($Icons):Style
[Index$($i)Name]
Meter=String
MeterStyle=RegularText | Name:Style

"@

    }

    $fileContent += @"
[mPath]
Measure=Plugin
Plugin=FileView
Path="%USERPROFILE%\Downloads"
Count=$rows
ShowDotDot=$Folders
SortType=Date
ShowFile=1
ShowFolder=$Folders
ShowSystem=0
ShowHidden=0
SortAscending=0
FinishAction=[!UpdateMeasureGroup Children][!ShowMeterGroup List][!UpdateMeter *][!Redraw]

[mFileCount]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileCount
Group=Children
[mFolderSize]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FolderSize
Group=Children
[mFolderSizeCalc]
Measure=Calc
Formula=mFolderSize/1048576
DynamicVariables=1
Group=Children

"@

    for (($i = 1); ($i -le $rows) ; $i++) {
        $fileContent += @"

[mIndex$($i)Name]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=FileName
Index=$($i)
Group=Children
RegexpSubstitute=1
Substitute="\.\.":"Up 1 directory"

"@
    }
    if ($Icons -eq 1) {
        for (($i = 1); ($i -le $rows) ; $i++) {
            $fileContent += @"

[mIndex$($i)Icon]
Measure=Plugin
Plugin=FileView
Path=[mPath]
Type=Icon
IconSize=#IconSize#
Index=$($i)
Group=Children

"@
        }
    }

    $fileContent | Out-File -FilePath $SaveLocation -Encoding unicode
}
