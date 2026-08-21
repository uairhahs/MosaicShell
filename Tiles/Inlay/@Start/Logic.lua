function check()
    mVer = SKIN:GetMeasure('mVer')
    CoreVer = tonumber(SKIN:GetVariable('Core.Ver', '00000'))
    ParsedVer = tonumber(mVer:GetStringValue())
    ParsedVerFull = mVer:GetStringValue()
    SavePos = SKIN:GetVariable('@')..'Actions\\InstallData.ini'
    SaveLocation = SKIN:GetVariable('@')..'Actions'
    if ParsedVer == CoreVer then
        print('Up2date - '..ParsedVer..'=='..CoreVer)
        SKIN:Bang('!UpdateMeasure', 'MosaicShellYes')
    elseif ParsedVer <= CoreVer then
        print('Beta - '..ParsedVer..'<='..CoreVer)
        SKIN:Bang('!UpdateMeasure', 'MosaicShellYes')
    else
        print('Update required - '..ParsedVer..'>='..CoreVer)
        SKIN:Bang('!WriteKeyValue', 'Data', 'DownloadLink', 'https://github.com/uairhahs/MosaicShell/releases/download/v'..ParsedVerFull..'/MosaicShell_v'..ParsedVerFull..'.rmskin', SavePos)
        SKIN:Bang('!WriteKeyValue', 'Data', 'SaveLocation', SaveLocation, SavePos)
        SKIN:Bang('!UpdateMeasure', 'MosaicShellNo')
    end
end