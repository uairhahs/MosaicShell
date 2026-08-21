# ---------------------------------------------------------------------------- #
#                                   Functions                                  #
# ---------------------------------------------------------------------------- #

function Get-IniContent ($filePath) {
    $ini = [ordered]@{}
    if (![System.IO.File]::Exists($filePath)) {
        throw "$filePath invalid"
    }
    $section = ';ItIsNotAFuckingSection;'
    $ini.Add($section, [ordered]@{})

    foreach ($line in [System.IO.File]::ReadLines($filePath)) {
        if ($line -match "^\s*\[(.+?)\]\s*$") {
            $section = $matches[1]
            $secDup = 1
            while ($ini.Keys -contains $section) {
                $section = $section + '||ps' + $secDup
            }
            $ini.Add($section, [ordered]@{})
        }
        elseif ($line -match "^\s*;.*$") {
            $notSectionCount = 0
            while ($ini[$section].Keys -contains ';NotSection' + $notSectionCount) {
                $notSectionCount++
            }
            $ini[$section][';NotSection' + $notSectionCount] = $matches[1]
        }
        elseif ($line -match "^\s*(.+?)\s*=\s*(.+?)$") {
            $key, $value = $matches[1..2]
            $ini[$section][$key] = $value
        }
        else {
            $notSectionCount = 0
            while ($ini[$section].Keys -contains ';NotSection' + $notSectionCount) {
                $notSectionCount++
            }
            $ini[$section][';NotSection' + $notSectionCount] = $line
        }
    }

    return $ini
}

function Set-IniContent($ini, $filePath) {
    $str = @()

    foreach ($section in $ini.GetEnumerator()) {
        if ($section -ne ';ItIsNotAFuckingSection;') {
            $str += "[" + ($section.Key -replace '\|\|ps\d+$', '') + "]"
        }
        foreach ($keyvaluepair in $section.Value.GetEnumerator()) {
            if ($keyvaluepair.Key -match "^;NotSection\d+$") {
                $str += $keyvaluepair.Value
            }
            else {
                $str += $keyvaluepair.Key + "=" + $keyvaluepair.Value
            }
        }
    }

    $finalStr = $str -join [System.Environment]::NewLine

    $finalStr | Out-File -filePath $filePath -Force -Encoding unicode
}

function debug {
    param(
        [Parameter()]
        [string]
        $message
    )

    $RmAPI.Bang("[!Log `"`"`"Search: " + $message + "`"`"`" Debug]")
}

# ---------------------------------------------------------------------------- #
#                                    Actions                                   #
# ---------------------------------------------------------------------------- #

function SearchForString($string) {
    $skin = $RmAPI.VariableStr('Skin.Name')
    $skinspath = $RmAPI.VariableStr('SKINSPATH')
    $skincoredir = "$skinspath$skin\Core\"
    $layout = $RmAPI.VariableStr('Layout')
    $style = $RmAPI.VariableStr('Style')

    debug "Skin directory: $skincoredir"

    $files = @(Get-ChildItem -Path "$skincoredir" -Directory -Recurse -Filter "SubpageOverrides" | Get-ChildItem -File)
    $vallimodules = $files
    $files += @(Get-ChildItem -Path "$skincoredir" -File | Where-Object Name -match "Appearance.inc|General.inc|Modules.inc|Colorscheme.inc|Layout.inc|Position.inc|Animation.inc")
    $files += @(Get-ChildItem -Path "$skincoredir\General" -File -Filter "*.inc")
    $files += @(Get-ChildItem -Path "$skincoredir\Appearance" -File -Filter "*.inc")
    If ([bool]$layout) {
        $files += @(Get-ChildItem -Path "$skincoredir" -Recurse -Filter "$layout.inc")
    } elseif ([bool]$style) {
        $files += @(Get-ChildItem -Path "$skincoredir" -Recurse -Filter "$style.inc")
    }
    $i = 0

    $files | ForEach-Object {
        $Ini = Get-IniContent $_.FullName

        $writesubpagemodule = $false
        $writesubpage = $false
        If ($($_.FullName | Split-Path | Split-Path -leaf) -match '^Appearance$|^General$') {
            $writesubpage = $true
            $subpage = $_ -replace '\..+$',''
            $page = $_.FullName | Split-Path | Split-Path -leaf
        } elseif ($vallimodules -contains $_) {
            $writesubpagemodule = $true
            $subpagemodule = $_ -replace '\..+$',''
            $subpage = '2'
            $page = 'Modules'
        } elseif ($_ -match "^$style.inc$|^$layout.inc$") {
            $page = 'Appearance'
        } else {
            $page = $_ -replace '\..+$',''
        }
        $breakthis = $false
        debug ">> Reading $_ <<"

        [string[]]$Ini.Keys | Where-Object { $_ -notmatch "^Option.*$" -or $Ini[$_]["Text"] -notmatch ".*$string.*" } | ForEach-Object {
            If ($Ini.Count -gt 1) {
                $Ini.Remove($_)
            } else {
                debug "Found 0"
                $breakthis = $true
            }
        }
        If (-not $breakthis) {
            debug "Found $($Ini.Count)"
            [string[]]$Ini.Keys | ForEach-Object {
                $i++
                $i_section = $_
                $i_string = $Ini[$_]["Text"] -replace '["]',''
                $i_string -match $string
                $i_match = $matches[0]
                debug "`"$i_string`" under [$i_section] in $page"
                # ---------------------------------- Action ---------------------------------- #
                $i_buttonaction = "[!WriteKeyvalue Variables Skin.Set_Page $page `"#@#CacheVars\Configurator.inc`"][!Refresh `"#MosaicShell\Main`"]"
                If ($writesubpage) {
                    $i_buttonaction += "[!WriteKeyvalue Variables Page.page $subpage `"$($skincoredir)$page.inc`"]"
                } elseif ($writesubpagemodule) {
                    $i_buttonaction += "[!WriteKeyvalue Variables Page.page $subpage `"$($skincoredir)$page.inc`"][!WriteKeyvalue Variables Page.SubpageModule $subpagemodule `"$($skincoredir)$page.inc`"]"
                }
                $i_buttonaction += "[!Delay 50][!SetOption `"$i_section`" InlineSetting `"GradientColor | 45 | #SEt.Accent_Color# ; 1 | #Set.Accent_color_2# ;0`" `"#MosaicShell\Main`"][!SetOption `"$i_section`" InlineSetting2 `"Weight | 600`" `"#MosaicShell\Main`"][!UpdateMeter `"$i_section`" `"#MosaicShell\Main`"][!Redraw `"#MosaicShell\Main`"][!CommandMeasure mActions `"Execute 2`"]"
                $RmAPI.Bang("[!SetOption Result$i Text `"$i_string $page`"][!SetOption Result$i InlinePattern `"$i_match`"][!SetOption Result$i InlinePattern2 `"$page$`"][!SetOption Result$i InlinePattern3 `"$page$`"][!SetOption Result$i LeftMouseUpAction `"`"`"$i_buttonaction`"`"`"][!ShowMeter Result$i]")
            }
        } else {
            Return
        }
    }
    debug "Search finished with $i results"
    If ($i -gt 0) {
        $RmAPI.Bang("[!SetVariable Process.Result_Count $i][!UpdateMeter *][!Redraw]")
    } else {
        $RmAPI.Bang("[!SetOption Result1 Text `"No matching commands or results`"][!ShowMeter Result1][!SetVariable Process.Result_Count 1][!UpdateMeter *][!Redraw]")
    }
}
