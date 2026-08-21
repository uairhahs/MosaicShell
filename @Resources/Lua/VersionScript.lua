IsExist = false

function Initialize()
    local function file_exists(name)
        local f = io.open(name, "r")
        if f ~= nil then io.close(f) return true else return false end
    end

    SKIN:Bang('[!Delay 500][!CommandMeasure CoreInstallHandler "GenCoreData"]')
    local SkinsPath = SKIN:GetVariable('SKINSPATH')
    local SkinName = SKIN:GetVariable('Skin.Name')
    local BetaSkinList = SKIN:GetVariable('BetaSkinList')
    if file_exists(SkinsPath .. SkinName .. '\\@Resources\\IsClone.txt') then
        SKIN:Bang('[!SetVariable Sec.Status Clone][!SetVariable Sec.HeaderImageName Clone][!HideMeterGroup Tags][!HideMeterGroup HideIsClone][!UpdateMeter *][!Redraw]')
    elseif SkinName == BetaSkinList then
        SKIN:Bang('[!SetVariable Sec.Status Beta][!UpdateMeter *][!Redraw]')
    else
        if file_exists(SkinsPath .. SkinName .. '\\@Resources\\Version.inc') then
            IsExist = true
        end
        SKIN:Bang('[!EnableMeasureGroup CheckForbeta][!UpdateMeasureGroup CheckForBeta]')
    end
end

function CheckVersion()
    local ver = tonumber(SKIN:GetMeasure('mVer'):GetStringValue())
    local localver = tonumber(SKIN:GetVariable('Version'))
    print(ver, localver)
    if IsExist then
        if ver == localver then
            SKIN:Bang('[!SetVariable Sec.Status Installed]')
        elseif ver < localver then
            SKIN:Bang('[!SetVariable Sec.Status Devbuild]') 
        elseif ver > localver then
            SKIN:Bang('[!SetVariable Sec.Status NotU2D][!ShowMeterGroup Download]')
        end
    else
        SKIN:Bang('[!SetVariable Sec.Status NotInstalled][!ShowMeterGroup Download]')
    end
    SKIN:Bang('[!UpdateMeter *][!Redraw]')
end

function CheckPatchNotes()
    local user = SKIN:GetMeasure('p.SysInfo.USER_NAME'):GetStringValue()
    local pnCheckerVar = SKIN:GetVariable('Core.patchNoteCheckvariable')
    if pnCheckerVar ~= nil then
        -- local user = SKIN:GetMeasure('user'):GetValue()
        if user ~= pnCheckerVar then
            SKIN:Bang('[!commandMeasure Func "interactionBox(\'PatchNote\')"][!WriteKeyvalue Variables Core.patchNoteCheckvariable "' .. user .. '" "' .. SKIN:GetVariable('SKINSPATH') .. SKIN:GetVariable('Skin.Name') .. '\\@Resources\\PatchNoteVar.inc' .. '"]')
            local SkinName = SKIN:GetVariable('Skin.Name')
            if string.match("Substrate|Keystrokes|ValliStart|QuickNote|Keylaunch|IdleStyle|YourMixer|YourFlyouts", SkinName) then
                local PROGRAMPATH = SKIN:GetVariable('PROGRAMPATH')
                local SkinsPath = SKIN:GetVariable('SkinsPath')
                SKIN:Bang('[!WriteKeyValue Variables RMPATH "'..PROGRAMPATH..'Rainmeter.exe" "'..SkinsPath..''..SkinName..'\\@Resources\\Actions\\Hotkeys.ini"]')
            end
        end
    end
end
