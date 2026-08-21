function CheckIfNone()
    local allNone = 1
    for i=1, 7 do
        local im = SKIN:GetVariable('Module'..i-1)
        if im ~= 'None' then
            allNone = 0
            break
        end
    end
    SKIN:Bang('[!WriteKeyValue Variables ModuleAllNone '..allNone..' "#SKINSPATH##skin.name#\\@Resources\\Vars.inc"]')
end

function ChangeModuleTo(name)
    local function has_value (tab, val)
        for index, value in ipairs(tab) do
            if value == val then
                return true
            end
        end
        return false
    end

    local mA = {}
    for i=1, 7 do
        mA[i] = SKIN:GetVariable('Module'..i-1)
    end
    local bang = ''
    if has_value(mA, name) then
        bang = bang .. '[!CommandMEasure Func "interactionBox([[#SKINSPATH##Skin.Name#\\Core\\InteractionBox\\ValliError.inc]])"]'
    else
        bang = bang .. '[!WriteKeyValue Variables #Sec.Arg1# '..name..' "#SKINSPATH#valliStart\\@Resources\\Vars.inc"]'
        bang = bang .. '[!SetVariable #Sec.Arg1# '..name..' "#MosaicShell\\Main"]'
        bang = bang .. '[!UpdateMeter * "#MosaicShell\\Main"][!Redraw "#MosaicShell\\Main"][!CommandMeasure LogicLua "CheckIfNone()" "#MosaicShell\\Main"][!UpdateMeasure Auto_Refresh:M "#MosaicShell\\Main"][!DeactivateConfig]'

    end
    SKIN:Bang(bang)
end


function InitDrag(handler)
    Module1Index = handler:gsub('.Image',''):gsub('Module','')
end

function TakeDrag(handler)
    Module2Index = handler:gsub('.Image',''):gsub('Module','')
    local module1Name = SKIN:GetVariable('Module'..Module1Index)
    local module2Name = SKIN:GetVariable('Module'..Module2Index)
    local saveLocation = SKIN:GetVariable('SKINSPATH')..'Inlay\\@Resources\\Vars.inc'
    if module1Name == module2Name then
        -- SKIN:Bang('[!CommandMEasure Func "window([[#SKINSPATH##Skin.Name#\\Core\\Window\\ValliModule.inc]], \'Module'..Module2Index..'\', \'1\')"]')
        SKIN:Bang('[!WriteKeyvalue Variables page.subpagemodule "[#Module'..Module2Index..']" "#SKINSPATH##Skin.Name#\\Core\\Modules.inc"][!WriteKeyvalue Variables page.page 2 "#SKINSPATH##Skin.Name#\\Core\\Modules.inc"][!Refresh]')
    else
        SKIN:Bang('[!WriteKeyvalue Variables Module'..Module1Index..' '..module2Name..' "'..saveLocation..'"][!WriteKeyvalue Variables Module'..Module2Index..' '..module1Name..' "'..saveLocation..'"]')
        SKIN:Bang('[!SetVariable Module'..Module1Index..' '..module2Name..'][!SetVariable Module'..Module2Index..' '..module1Name..'][!UpdateMeter *][!Redraw][!UpdateMeasure Auto_Refresh:M]')
    end
end

function InitDragApps(handler)
    HotApp1Index = handler:gsub('.Shape',''):gsub('HotApp','')
end

function TakeDragApps(handler)
    HotApp2Index = handler:gsub('.Shape',''):gsub('HotApp','')
    local HotApp1Name = SKIN:GetVariable('HotApp'..HotApp1Index)
    local HotApp2Name = SKIN:GetVariable('HotApp'..HotApp2Index)
    local HotApp1Bool = SKIN:GetVariable('HotApp'..HotApp1Index..'Bool')
    local HotApp2Bool = SKIN:GetVariable('HotApp'..HotApp2Index..'Bool')
    local HotApp1Actual = SKIN:GetVariable('HotApp'..HotApp1Index..'Actual')
    local HotApp2Actual = SKIN:GetVariable('HotApp'..HotApp2Index..'Actual')
    local saveLocation = SKIN:GetVariable('SKINSPATH')..'Inlay\\Main\\Modules\\Vars\\Hotapps.inc'
    local saveLocationBool = SKIN:GetVariable('SKINSPATH')..'Inlay\\@Resources\\HotApps.inc'
    local bang = ''
    if HotApp1Name == HotApp2Name then
        if HotApp1Bool == '1' then
            bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..' None "'..saveLocation..'"]'
            bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..'Bool 0 "'..saveLocationBool..'"]'
            bang = bang .. '[!SetVariable HotApp'..HotApp1Index..' None]'
            bang = bang .. '[!SetVariable HotApp'..HotApp1Index..'Bool 0]'
        else
            bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..' '..HotApp1Actual..' "'..saveLocation..'"]'
            bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..'Bool 1 "'..saveLocationBool..'"]'
            bang = bang .. '[!SetVariable HotApp'..HotApp1Index..' '..HotApp1Actual..']'
            bang = bang .. '[!SetVariable HotApp'..HotApp1Index..'Bool 1]'
        end
    else
        bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..' '..HotApp2Name..' "'..saveLocation..'"][!WriteKeyvalue Variables HotApp'..HotApp2Index..' '..HotApp1Name..' "'..saveLocation..'"]'
        bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..'Bool '..HotApp2Bool..' "'..saveLocationBool..'"][!WriteKeyvalue Variables HotApp'..HotApp2Index..'Bool '..HotApp1Bool..' "'..saveLocationBool..'"]'
        bang = bang .. '[!WriteKeyvalue Variables HotApp'..HotApp1Index..'Actual '..HotApp2Actual..' "'..saveLocationBool..'"][!WriteKeyvalue Variables HotApp'..HotApp2Index..'Actual '..HotApp1Actual..' "'..saveLocationBool..'"]'
        bang = bang .. '[!SetVariable HotApp'..HotApp1Index..' '..HotApp2Name..'][!SetVariable HotApp'..HotApp2Index..' '..HotApp1Name..']'
        bang = bang .. '[!SetVariable HotApp'..HotApp1Index..'Bool '..HotApp2Bool..'][!SetVariable HotApp'..HotApp2Index..'Bool '..HotApp1Bool..']'
        bang = bang .. '[!SetVariable HotApp'..HotApp1Index..'Actual '..HotApp2Actual..'][!SetVariable HotApp'..HotApp2Index..'Actual '..HotApp1Actual..']'
    end
    bang = bang .. '[!UpdateMeter *][!Redraw][!UpdateMeasure Auto_Refresh:M]'
    SKIN:Bang(bang)
end