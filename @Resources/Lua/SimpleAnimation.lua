function Initialize()
    -- SKIN:Bang('[!SetTransparency 20]')
    AniSteps = tonumber(SELF:GetOption('Animation_Steps'))
    AniEase = SELF:GetOption('Animation_Easing')
    AniHandler = SELF:GetOption('Animation_Handler')
    AniVar= SELF:GetOption('Animation_Node')
    AniGroup= SELF:GetOption('Animation_Group')
    dofile(SELF:GetOption("ScriptFile"):match("(.*[/\\])") .. "tween.lua")
    subject = {
        TweenNode = 0
    }
    t = tween.new(AniSteps, subject, {TweenNode=1}, AniEase)
end


function tweenAnimation(dir)
    if dir == 'in' then 
        t:update(1)
    else
        t:update(-1)
    end
    resultantTN = subject.TweenNode
    if resultantTN > 1 then resultantTN = 1 elseif resultantTN < 0 then resultantTN = 0 end
    local bang = '[!SetVariable '..AniVar..' '..resultantTN..']'
    bang = bang..'[!UpdateMeterGroup '..AniGroup..'][!Redraw]'
    SKIN:Bang(bang)
end

function endTween()
    t:set(AniSteps)
end