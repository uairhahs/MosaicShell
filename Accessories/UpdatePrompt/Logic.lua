function check()
    mVer = SKIN:GetMeasure('mVer')
    CoreVer = tonumber(SKIN:GetVariable('Core.Ver', '00000'))
    ParsedVer = tonumber(mVer:GetStringValue())
    ParsedVerFull = mVer:GetStringValue()
    if ParsedVer == CoreVer then
        print('Up2date - ' .. ParsedVer .. '==' .. CoreVer)
    elseif ParsedVer <= CoreVer then
        print('Beta - ' .. ParsedVer .. '<=' .. CoreVer)
    else
        print('Update required - ' .. ParsedVer .. '>=' .. CoreVer)
        SKIN:Bang('!CommandMeasure', 'Func', 'interactionBox(\'CoreUpdateAvailable\', \'' .. ParsedVerFull .. '\')')
        -- SKIN:Bang("[!SetVariable Sec.LatestCoreVer "..ParsedVerFull.."][!UpdateMeasure Toaster][!CommandMeasure Toaster Run]")
    end
end