$SkinsPath = $RmAPI.VariableStr('SKINSPATH')
$SkinName = $RmAPI.VariableStr('Skin.Name')
$CurrentPage = $RmAPI.VariableStr('Skin.Set_Page')
$rootConfig = $RmAPI.VariableStr('Skin.Name') + '\Main'
function Vector {

    Craft -type vector

    $RmAPI.Bang("[!Refresh `"$rootConfig`"]")

    $RmAPI.Bang('[!WriteKeyValue Variables Sec.Generate 0 "' + $SkinsPath + $SkinName + '\Core\' + $CurrentPage + '.inc"]')
}
function Round {
    Craft -type round

    $RmAPI.Bang("[!Refresh `"$rootConfig`"]")

    $RmAPI.Bang('[!WriteKeyValue Variables Sec.Generate 0 "' + $SkinsPath + $SkinName + '\Core\' + $CurrentPage + '.inc"]')
}
function Bar {

    $barType = $RmAPI.VariableStr('BarType')
    $RmAPI.Log($barType)

    Craft -type bar -barType $barType

    $RmAPI.Bang("[!Refresh `"$rootConfig`"]")

    $RmAPI.Bang('[!WriteKeyValue Variables Sec.Generate 0 "' + $SkinsPath + $SkinName + '\Core\' + $CurrentPage + '.inc"]')
}
function Craft {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('bar', 'round', 'vector')]
        $type,
        [Parameter()]
        [ValidateSet('normal', 'reflection', 'mirrorX', 'mirrorY', 'mirrorXY')]
        $barType
    )
    $resources = $RmAPI.VariableStr('SKINSPATH') + $RmAPI.VariableStr('Skin.Name') + '\@Resources\'
    $CoreData = $RmAPI.VariableStr('SKINSPATH') + '..\CoreData\' + $RmAPI.VariableStr('Skin.Name') + '\'
    $measureContent = @"
"@

    $bands = $RmAPI.Variable("Bands")


    #measure switch
    switch ($type) {
        'vector' {
            $layerCount = $RmAPI.Variable("LayerCount")

            $k = $RmAPI.Variable("VectorBands")

            for ($j = 1; $j -lt $layerCount + 1; $j++) {
                for ($i = 1; $i -lt $k + 1; $i++) {
                    $measureContent += @"

[MeasureL$j$i]
Measure=Plugin
Plugin=AudioAnalyzer
Parent=AudioAnalyzer
Type=Child
Index=$i
Channel=Auto
HandlerName=Layer$($j)Output

"@
                }
                $k = 2 * $k - 1
            }
            $measureContent | Out-File -FilePath $($CoreData + 'Measures\AudioMeasures.inc') -Encoding utf8
            break
        }
        default {
            for ($i = 1; $i -lt $bands + 1; $i++) {
                $measureContent += @"

[Measure$i]
Measure=Plugin
Plugin=AudioAnalyzer
Parent=AudioAnalyzer
Type=Child
Index=$i
Channel=Auto
HandlerName=MainFinalOutput

"@
            }
            $measureContent | Out-File -FilePath $($CoreData + 'Measures\AudioMeasures.inc') -Encoding utf8
            $colorHash = HashColors -bands $bands
        }
    }


    #visualizer switch
    switch ($type) {
        'bar' {

            $barWidth = $RmAPI.Variable("BarWidth")

            $barGap = $RmAPI.Variable("BarGap")

            $flipY = $RmAPI.Variable("flipY")

            $flipX = $RmAPI.Variable("flipX")

            $barStrokeWidth = $RmAPI.Variable("BarStrokeWidth")

            $barHeight = $RmAPI.Variable("Height")

            $levitate = $RmAPI.Variable("Levitate")

            $angle = $RmAPI.Variable("Angle")

            $angleSin = Math -f sin -v $($angle % 360)

            $angleCos = Math -f cos -v $($angle % 360)

            $cornerRadius = $RmAPI.Variable("CornerRounding")

            $minimumHeight = $RmAPI.Variable("MinimumHeight")

            $strokeMaxAlpha = $RmAPI.Variable('StrokeMaxAlpha')

            $strokeMinAlpha = $RmAPI.Variable('StrokeMinAlpha')

            $shapeContent = @"
"@

            switch ($barType) {
                'normal' {
                    $a = $barWidth + $barGap
                    $width1 = ($barWidth + $barGap) * $bands + $barStrokeWidth - $barGap
                    $height1 = $barHeight + $minimumHeight + $levitate + $barStrokeWidth

                    $transformationMatrix = "$([math]::Cos(-$angle%360*([math]::PI/180)));$(-[math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Cos(-$angle%360*([math]::PI/180)));$($(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$width1}else{0})+$(if ($angle%360 -gt 0 -and $angle%360 -lt 180){$angleSin*$height1}else{0}));$($(if ($angle%360 -gt 180 -and $angle%360 -lt 360){$angleSin*$width1}else{0})+$(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$height1}else{0}))"

                    $shapeContent += @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$($angleCos*$width1 + $angleSin*$height1)
H=$($angleSin*$width1 + $angleCos*$height1)
TransformationMatrix=$transformationMatrix

"@
                    for ($i = 1; $i -lt $bands + 1; $i++) {
                        $shapeContent += @"

Shape$(if ($i -ne 1){$i}else{''})=Rectangle $(if($i -eq 1){$barStrokeWidth/2}else{$barStrokeWidth/2+($i-1)*$a}), ($((1-$flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$flipY)*($levitate)*[Measure$(if ($flipX -eq 0){$i}else{$bands-$i+1})])), $barWidth, ($(-1+2*$flipY)*($minimumHeight+$barHeight*[Measure$(if ($flipX -eq 0){$i}else{$bands-$i+1})])), $cornerRadius | Fill Color $($colorHash[$(if ($flipX -eq 0){$i}else{$bands-$i+1})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($flipX -eq 0){$i}else{$bands-$i+1})])"})

"@
                    }
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$height1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    break
                }


                'mirrorX' {
                    $gr1XOff = $RmAPI.Variable("Group1XOffset")
                    $gr1YOff = $RmAPI.Variable("Group1YOffset")
                    $gr1flipX = $RmAPI.Variable("Group1XFlip")
                    $gr1flipY = $RmAPI.Variable("Group1YFlip")
                    $gr2XOff = $RmAPI.Variable("Group2XOffset")
                    $gr2YOff = $RmAPI.Variable("Group2YOffset")
                    $gr2flipX = $RmAPI.Variable("Group2XFlip")
                    $gr2flipY = $RmAPI.Variable("Group2YFlip")

                    $groupWidth = $(if (($gr1XOff + ($barWidth + $barGap) * $bands - $barGap) -le ($gr2XOff + 2 * ($barWidth + $barGap) * $bands - $barGap)) { ($gr2XOff + 2 * ($barWidth + $barGap) * $bands - $barGap) }else { ($gr1XOff + ($barWidth + $barGap) * $bands - $barGap) })
                    $groupHeight = $(if ($gr1YOff -ge $gr2YOff) { $gr1YOff }else { $gr2YOff })

                    $a = $barWidth + $barGap
                    $RmAPI.Log("$($a*$bands-$barGap)")
                    $width1 = $groupWidth + $barStrokeWidth
                    $height1 = $barHeight + $minimumHeight + $levitate + $barStrokeWidth + $groupHeight

                    $transformationMatrix = "$([math]::Cos(-$angle%360*([math]::PI/180)));$(-[math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Cos(-$angle%360*([math]::PI/180)));$($(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$width1}else{0})+$(if ($angle%360 -gt 0 -and $angle%360 -lt 180){$angleSin*$height1}else{0}));$($(if ($angle%360 -gt 180 -and $angle%360 -lt 360){$angleSin*$width1}else{0})+$(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$height1}else{0}))"

                    $shapeContent += @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$($angleCos*$width1 + $angleSin*$height1)
H=$($angleSin*$width1 + $angleCos*$height1)
TransformationMatrix=$transformationMatrix

"@
                    for ($i = 1; $i -lt $bands + 1; $i++) {
                        $shapeContent += @"

Shape$(if ($i -ne 1){$i}else{''})=Rectangle $($gr1XOff+$barStrokeWidth/2+($i-1)*$a), ($($gr1YOff+(1-$gr1flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$gr1flipY)*($levitate)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $barWidth, ($(-1+2*$flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])"})

"@
                    }
                    $b = $a * $bands
                    for ($i = $bands + 1; $i -lt 2 * $bands + 1; $i++) {
                        $shapeContent += @"

Shape$i=Rectangle $($gr2XOff+$b+($i-1-$bands)*$a), ($($gr2YOff+(1-$gr2flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$gr2flipY)*($levitate)*[Measure$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})])), $barWidth, ($(-1+2*$gr2flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})])"})

"@
                    }
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$height1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    break
                }


                'mirrorY' {
                    $gr1XOff = $RmAPI.Variable("Group1XOffset")
                    $gr1YOff = $RmAPI.Variable("Group1YOffset")
                    $gr1flipX = $RmAPI.Variable("Group1XFlip")
                    $gr1flipY = $RmAPI.Variable("Group1YFlip")
                    $gr2XOff = $RmAPI.Variable("Group2XOffset")
                    $gr2YOff = $RmAPI.Variable("Group2YOffset")
                    $gr2flipX = $RmAPI.Variable("Group2XFlip")
                    $gr2flipY = $RmAPI.Variable("Group2YFlip")

                    $groupWidth = $(if ($gr1XOff -le $gr2XOff) { ($gr2XOff + ($barWidth + $barGap) * $bands - $barGap) }else { ($gr1XOff + ($barWidth + $barGap) * $bands - $barGap) })
                    $groupHeight = $gr1YOff + $gr2YOff

                    $a = $barWidth + $barGap
                    $width1 = $groupWidth + $barStrokeWidth
                    $height1 = 2 * ($barHeight + $minimumHeight + $levitate + $barStrokeWidth) + $groupHeight

                    $transformationMatrix = "$([math]::Cos(-$angle%360*([math]::PI/180)));$(-[math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Cos(-$angle%360*([math]::PI/180)));$($(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$width1}else{0})+$(if ($angle%360 -gt 0 -and $angle%360 -lt 180){$angleSin*$height1}else{0}));$($(if ($angle%360 -gt 180 -and $angle%360 -lt 360){$angleSin*$width1}else{0})+$(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$height1}else{0}))"

                    $shapeContent += @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$($angleCos*$width1 + $angleSin*$height1)
H=$($angleSin*$width1 + $angleCos*$height1)
TransformationMatrix=$transformationMatrix

"@
                    for ($i = 1; $i -lt $bands + 1; $i++) {
                        $shapeContent += @"

Shape$(if ($i -ne 1){$i}else{''})=Rectangle $($gr1XOff+$barStrokeWidth/2+($i-1)*$a), ($($gr1YOff+(1-$gr1flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$gr1flipY)*($levitate)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $barWidth, ($(-1+2*$gr1flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])"})

"@
                    }
                    for ($i = $bands + 1; $i -lt 2 * $bands + 1; $i++) {
                        $shapeContent += @"

Shape$i=Rectangle $($gr2XOff+$barStrokeWidth/2+($i-1-$bands)*$a), ($($barHeight+$barStrokeWidth+$levitate+$minimumHeight+$gr2YOff+$gr2flipY*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(1-2*$gr2flipY)*($levitate)*[Measure$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1-$i})])), $barWidth, ($(1-2*$gr2flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1-$i})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1 - $i})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1 - $i})])"})

"@
                    }
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$height1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    break
                }


                'mirrorXY' {
                    $gr1XOff = $RmAPI.Variable("Group1XOffset")
                    $gr1YOff = $RmAPI.Variable("Group1YOffset")
                    $gr1flipX = $RmAPI.Variable("Group1XFlip")
                    $gr1flipY = $RmAPI.Variable("Group1YFlip")
                    $gr2XOff = $RmAPI.Variable("Group2XOffset")
                    $gr2YOff = $RmAPI.Variable("Group2YOffset")
                    $gr2flipX = $RmAPI.Variable("Group2XFlip")
                    $gr2flipY = $RmAPI.Variable("Group2YFlip")
                    $gr3XOff = $RmAPI.Variable("Group3XOffset")
                    $gr3YOff = $RmAPI.Variable("Group3YOffset")
                    $gr3flipX = $RmAPI.Variable("Group3XFlip")
                    $gr3flipY = $RmAPI.Variable("Group3YFlip")
                    $gr4XOff = $RmAPI.Variable("Group4XOffset")
                    $gr4YOff = $RmAPI.Variable("Group4YOffset")
                    $gr4flipX = $RmAPI.Variable("Group4XFlip")
                    $gr4flipY = $RmAPI.Variable("Group4YFlip")

                    $groupWidthRawTop = $(if (($gr1XOff + ($barWidth + $barGap) * $bands - $barGap) -le ($gr2XOff + 2 * ($barWidth + $barGap) * $bands - $barGap)) { ($gr2XOff + 2 * ($barWidth + $barGap) * $bands - $barGap) }else { ($gr1XOff + ($barWidth + $barGap) * $bands - $barGap) })
                    $groupHeightRawTop = $(if ($gr1YOff -ge $gr2YOff) { $gr1YOff }else { $gr2YOff })

                    $groupWidthRawBottom = $(if (($gr3XOff + ($barWidth + $barGap) * $bands - $barGap) -le ($gr4XOff + 2 * ($barWidth + $barGap) * $bands - $barGap)) { ($gr4XOff + 2 * ($barWidth + $barGap) * $bands - $barGap) }else { ($gr3XOff + ($barWidth + $barGap) * $bands - $barGap) })
                    $groupHeightRawBottom = $(if ($gr3YOff -ge $gr4YOff) { $gr3YOff }else { $gr4YOff })

                    $groupWidth = $(if ($groupWidthRawTop -ge $groupWidthRawBottom) { $groupWidthRawTop }else { $groupWidthRawBottom })
                    $groupHeight = $groupHeightRawTop + $groupHeightRawBottom

                    $a = $barWidth + $barGap
                    $b = $a * $bands

                    $width1 = $groupWidth + $barStrokeWidth
                    $height1 = 2 * ($barHeight + $minimumHeight + $levitate + $barStrokeWidth) + $groupHeight

                    $transformationMatrix = "$([math]::Cos(-$angle%360*([math]::PI/180)));$(-[math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Cos(-$angle%360*([math]::PI/180)));$($(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$width1}else{0})+$(if ($angle%360 -gt 0 -and $angle%360 -lt 180){$angleSin*$height1}else{0}));$($(if ($angle%360 -gt 180 -and $angle%360 -lt 360){$angleSin*$width1}else{0})+$(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$height1}else{0}))"

                    $shapeContent += @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$($angleCos*$width1 + $angleSin*$height1)
H=$($angleSin*$width1 + $angleCos*$height1)
TransformationMatrix=$transformationMatrix

"@
                    for ($i = 1; $i -lt $bands + 1; $i++) {
                        $shapeContent += @"

Shape$(if ($i -ne 1){$i}else{''})=Rectangle $($gr1XOff+$barStrokeWidth/2+($i-1)*$a), ($($gr1YOff+(1-$gr1flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$gr1flipY)*($levitate)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $barWidth, ($(-1+2*$flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])"})

"@
                    }
                    for ($i = $bands + 1; $i -lt 2 * $bands + 1; $i++) {
                        $shapeContent += @"

Shape$i=Rectangle $($gr2XOff+$barStrokeWidth/2+$b+($i-1-$bands)*$a), ($($gr2YOff+(1-$gr2flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$gr2flipY)*($levitate)*[Measure$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})])), $barWidth, ($(-1+2*$gr2flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr2flipX -eq 0){2*$bands+1 - $i}else{$i-$bands})])"})

"@
                    }
                    for ($i = 2 * $bands + 1; $i -lt 3 * $bands + 1; $i++) {
                        $shapeContent += @"

Shape$i=Rectangle $($gr3XOff+$barStrokeWidth/2+($i-1-2*$bands)*$a), ($(($barHeight+$barStrokeWidth+$levitate+$minimumHeight)+$gr3YOff+$gr3flipY*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(1-2*$gr3flipY)*($levitate)*[Measure$(if ($gr3flipX -eq 0){$i-2*$bands}else{3*$bands+1-$i})])), $barWidth, ($(1-2*$gr3flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr3flipX -eq 0){$i-2*$bands}else{3*$bands+1-$i})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr3flipX -eq 0){$i-2*$bands}else{3*$bands-$i+1})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr3flipX -eq 0){$i-2*$bands}else{3*$bands+1-$i})])"})

"@
                    }
                    for ($i = 3 * $bands + 1; $i -lt 4 * $bands + 1; $i++) {
                        $shapeContent += @"

Shape$i=Rectangle $($gr4XOff+$barStrokeWidth/2+$b+($i-1-3*$bands)*$a), ($(($barHeight+$barStrokeWidth+$levitate+$minimumHeight)+$gr4YOff+$gr4flipY*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(1-2*$gr4flipY)*($levitate)*[Measure$(if ($gr4flipX -eq 0){4*$bands+1-$i}else{$i-3*$bands})])), $barWidth, ($(1-2*$gr4flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr4flipX -eq 0){4*$bands+1-$i}else{$i-3*$bands})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr4flipX -eq 0){4*$bands+1-$i}else{$i-3*$bands})]) | StrokeWidth $barStrokeWidth | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))$(if($RmAPI.Variable('StrokeLiveAlpha') -eq 1){",($strokeMinAlpha+$($strokeMaxAlpha-$strokeMinAlpha)*[Measure$(if ($gr4flipX -eq 0){4*$bands+1 - $i}else{$i-3*$bands})])"})

"@
                    }
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$height1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    break
                }


                'reflection' {
                    $gr1XOff = $RmAPI.Variable("Group1XOffset")
                    $gr1YOff = $RmAPI.Variable("Group1YOffset")
                    $gr1flipX = $RmAPI.Variable("Group1XFlip")
                    $gr1flipY = $RmAPI.Variable("Group1YFlip")
                    $gr2XOff = $RmAPI.Variable("Group2XOffset")
                    $gr2YOff = $RmAPI.Variable("Group2YOffset")
                    $gr2flipX = $RmAPI.Variable("Group2XFlip")
                    $gr2flipY = $RmAPI.Variable("Group2YFlip")
                    $reflectionPercentage = $RmAPI.Variable("ReflectionPercentage") / 100

                    $groupWidth = $(if ($gr1XOff -le $gr2XOff) { ($gr2XOff + ($barWidth + $barGap) * $bands - $barGap) }else { ($gr1XOff + ($barWidth + $barGap) * $bands - $barGap) })
                    $groupHeight = $gr1YOff + $gr2YOff

                    $a = $barWidth + $barGap
                    $width1 = $groupWidth + $barStrokeWidth
                    $height1 = 2 * ($barHeight + $minimumHeight + $levitate + $barStrokeWidth) + $groupHeight

                    $transformationMatrix = "$([math]::Cos(-$angle%360*([math]::PI/180)));$(-[math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Cos(-$angle%360*([math]::PI/180)));$($(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$width1}else{0})+$(if ($angle%360 -gt 0 -and $angle%360 -lt 180){$angleSin*$height1}else{0}));$($(if ($angle%360 -gt 180 -and $angle%360 -lt 360){$angleSin*$width1}else{0})+$(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$height1}else{0}))"

                    $shapeContent += @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$($angleCos*$width1 + $angleSin*$height1)
H=$($angleSin*$width1 + $angleCos*$height1)
TransformationMatrix=$transformationMatrix

"@
                    for ($i = 1; $i -lt $bands + 1; $i++) {
                        $shapeContent += @"

Shape$(if ($i -ne 1){$i}else{''})=Rectangle $($gr1XOff+$barStrokeWidth/2+($i-1)*$a), ($($gr1YOff+(1-$gr1flipY)*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth/2)+($(-1+2*$gr1flipY)*($levitate)*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $barWidth, ($(-1+2*$gr1flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})])), $cornerRadius | Fill Color $($colorHash[$(if ($gr1flipX -eq 0){$i}else{$bands-$i+1})]) | StrokeWidth $barStrokeWidth | Stroke Color $($RmAPI.VariableStr('StrokeColor'))

"@
                    }
                    for ($i = $bands + 1; $i -lt 2 * $bands + 1; $i++) {
                        $shapeContent += @"

Shape$i=Rectangle $($gr2XOff+$barStrokeWidth/2+($i-1-$bands)*$a), ($($barHeight+$barStrokeWidth+$levitate+$minimumHeight+$gr2YOff+$gr2flipY*($barHeight+$levitate+$minimumHeight)+$barStrokeWidth)+($(1-2*$gr2flipY)*($levitate)*[Measure$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1-$i})])), $barWidth, ($(1-2*$gr2flipY)*($minimumHeight+$barHeight*[Measure$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1-$i})])), $cornerRadius | Fill LinearGradient Gradient$i  | StrokeWidth $barStrokeWidth | Stroke LinearGradient StrokeGradient
Gradient$i=90 | $($colorHash[$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1 - $i})]),0;0.0 | $($colorHash[$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1 - $i})]),0;$(1-$reflectionPercentage) | $($colorHash[$(if ($gr2flipX -eq 0){$i-$bands}else{2*$bands+1 - $i})]),150;1.0

"@
                    }
                    $shapeContent += @"
StrokeGradient=90 | $($RmAPI.VariableStr('StrokeColor')),0;0.0 | $($RmAPI.VariableStr('StrokeColor')),0;$(1-$reflectionPercentage) | $($RmAPI.VariableStr('StrokeColor')),150;1.0
"@
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                    $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$height1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
                }
            }

            $shapeContent | Out-File -FilePath $($CoreData + 'Visualizer\Bar.inc') -Encoding utf8
        }




        'round' {
            $bands = $RmAPI.Variable("Bands")

            $barWidth = $RmAPI.Variable("BarWidth")

            $height = $RmAPI.Variable("Height")

            $minimumHeight = $RmAPI.Variable("MinimumHeight")

            $radius = $RmAPI.Variable("Radius")

            $angleStart = $RmAPI.Variable("StartAngle")

            $totalAngle = $RmAPI.Variable("TotalAngle")

            $levitate = $RmAPI.Variable("Levitate")

            $strokeStartCap = $RmAPI.VariableStr("StartCap")

            $strokeEndCap = $RmAPI.VariableStr("EndCap")

            $transformationMatrix = "1;0;0;1;0;0"

            $width1 = 2 * ($height + $radius + $levitate + $minimumHeight) + $barWidth

            $shapeContent = @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$(2*($height+$radius+$levitate+$minimumHeight)+$barWidth)
H=$(2*($height+$radius+$levitate+$minimumHeight)+$barWidth)

"@

            for ($i = 1; $i -lt $bands + 1; $i++) {
                $shapeContent += @"

Shape$(if($i -ne 1){$i}else{''})=Line ($((-([math]::Cos($(Math -f rad -v $($angleStart+$i*($totalAngle/$bands)))))))*($radius+($levitate)*[Measure$i]) + $($height+$radius+$levitate+$minimumHeight+$barWidth/2)), ($((-([math]::Sin($(Math -f rad -v $($angleStart+$i*($totalAngle/$bands)))))))*($radius+ ($levitate)*[Measure$i]) + $($height+$radius+$levitate+$minimumHeight+$barWidth/2)), ($(-([math]::Cos($(Math -f rad -v $($angleStart + $i*$TotalAngle/$bands)))))*($radius+($levitate)*[Measure$i]+$minimumHeight+$height*[Measure$i]) + $($height+$radius+$levitate+$minimumHeight+$barWidth/2)), ($(-([math]::Sin($(Math -f rad -v $($angleStart + $i*$TotalAngle/$bands)))))*($radius+($levitate)*[Measure$i]+$minimumHeight+$height*[Measure$i]) + $($height+$radius+$levitate+$minimumHeight+$barWidth/2)) | StrokeWidth $barWidth | Stroke Color $($colorHash[$i]) | StrokeStartCap $strokeStartCap | StrokeEndCap $strokeEndCap
"@
            }
            $shapeContent | Out-File -FilePath $($CoreData + 'Visualizer\Round.inc') -Encoding utf8
            $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
            $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
        }




        'vector' {
            #Getting variables
            $vectorBands = $RmAPI.Variable("VectorBands")

            $layerCount = $RmAPI.Variable("LayerCount")

            $vectorWidth = $RmAPI.Variable("VectorWidth")


            $angle = $RmAPI.Variable("Angle")

            $angleSin = Math -f sin -v $($angle % 360)

            $angleCos = Math -f cos -v $($angle % 360)

            $k = $vectorBands

            $layerHash = @{}

            $mainUnit = "Unit-Main=Channels [#Channel] | Handlers MainFFT"

            $Handlers = @"
"@
            #Getting heights for different layers and putting them into an array for later use
            $heightArray = @('')

            for ($j = 1; $j -lt $layerCount + 1; $j++) {
                $heightArray += $RmAPI.Variable("Layer$($j)Height")
            }

            $maxHeight = ($heightArray | Measure-Object -Maximum).Maximum

            $minimumHeight = if ($RmAPI.Variable('MinimumHeight') -le $maxHeight) { $RmAPI.Variable('MinimumHeight') }else { $maxHeight }

            $width1 = $vectorWidth
            $height1 = $maxHeight

            $transformationMatrix = "$([math]::Cos(-$angle%360*([math]::PI/180)));$(-[math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Sin(-$angle%360*([math]::PI/180)));$([math]::Cos(-$angle%360*([math]::PI/180)));$($(if (($angle%360 -gt 90) -and ($angle%360 -lt 270)){$angleCos*$width1}else{0})+$(if ($angle%360 -gt 0 -and $angle%360 -lt 180){$angleSin*$height1}else{0}));$($(if ($angle%360 -gt 180 -and $angle%360 -lt 360){$angleSin*$width1}else{0})+$(if ($angle%360 -gt 90 -and $angle%360 -lt 270){$angleCos*$height1}else{0}))"

            #Creating required content
            for ($j = 1; $j -lt $layerCount + 1; $j++) {

                $vectorLine = "Layer$j = 0,$maxHeight | LineTo 0,$($maxHeight-$minimumHeight) | "

                for ($i = 1; $i -lt $k + 1; $i++) {
                    $vectorLine += "LineTo $(if ($i -ne 1){$i*($VectorWidth/$k)}else{0}), ($($maxHeight-$minimumHeight-$heightArray[$j]) + $($heightArray[$j])*(1-[MeasureL$j$i])) | "
                }

                $mainUnit += ", Layer$($j)BR(MainFFT), Layer$($j)BCT(Layer$($j)BR), Layer$($j)Output(Layer$($j)BCT)"
                $layerHash.Add("L$j", $vectorLine)
                $Handlers += @"

Handler-Layer$($j)BR=Type BandResampler | Bands Log(Count $($k+1), FreqMin 20, FreqMax 16000) | CubicInterpolation true
Handler-Layer$($j)BCT=Type BandCascadeTransformer | MixFunction Average | MinWeight 0.01 | TargetWeight 2 | ZeroLevelMultiplier 1
Handler-Layer$($j)Output=Type TimeResampler | Attack [#Attack] | Decay [#Decay] | Transform dB, Map(From -[#MaxSensitivity] : -[#MinSensitivity]), Clamp


"@
                $k = 2 * $k - 1
            }

            $audioMeasureContent = @"

[AudioAnalyzer]
Measure=Plugin
Plugin=AudioAnalyzer
Type=Parent
Source=[#AnalyzerPort]
ProcessingUnits=Main
$mainUnit | Filter Custom bqHighPass(Q 0.2, Freq 20, ForcedGain 5.58), bqLowPass(Q 1, Freq 16000, ForcedGain 20)
Handler-MainFFT=Type FFT | BinWidth 8 | OverlapBoost 10 | CascadesCount 2 | WindowFunction [#WindowFunction]
$Handlers
LogUnusedOptions=false

"@
            $shapeContent = @"

[Shape]
Meter=Shape
DynamicVariables=1
W=$($angleCos*$vectorWidth + $angleSin*$maxHeight)
H=$($angleSin*$vectorWidth + $angleCos*$maxHeight)
TransformationMatrix=$transformationMatrix
"@
            for ($i = 1; $i -lt $layerCount + 1; $i++) {
                $shapeContent += @"

Shape$(if($i -ne 1){$i}else{''})=Path Layer$i | StrokeWidth 0 | Extend [#BarType]$i
$($layerHash["L$i"])LineTo $vectorWidth,$($maxHeight-$minimumHeight) | LineTo $vectorWidth,$maxHeight | ClosePath 1
Solid$i=Fill Color $($RmAPI.VariableStr("Layer$($i)Color"))
Gradient$i=Fill LinearGradient1 MyGradient$i
MyGradient$i=$($RmAPI.VariableStr("Layer$($i)Gradient"))
"@
            }
            $audioMeasureContent | Out-File -FilePath $($CoreData + 'Measures\VectorAnalyzer.inc') -Encoding utf8
            $shapeContent | Out-File -FilePath $($CoreData + 'Visualizer\Vector.inc') -Encoding utf8
            $RmAPI.Bang("[!WriteKeyValue ImageUnderlay TransformationMatrix `"$transformationMatrix`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
            $RmAPI.Bang("[!WriteKeyValue ImageUnderlay W `"$width1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"][!WriteKeyValue ImageUnderlay H `"$height1`" `"$($RmAPI.VariableStr('SKINSPATH')+$RmAPI.VariableStr('Skin.Name')+'\@Resources\'+'ImageUnderlay\ImageUnderlay1.inc')`"]")
        }
    }
}
function HashColors {
    Param(
        [Parameter(Mandatory = $true)]
        $bands
    )
    $colorNum = $RmAPI.Variable('ColorCount') - 1
    $bands = $RmAPI.Variable('Bands')
    $reflection = $RmAPI.VariableStr('BarType')
    $maxAlpha = $RmAPI.Variable('MaxAlpha')
    $minAlpha = $RmAPI.Variable('MinAlpha')

    $groupStart = @()
    for ($i = 0; $i -lt $colorNum + 1; $i++) {
        $groupStart += $(if ($i -ne $colorNum) { ($bands - $bands % $colorNum) * $i / $colornum }else { $bands })
    }

    $r = @()
    $g = @()
    $b = @()
    for ($i = 1; $i -lt $colorNum + 2; $i++) {
        $a = $RmAPI.VariableStr("ColorA$i")
        $a = if ($a -match '[A-Fa-f0-9]{6,8}') { HexToRGB -type hex -HEX $a }else { $a }
        $a = $a -split ','
        $r += [int]$a[0]
        $g += [int]$a[1]
        $b += [int]$a[2]
    }

    $colorHash = @('')

    for ($i = 0; $i -lt $colorNum; $i++) {
        for ($j = $groupStart[$i]; $j -lt $groupStart[$i + 1]; $j++) {
            $colorHash += "$($r[$i]+($r[$i+1] - $r[$i])*($j-$groupStart[$i])/($groupStart[$i+1] - $groupStart[$i])),$($g[$i]+($g[$i+1] - $g[$i])*($j-$groupStart[$i])/($groupStart[$i+1] - $groupStart[$i])),$($b[$i]+($b[$i+1] - $b[$i])*($j-$groupStart[$i])/($groupStart[$i+1] - $groupStart[$i]))$(if($reflection -eq 'reflection'){}else{$(if($RmAPI.Variable('LiveAlphaBool') -eq 0){",$($RmAPI.Variable('MaxAlpha'))"}else{",($minAlpha + ($($maxAlpha-$minAlpha))*[Measure$($j+1)])"})})"
        }
    }
    if ($colorNum -eq 0) {
        $i = 0
        for ($j = 1; $j -lt $bands + 1; $j++) {
            $colorHash += "$($r[0]),$($g[0]),$($b[0])" + "$(if($reflection -eq 'reflection'){''}else{if($RmAPI.Variable('LiveAlphaBool') -eq 0){','+$maxAlpha}else{",($minAlpha + ($maxAlpha - $minAlpha)*[Measure$($i+$j)])"}})"
        }
    }
    return $colorHash
}
function Math {
    Param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('rad', 'sin', 'cos')]
        [Alias('f')]
        $func,
        [Parameter(Mandatory = $true)]
        [Alias('v')]
        $value
    )
    switch ($func) {
        rad { return $value * ([math]::PI / 180) }
        sin { return [math]::Abs([math]::Sin($value * ([math]::PI / 180))) }
        cos { return [math]::Abs([math]::Cos($value * ([math]::PI / 180))) }
    }
}
function HexToRGB {
    Param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('hex', 'hexa')]
        $Type,
        [Parameter(ParameterSetName = 'hex', Position = 1)]
        [ValidateScript({ $_ -match '[A-Fa-f0-9]{6,8}' })]
        $HEX,
        [Parameter(ParameterSetName = 'hexa', Position = 1)]
        [ValidateScript({ $_ -match '[A-Fa-f0-9]{6,8}' })]
        $HEXA
    )
    switch ($type) {
        'hex' {

            $red = $HEX.Remove(2, 4)
            $Green = $HEX.Remove(4, 2)
            $Green = $Green.remove(0, 2)
            $Blue = $hex.Remove(0, 4)
            $Red = [convert]::ToInt32($red, 16)
            $Green = [convert]::ToInt32($green, 16)
            $Blue = [convert]::ToInt32($blue, 16)
            return "$red,$Green,$blue"

        }
        'hexa' {

            $Red = $HEXA.Remove(2, 6)
            $Green = $HEXA.Remove(4, 4)
            $Green = $Green.Remove(0, 2)
            $Blue = $HEXA.Remove(0, 4)
            $Blue = $Blue.Remove(2, 2)
            $Alpha = $HEXA.Remove(0, 6)
            $Red = [convert]::ToInt32($Red, 16)
            $Green = [convert]::ToInt32($Green, 16)
            $Blue = [convert]::ToInt32($Blue, 16)
            $Alpha = [convert]::ToInt32($Alpha, 16)
            return "$Red,$Green,$Blue,$Alpha"
        }
    }
}
