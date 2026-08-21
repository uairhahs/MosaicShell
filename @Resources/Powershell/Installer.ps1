$ErrorActionPreference = 'SilentlyContinue'
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

    [System.IO.File]::WriteAllText($filePath, $finalStr, [System.Text.UTF8Encoding]::new($true))
}

function debug {
    param(
        [Parameter()]
        [string]
        $message
    )

    $RmAPI.Bang("[!Log `"`"`"CoreInstaller: " + $message + "`"`"`" Debug]")
}

function post-prog {
    param(
        [Parameter()]
        [string]
        $message
    )
    $RmAPI.Bang("[!SetVariable InstallText `"`"`"" + $message + "`"`"`"][!UpdateMeterGroup Progress][!Redraw]")

}

function Get-Webfile ($url, $dest) {
    debug "Downloading $url`n"
    $uri = New-Object "System.Uri" "$url"
    $request = [System.Net.HttpWebRequest]::Create($uri)
    $request.set_Timeout(5000)
    $response = $request.GetResponse()
    $length = $response.get_ContentLength()
    $responseStream = $response.GetResponseStream()
    $destStream = New-Object -TypeName System.IO.FileStream -ArgumentList $dest, Create
    $buffer = New-Object byte[] 100KB
    $count = $responseStream.Read($buffer, 0, $buffer.length)
    $downloadedBytes = $count

    while ($count -gt 0) {
        $RmAPI.Bang("[!CommandMeasure s.CircleBarHelper `"DrawCircleBar($([System.Math]::Round(($downloadedBytes / $length) * 100,0)))`"][!SetOption ProgressBar.String Text `"$([System.Math]::Round(($downloadedBytes / $length) * 100,0))%`"][!UpdateMeterGroup Progress][!Redraw]")
        $destStream.Write($buffer, 0, $count)
        $count = $responseStream.Read($buffer, 0, $buffer.length)
        $downloadedBytes += $count
    }

    debug "`nDownload of `"$dest`" finished."
    $destStream.Flush()
    $destStream.Close()
    $destStream.Dispose()
    $responseStream.Dispose()
}

function New-Cache {

    [System.IO.Directory]::CreateDirectory("$root") | Out-Null
    Get-Item $root -Force | foreach { $_.Attributes = $_.Attributes -bor "Hidden" }
    Get-ChildItem "$root" | ForEach-Object {
        Remove-Item $_.FullName -Force -Recurse
    }
}

# ---------------------------------------------------------------------------- #
#                                 Start install                                #
# ---------------------------------------------------------------------------- #

function Install($installModuleObj, $Version) {
    # ------------------------- Convert object to string ------------------------- #
    If ($installModuleObj.Count -gt 1) {
        $installModule = '@('
        $installModuleObj = $installModuleObj | Foreach-Object { "'"+$_+"'" }
        $installModule += $installModuleObj -join ', '
        $installModule += ')'
    } else {
        $installModule = "'$installModuleObj'"
    }
    # ---------------------------------- Command --------------------------------- #
    $command = "-command `"`$o_FromCore=`$true;`$o_InstallModule=$installModule;"
    $rminstalledat = join-path "$($RmAPI.VariableStr('SETTINGSPATH'))" ""
    # If installation is portable
    If (!((join-path "$Env:APPDATA\Rainmeter\" "") -eq ($rminstalledat))) {
        $rminstalledat = Split-path "$rminstalledat"
        $command += "`$o_Location='$rminstalledat';"
    }
    # If specific version is requested
    If ($Version) {
        $Version = $Version -replace 'v', ''
        $command += "`$o_Version='$Version';"
    }
    If ($RmAPI.VariableStr('Set.UseExtInstaller') -eq '1') {
        $command += "`$o_ExtInstall=`$true;"
    }
    If ($RmAPI.VariableStr('Set.UseBetaInstaller') -eq '1') {
        $command += "iwr -useb 'https://raw.githubusercontent.com/uairhahs/MosaicShell/main/CoreInstallerBeta.ps1' | iex`""
    } else {
        $command += "iwr -useb 'https://raw.githubusercontent.com/uairhahs/MosaicShell/main/CoreInstaller.ps1' | iex`""
    }
    # --------------------------------- Fallback --------------------------------- #
    $RmAPI.Bang("[!CommandMeasure Func `"startOverlay('InstallFallback')`"]")
    # ---------------------------- Launch PS instance ---------------------------- #
    If (Test-Path "C:\Windows\System32\WindowsPowershell\v1.0\powershell.exe") {
        Start-Process "C:\Windows\System32\WindowsPowershell\v1.0\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass", $command
    } else {
        Start-Process powershell.exe -ArgumentList "-ExecutionPolicy Bypass", $command
    }
}

function InstallBatch () {
    $RmAPI.Bang("[!CommandMeasure Func `"startOverlay('InstallBatchFallback')`"]")
    Install $global:batchinstall_list
    # $RmAPI.Bang('[!DeactivateConfig]')
}

# ---------------------------------------------------------------------------- #
#                                 Batch install                                #
# ---------------------------------------------------------------------------- #

function BatchInstall-CreateList {
    [System.Collections.ArrayList]$global:batchinstall_list = @('YourFlyouts', 'YourMixer')
}

function BatchInstall-AddToList($name) {
    $global:batchinstall_list.Add("$name")
    debug $global:batchinstall_list
}

function BatchInstall-RemoveFromList($name) {
    $global:batchinstall_list.Remove("$name")
    debug $global:batchinstall_list
}

function CheckAvailableUpdates {
    $skinList = $RmAPI.VariableStr('SkinList')
    $skinspath = $RmAPI.VariableStr('Skinspath')
    $SkinArray = $SkinList -split '\s\|\s'
    [System.Collections.ArrayList]$global:batchinstall_list = @()

    for ($i=0; $i -lt $SkinArray.Count; $i++) {
        $moduleName = $SkinArray[$i]
        If (Test-Path -Path "$($skinspath)$moduleName\") {
            try {
                $latest_v = '0.0'
                $urls = @(
                    "https://raw.githubusercontent.com/uairhahs/$moduleName/main/%40Resources/Version.inc",
                    "https://raw.githubusercontent.com/MosaicShell/$moduleName/main/%40Resources/Version.inc",
                    "https://raw.githubusercontent.com/Jax-Core/$moduleName/main/%40Resources/Version.inc"
                )
                foreach ($url in $urls) {
                    try {
                        $response = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 3
                        $content = [System.Text.Encoding]::UTF8.GetString($response.RawContentStream.ToArray())
                        if ($content -match 'Version=(.+)') {
                            $latest_v = $matches[1].Trim()
                            break
                        }
                    } catch {}
                }
            } catch {
                debug "$moduleName repository does not exist or is hidden"
                $latest_v = '0.0'
            } finally {
                Get-Content "$($skinspath)$moduleName\@Resources\Version.inc" -Raw | Select-String -Pattern '\d\.\d+' -AllMatches | Foreach-Object {$local_v = $_.Matches.Value}
                debug "$moduleName ✔️ - 🔼 |$latest_v| 🔽 |$local_v|"
                if ($latest_v -gt $local_v) {
                    $global:batchinstall_list.Add("$moduleName")
                    $RmAPI.Bang("[!SetOption ProgressBar.String Text $($global:batchinstall_list.Count)][!SetOption ProgressBar.Title.String Text `"Required updates found`"][!UpdateMeterGroup Progress][!Redraw]")
                }
            }
        }
    }
    debug "$($global:batchinstall_list.Count) - $global:batchinstall_list"
    If ($global:batchinstall_list.Count -eq 0) {
        $RmAPI.Bang('[!SetOption Button2.String Text Back][!SetOption Button2.Shape LeftMouseUpAction """[!CommandMeasure ActionTimer "Execute 2"][!WriteKeyValue Variables Sec.Page 1 "#ROOTCONFIGPATH#Main\Home.ini"][!ActivateConfig "#MosaicShell\Main" "Home.ini"]"""][!SetOption ProgressBar.String Text 0][!SetOption ProgressBar.Title.String Text "Required updates found"][!UpdateMeterGroup Progress][!UpdateMeterGroup Button][!Redraw]')
    } else {
        $RmAPI.Bang('[!SetOption Button2.String Text "Install all"][!SetOption Button2.Shape LeftMouseUpAction """[!WriteKeyValue "#CURRENTCONFIG#" Active 0 "#SETTINGSPATH#Rainmeter.ini"][!CommandMeasure CoreInstallHandler "InstallBatch"]"""][!UpdateMeterGroup Button][!Redraw]')
    }
}

# ---------------------------------------------------------------------------- #
#                                    Actions                                   #
# ---------------------------------------------------------------------------- #

function Open-DALink {
    # DeviantArt removed... no-op kept so old bangs do not error
    $RmAPI.Log('Open-DALink: DeviantArt links have been removed')
}

function GenCoreData {
    $SkinsPath = $RmAPI.VariableStr('SKINSPATH')
    $SkinName = $RmAPI.VariableStr('Skin.Name')
    $SkinVer = $RmAPI.VariableStr('Version')
    If (Test-Path -Path "$SkinsPath..\CoreData") {
        $RmAPI.Log("Found coredata in programs")

        If (Test-Path -Path "$SkinsPath$SkinName\@Resources\@Structure") {
            $RmAPI.Log("Available CoreData structure for $SkinName")
            If (-not (Test-Path -Path "$SkinsPath..\CoreData\$SkinName\$SkinVer.txt")) {
                $RmAPI.Log("Generating: Can't find $SkinVer.txt file in CoreData of $SkinName")
                If (Test-Path "C:\Windows\system32\robocopy.exe") {
                    &"C:\Windows\system32\robocopy.exe" "$SkinsPath$SkinName\@Resources\@Structure" "$SkinsPath..\CoreData\$SkinName" /E /XC /XN /XO
                } else {
                    Robocopy "$SkinsPath$SkinName\@Resources\@Structure" "$SkinsPath..\CoreData\$SkinName" /E /XC /XN /XO
                }
                New-Item -Path "$SkinsPath..\CoreData\$SkinName" -Name "$SkinVer.txt" -ItemType "file"
            }
            else {
                $RmAPI.Log("CoreData for $SkinName is already generated")
            }
        }
        else {
            $RmAPI.Log("$SkinName doesn't require creation of CoreData")
        }
    }
    else {
        $RmAPI.Log("Failed to find coredata in programs, generating")
        New-Item -Path "$SkinsPath..\" -Name "CoreData" -ItemType "directory"
        $RmAPI.Bang('[!Refresh]')
    }
}

function DuplicateSkin {
    param (
        [string]$DuplicateName = 'CloneSkinName'
    )
    $SkinsPath = $RmAPI.VariableStr('SKINSPATH')
    $Resources = $RmAPI.VariableStr('@')
    $SkinName = $RmAPI.VariableStr('Arg.1')

    If (Test-Path -Path "$SkinsPath$DuplicateName") {
        $RmAPI.Log("Folder already exits")
    }
    else {
        $RmAPI.Log("Duplicating to $DuplicateName")
        Copy-Item -Path "$SkinsPath$SkinName\" -Destination "$SkinsPath$DuplicateName\" -Recurse
        New-Item -Path "$SkinsPath$DuplicateName\@Resources\" -Name "IsClone.txt" -ItemType "file"
    }
    $RmAPI.Bang("[!WriteKeyValue Rainmeter OnRefreshAction `"`"`"[!WriteKeyValue Rainmeter OnRefreshAction `"#*Sec.DefaultStartActions*#`"][!DeactivateConfig]`"`"`"][!WriteKeyValue Variables Skin.Name $DuplicateName `"$($Resources)CacheVars\Configurator.inc`"][!WriteKeyValue Variables Skin.Set_Page Info `"$($Resources)CacheVars\Configurator.inc`"][!WriteKeyValue `"#MosaicShell\Main`" Active 1 `"$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini`"][`"$($Resources)Addons\RestartRainmeter.exe`"]")

}

function Remove-Section($SkinName) {

    $Ini = Get-IniContent -filePath "$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini"
    $pattern = "^$SkinName"
    [string[]]$Ini.Keys | Where-Object { $_ -match $pattern } | ForEach-Object {
        debug "Detected section [$_] in Rainmeter.ini, removing"
        $Ini.Remove($_)
    }
    Set-IniContent -ini $Ini -filePath "$($RmAPI.VariableStr('SETTINGSPATH'))Rainmeter.ini"

}

function Uninstall {
    $SkinsPath = $RmAPI.VariableStr('SKINSPATH')
    $Resources = $RmAPI.VariableStr('@')
    $SkinName = $RmAPI.VariableStr('Arg.1')
    Stop-Process -Name 'AHKv1'
    If (Test-Path -Path "$SkinsPath..\CoreData\$SkinName") {
        Remove-Item -LiteralPath "$SkinsPath..\CoreData\$SkinName" -Force -Recurse
    }

    Remove-Item -LiteralPath "$SkinsPath$SkinName" -Force -Recurse
    Remove-Item -LiteralPath "$SkinsPath@Backup\$SkinName" -Force -Recurse

    Remove-Section $SkinName
    # -- Rainmeter might not restart if the uninstalled skin is not loaded once! - #

    # Start-Sleep -s 1
    $RmAPI.Bang("[!WriteKeyvalue Variables Sec.variant `"Variants\Uninstalled.inc`"][!WriteKeyValue Variables Skin.Name $SkinName `"$($Resources)CacheVars\Configurator.inc`"][!WriteKeyValue Variables Skin.Set_Page Info `"$($Resources)CacheVars\Configurator.inc`"][`"$($Resources)Addons\RestartRainmeter.exe`"]")
}
