
-- -------------------------------------------------------------------------- --
--                                CircleBar                                --
-- -------------------------------------------------------------------------- --

function ImportCircleBar()
    CSCX = SKIN:GetMeasure('CircleBarCenterX'):GetValue()
    CSCY = SKIN:GetMeasure('CircleBarCenterY'):GetValue()
    CSR = SKIN:GetMeasure('CircleBarRadius'):GetValue()
    METER = SELF:GetOption('BarMeter')
    INDEX = SELF:GetOption('BarShapeIndex')
end

function DrawCircleBar(percent)
    local angle = 360 * percent / 100
    local toX = CSR * math.sin(math.rad(angle)) + CSCX
    local toY = CSCY - CSR * math.cos(math.rad(angle))
    local rflx = 0
    local bang = ''
    if angle < 360 then
        if angle > 180 then
            rflx = 1
        end
        bang = bang .. '[!SetOption "'..METER..'" Shape'..INDEX..' "Arc ' .. CSCX .. ',' .. CSCY - CSR .. ',' .. toX .. ', ' .. toY .. ',' .. CSR .. ',' .. CSR .. ',0,0,' .. rflx .. ' | Extend CircleBarLIne"]'
    elseif angle == 360 then
        bang = bang .. '[!SetOption "'..METER..'" Shape'..INDEX..' "Ellipse ' .. CSCX .. ',' .. CSCY .. ',' .. CSR .. ' | Extend CircleBarLIne"]'
    end
    bang = bang .. '[!UpdateMeterGroup CircleBar][!Redraw]'
    SKIN:Bang(bang)
end
