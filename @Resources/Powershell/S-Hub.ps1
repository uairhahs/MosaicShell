# ---------------------------------------------------------------------------- #
#                                    Actions                                   #
# ---------------------------------------------------------------------------- #

function Get-CoreRoot {
    $skins = $RmAPI.VariableStr('SKINSPATH').TrimEnd('\')
    foreach ($name in @('#MosaicShell', 'MosaicShell')) {
        $c = Join-Path $skins $name
        if (Test-Path -LiteralPath (Join-Path $c 'S-Hub')) { return $c }
    }
    return (Join-Path $skins '#MosaicShell')
}

function Get-ExportsDir {
    $skins = $RmAPI.VariableStr('SKINSPATH').TrimEnd('\')
    return (Join-Path (Split-Path $skins -Parent) 'CoreData\S-Hub\Exports')
}

Function Initiate {
    $SKINSPATH = $RmAPI.VariableStr('SKINSPATH').TrimEnd('\')
    $PROGRAMPATH = $RmAPI.VariableStr('SETTINGSPATH').TrimEnd('\')
    $script = Join-Path (Get-CoreRoot) 'S-Hub\shp-registerer.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        $RmAPI.Log("S-Hub: registerer not found at $script")
        return
    }
    # Registry association requires elevation
    Start-Process powershell.exe -Verb RunAs -ArgumentList ("-ExecutionPolicy Bypass -noprofile -file `"$script`" -corepath `"$SKINSPATH`" -rmpath `"$PROGRAMPATH`" -elevated")
}

Function Pack {
    $script = Join-Path (Get-CoreRoot) 'S-Hub\getlatest-packager.ps1'
    if (-not (Test-Path -LiteralPath $script)) {
        $RmAPI.Log("S-Hub: packager not found at $script")
        return
    }
    # Packager itself does not need elevation; register first so Rainmeter is on PATH
    Start-Process powershell.exe -ArgumentList ("-ExecutionPolicy Bypass -noexit -noprofile -file `"$script`"")
}

Function OpenExports {
    $dir = Get-ExportsDir
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Start-Process explorer.exe -ArgumentList "`"$dir`""
}