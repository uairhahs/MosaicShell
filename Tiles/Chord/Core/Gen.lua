function Update()
    t = tonumber(SKIN:GetVariable('Total'))
    if t == nil then print("ERROR, CONTACT DEVELOPER FOR MORE INFORMATION")
    elseif t == 0 then
        print('Blank canvas. Writing data right now.')
        Reset()
    end
end

function Add()
    local t1 = t + 1
    local t2 = t + 2
    local t3 = t + 3

    Write(t1, t2, t3)
end


function Reset()
    Write(1, 2, 3)
end

function Write(t1, t2, t3)
    SKIN:Bang('[!CommandMeasure GenPs1 "WriteAll -arg1 '..t1..' -arg2 '..t2..' -arg3 '..t3..'"]')
end

function InitDrag(handler)
    Action1Index = handler:gsub('.Shape','')
end

function TakeDrag(handler)
    Action2Index = handler:gsub('.Shape','')
    if Action1Index == Action2Index then
        SKIN:Bang('[!CommandMEasure Func "interactionBox([[#SKINSPATH##Skin.Name#\\Core\\InteractionBox\\Action.inc]], \''..Action1Index..'\')"]')
    else
        SKIN:Bang('[!CommandMeasure GenPs1 "WriteSwap -i1 '..Action1Index..' -i2 '..Action2Index..'"]')
    end
end