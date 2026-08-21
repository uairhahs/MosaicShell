function Initialize()
    if SKIN:GetVariable('Page.SubpageModule') == 'SingleRow' then
        EditingModule = 'Single'
        EditingPrefix = 'S'
        Count = 5
    elseif SKIN:GetVariable('Page.SubpageModule') == 'DoubleRow' then
        EditingModule = 'Double'
        EditingPrefix = 'D'
        Count = 10
    elseif SKIN:GetVariable('Page.SubpageModule') == 'Win11Row' then
        EditingModule = 'Win11'
        EditingPrefix = 'W'
        Count = 12
    end
    for i = 1, Count do
        SKIN:Bang('!SetOption', i .. '.String', 'Text', '#*' .. EditingPrefix .. i .. '*#')
    end
    SKIN:Bang('[!SetVariable EditingSectionPrefix ' .. EditingModule .. '][!SetVariable EditingPrefix ' .. EditingPrefix .. '][!SetVariable Count ' .. Count .. '][!UpdateMeterGroup DynamicName][!Redraw]')
end

-- -------------------------------------------------------------------------- --
--                                  Functions                                 --
-- -------------------------------------------------------------------------- --

function Remove(initSelection, editingHandle)
    if initSelection == 1 then
        SKIN:Bang('!SetOptionGroup', 'ActionButton', 'Text', '[\\xF78A]')
        SKIN:Bang('!SetOptionGroup', 'ActionButtonShape', 'MeterStyle', 'Set.RectButton:S | Sec.Delete:S')
        SKIN:Bang('!SetOptionGroup', 'ActionButtonShape', 'Fill', 'Fill Color 255,0,0,100')
        SKIN:Bang('!UpdateMeterGroup', 'Actions')
        SKIN:Bang('!Redraw')
    elseif initSelection == 1 then
        SKIN:Bang('!SetOptionGroup', 'ActionButton', 'Text', '[\\xE70F]')
        SKIN:Bang('!SetOptionGroup', 'ActionButtonShape', 'MeterStyle', 'Set.RectButton:S | Sec.Edit:S')
        SKIN:Bang('!SetOptionGroup', 'ActionButtonShape', 'Fill', 'Fill Color 0,255,50,100')
        SKIN:Bang('!UpdateMeterGroup', 'Actions')
        SKIN:Bang('!Redraw')
    end
end
