mediaPlayers = { 'AIMP', 'CAD', 'WMP', 'iTunes', 'Winamp', 'WebNowPlaying', 'Modern' }
havePlaying = false
activated = false

function Initialize()
    RootConfig = SKIN:GetVariable('ROOTCONFIG')
    SKIN:Bang('[!Delay 1000][!CommandMeasure Func "checkMedia()"]')
end

function checkMedia()
    for i = 1, 7 do
        if SKIN:GetMeasure('state' .. mediaPlayers[i]):GetValue() ~= 0 then
            havePlaying = true
            break
        else
            havePlaying = false
        end
    end
    if havePlaying == true and activated == false then
        SKIN:Bang('[!ActivateConfig "' .. RootConfig .. '\\Main"]')
        activated = true
    elseif havePlaying == false then
        SKIN:Bang('[!DeactivateConfig "' .. RootConfig .. '\\Main"]')
        activated = false
    end
end
