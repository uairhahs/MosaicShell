# ---------------------------------------------------------------------------- #
#                                    Actions                                   #
# ---------------------------------------------------------------------------- #

Function Initiate {
    $SKINSPATH = $RmAPI.VariableStr('SKINSPATH').TrimEnd('\')
    $PROGRAMPATH = $RmAPI.VariableStr('SETTINGSPATH').TrimEnd('\')
    Start-Process powershell.exe -Verb RunAs -ArgumentList ("-ExecutionPolicy Bypass -noprofile -file `"$SKINSPATH\#MosaicShell\S-Hub\shp-registerer.ps1`" -corepath `"$SKINSPATH`" -rmpath `"$PROGRAMPATH`" -elevated")
}

Function Pack {
    $SKINSPATH = $RmAPI.VariableStr('SKINSPATH').TrimEnd('\')
    Start-Process powershell.exe -Verb RunAs -ArgumentList ("-ExecutionPolicy Bypass -noexit -noprofile -file `"$SKINSPATH\#MosaicShell\S-Hub\getlatest-packager.ps1`"")
}
