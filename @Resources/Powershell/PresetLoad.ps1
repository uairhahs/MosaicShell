$sectionTable = New-Object System.Collections.Generic.List[System.Object]

function ReadIni($filePath)
{
    $ini = @{}
    $content=Get-Content -Path $filePath
    switch -regex ($content)
    {
        “^\[(.+)\]” # Section
        {
            $section = $matches[1]
            $ini[$section] = @()
            $sectionTable.Add($section)
            $CommentCount = 0
        }
        “^(;.*)$” # Comment
        {
            $value = $matches[1]
            $CommentCount = $CommentCount + 1
            $name = “Comment” + $CommentCount
            # $ini[$section][$name] = $value
        }
        “(.+?)\s*=(.*)” # Key
        {
            $name,$value = $matches[1..2]
            $ini[$section] += @{
                Name = $name
                Value = $value
            }
        }
    }
    return $ini
}

function ChangeToFiles {
    param (
    [Parameter(Mandatory=$true)]
    [String]
    $theme = "DEFAULT"
    )
    $Skinpath = $RmAPI.VariableStr('SKINSPATH')
    $SkinName = $RmAPI.VariableStr('Skin.Name')
    if (!$SkinName) {
        $SkinName = '#MosaicShell'
    }

    $Defaultini = ReadIni("$Skinpath$SkinName\@Resources\Presets\DEFAULT.inc")
    $ini = ReadIni("$Skinpath$SkinName\@Resources\Presets\$theme.inc")
    $sectionTable.ToArray()
    for ($i = 0; $i -lt $Defaultini.Count; $i++) {
        $saveLocation = $sectionTable[$i]
        $RmAPI.Log("WRITING DEFAULT $saveLocation")
        for ($j = 0; $j -lt $Defaultini[$saveLocation].Count; $j++) {
            $RmAPI.Bang("[!WriteKeyValue Variables `"$($Defaultini[$saveLocation][$j].Name)`" `"$($Defaultini[$saveLocation][$j].Value)`" `"$Skinpath$SkinName\$saveLocation`"]")
            $RmAPI.Log("DEFAULT: $($Defaultini[$saveLocation][$j].Name)=$($Defaultini[$saveLocation][$j].Value)")
        }
    }
    for ($i = $Defaultini.Count; $i -lt $ini.Count + $Defaultini.Count; $i++) {
        $saveLocation2 = $sectionTable[$i]
        $RmAPI.Log("WRITING $saveLocation")
        for ($j = 0; $j -lt $ini[$saveLocation2].Count; $j++) {
            $RmAPI.Bang("[!WriteKeyValue Variables `"$($ini[$saveLocation2][$j].Name)`" `"$($ini[$saveLocation2][$j].Value)`" `"$Skinpath$SkinName\$saveLocation2`"]")
            $RmAPI.Log("$($ini[$saveLocation2][$j].Name)=$($ini[$saveLocation2][$j].Value)")
        }
    }
    $RmAPI.Log("Successfully written all to skin $SkinName")
}
