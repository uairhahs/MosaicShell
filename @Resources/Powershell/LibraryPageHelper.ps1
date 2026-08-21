$skinList = $RmAPI.VariableStr('SkinList')
$SkinArray = $SkinList -split '\s\|\s'
$resources = $RmAPI.VariableStr('@')
$skinspath = $RmAPI.VariableStr('Skinspath')
$mosaicRoot = Join-Path $skinspath '#MosaicShell'

function rmlog ($Text) {
  $RmAPI.Log($Text)
}
function rmbang ($bang) {
    $RmAPI.Bang($bang)
}

function Test-TileInstalled($name) {
    $candidates = @(
        (Join-Path $skinspath $name),
        (Join-Path $mosaicRoot "Tiles\$name"),
        (Join-Path $skinspath "Tiles\$name")
    )
    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath $path) { return $true }
    }
    return $false
}

# ---------------------------------------------------------------------------- #
#                                    Process                                   #
# ---------------------------------------------------------------------------- #

rmlog "Starting library page update..."
for ($i=0; $i -lt $SkinArray.Count; $i++) {
    $i_name = $SkinArray[$i].Trim()
    if ([string]::IsNullOrWhiteSpace($i_name)) { continue }
    If (Test-TileInstalled $i_name) {
        rmbang "[!SetOption $i_name.Name.String MeterStyle `"Set.String:S | ListItem.Name.String:S | ListItem.Name.String:Installed`"][!SetOption $i_name.Button.String MeterStyle `"Set.String:S | ListItem.Button.String:S | ListItem.Button.String:Installed`"][!SetOption $i_name.Image MeterStyle `"ListItem.Image:S | ListItem.Image:Installed`"]"
    }
}
rmbang "[!UpdateMeterGroup List][!ShowMeterGroup List][!Redraw]"
