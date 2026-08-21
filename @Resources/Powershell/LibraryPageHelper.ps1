$skinList = $RmAPI.VariableStr('SkinList')
$SkinArray = $SkinList -split '\s\|\s'
$resources = $RmAPI.VariableStr('@')
$skinspath = $RmAPI.VariableStr('Skinspath')

function rmlog ($Text) {
  $RmAPI.Log($Text)
}
function rmbang ($bang) {
  $RmAPI.Bang($bang)
}

# ---------------------------------------------------------------------------- #
#                                    Process                                   #
# ---------------------------------------------------------------------------- #

rmlog "Starting library page update..."
for ($i=0; $i -lt $SkinArray.Count; $i++) {
    $i_name = $SkinArray[$i]
    If (Test-Path -Path "$skinspath$i_name\") {
        rmbang "[!SetOption $i_name.Name.String MeterStyle `"Set.String:S | ListItem.Name.String:S | ListItem.Name.String:Installed`"][!SetOption $i_name.Button.String MeterStyle `"Set.String:S | ListItem.Button.String:S | ListItem.Button.String:Installed`"][!SetOption $i_name.Image MeterStyle `"ListItem.Image:S | ListItem.Image:Installed`"]"
    }
}
rmbang "[!UpdateMeterGroup List][!ShowMeterGroup List][!Redraw]"

# if (-not ($RmAPI.VariableStr('Parsed') -match '1')) {
#     # Print-Skin "Converting to array"
#     # Print-Skin "Checking for installation"
#             try {
#                 Print-Skin "Checking remote $($SkinArray[$i]) version..."
#                 $response = Invoke-WebRequest "https://raw.githubusercontent.com/MosaicShell/$($SkinArray[$i])/main/%40Resources/Version.inc" -UseBasicParsing
#                 $responseBytes = $response.RawContentStream.ToArray()
#                 if ([System.Text.Encoding]::Unicode.GetString($responseBytes) -match 'Version=(.+)') {
#                     $latest_v = $matches[1]
#                 }
#             } catch {
#                 rmlog "$($SkinArray[$i]) repository does not exist or is hidden"
#                 $latest_v = '0.0'
#             } finally {
#                 Get-Content "$($skinspath)$($SkinArray[$i])\@Resources\Version.inc" -Raw | Select-String -Pattern '\d\.\d+' -AllMatches | Foreach-Object {$local_v = $_.Matches.Value}
#                 rmlog "$($SkinArray[$i]) ✔️ - 🔼 |$latest_v| 🔽 |$local_v|"
#                 if ($latest_v -eq $local_v) {
#                     Write-Eq $i $SkinArray[$i] $local_v
#                 } elseif ($latest_v -gt $local_V) {
#                     Write-Gt $i $SkinArray[$i] $local_v $latest_v
#                 } else {
#                     Write-Dev $i $SkinArray[$i] $local_v
#                 }
#             }

#         } else {
#             rmlog "$($SkinArray[$i]) ❌"
#             Write-NA $i $SkinArray[$i]
#         }
#     }
#     $global:exportString | Out-File -filePath $exportTo -Force -Encoding unicode
#     $RmAPI.Bang('[!WriteKeyValue Variables Parsed 1 "'+$root+'Accessories\GenericWindow\Variants\GlobalStatusChecker.inc"][!Refresh]')
# } else {
#     $blank = ""
#     $blank | Out-File -filePath $exportTo -Force -Encoding unicode
#     $RmAPI.Bang('[!WriteKeyValue Variables Parsed 0 "'+$root+'Accessories\GenericWindow\Variants\GlobalStatusChecker.inc"]')
# }
