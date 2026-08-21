function ToggleMaximize()
    SKIN:Bang("[!AutoSelectScreen 1]")
    local isMaximized = tonumber(SKIN:GetVariable("Maximized"))
    local WindowPosX = tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
    local WindowPosY = tonumber(SKIN:GetVariable("CURRENTCONFIGY"))
    local windowWidth = tonumber(SKIN:GetVariable("W"))
    local windowHeight = tonumber(SKIN:GetVariable("H"))
    local windowScale = SKIN:GetMeasure("S"):GetValue()
    local windowBorder = SKIN:GetVariable('B')

    local workAreaX = tonumber(SKIN:GetVariable("WORKAREAX"))
    local workAreaY = tonumber(SKIN:GetVariable("WORKAREAY"))
    local dragMargin = tonumber(SKIN:GetVariable("WindowDragMarginSize"))

    isMaximized = math.abs(-isMaximized + 1) -- switch maximized

    if savedX == nil then
        savedX = SKIN:GetMeasure('Windowsave'):GetOption('X')
        savedY = SKIN:GetMeasure('Windowsave'):GetOption('Y')
        savedW = SKIN:GetMeasure('Windowsave'):GetOption('W')
        savedH = SKIN:GetMeasure('Windowsave'):GetOption('H')
        savedS = SKIN:GetMeasure('Windowsave'):GetOption('S')
    end

    local bang = ""
    bang = bang .. "[!SetVariable Maximized " .. isMaximized .. "]"
    bang = bang .. '[!WriteKeyValue Variables Maximized "' .. isMaximized .. '" "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
    if isMaximized == 1 then
        bang = bang .. '[!SetVariable WindowPosX "(#WORKAREAX#)"][!SetVariable WindowPosY "(#WORKAREAY#)"]'
        bang = bang .. '[!SetVariable W "(#WORKAREAWIDTH#)"][!SetVariable H "(#WORKAREAHEIGHT#)"][!SetOption S Formula 1.0][!UpdateMeasure S]'
        bang = bang .. '[!WriteKeyValue Variables WindowPosX  "(#WORKAREAX#)" "#@#Includes\\Window.inc"]'
        bang = bang .. '[!WriteKeyValue Variables WindowPosY  "(#WORKAREAY#)" "#@#Includes\\Window.inc"]'
        bang = bang .. '[!WriteKeyValue Variables W "(#WORKAREAWIDTH#)" "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue Variables H "(#WORKAREAHEIGHT#)" "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue S Formula 1.0 "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!HideMeterGroup WindowDragMargin][!Updatemeter *][!Redraw]'
        bang = bang .. '[!Move "' .. (workAreaX - windowBorder) .. '" ' .. (workAreaY - windowBorder) .. ']'
        bang = bang .. '[!Draggable 0]'

        -- Store positions for future use
        savedX = WindowPosX
        savedY = WindowPosY
        savedW = windowWidth
        savedH = windowHeight
        savedS = windowScale
        bang = bang .. '[!WriteKeyValue WindowSave X  ' .. savedX .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue WindowSave Y  ' .. savedY .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue WindowSave W  ' .. savedW .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue WindowSave H  ' .. savedH .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue WindowSave S  ' .. savedS .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'

    else
        bang = bang .. '[!SetVariable WindowPosX "' .. savedX .. '"][!SetVariable WindowPosY " ' .. savedY .. '"]'
        bang = bang .. '[!SetVariable W "' .. savedW .. '"][!SetVariable H "' .. savedH .. '"][!SetOption S Formula ' .. savedS .. ']'
        bang = bang .. '[!WriteKeyValue Variables WindowPosX  ' .. savedX .. ' "#@#Includes\\Window.inc"]'
        bang = bang .. '[!WriteKeyValue Variables WindowPosY  ' .. savedY .. ' "#@#Includes\\Window.inc"]'
        bang = bang .. '[!WriteKeyValue Variables W  ' .. savedW .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue Variables H  ' .. savedH .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!WriteKeyValue S Formula ' .. savedS .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]'
        bang = bang .. '[!ShowMeterGroup WindowDragMargin][!Updatemeter *][!Redraw]'
        bang = bang .. "[!Move \"" .. (savedX) .. "\" \"" .. (savedY) .. "\"]"
    end

    SKIN:Bang(bang)
end

function ToggleDrag(toggle)
    local isMaximized = tonumber(SKIN:GetVariable("Maximized"))
    if isMaximized == 0 then
        SKIN:Bang('[!Draggable ' .. toggle .. ']')
    end
end

local Resizing = false
local ResizeBorder = nil
local offsetX = 0
local offsetY = 0

function round(num, numDecimalPlaces)
    local mult = 10 ^ (numDecimalPlaces or 0)
    return math.floor(num * mult + 0.5) / mult
end

function MouseMovedCallback(mouseX, mouseY)
    if not Resizing then
        print("Error, MouseMovedCallback called when not resizing")
        return
    end

    local skinPosX = tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
    local skinPosY = tonumber(SKIN:GetVariable("CURRENTCONFIGY"))
    local windowWidth = tonumber(SKIN:GetVariable("W"))
    local windowHeight = tonumber(SKIN:GetVariable("H"))
    local resolution = 4 / 3
    local scaleWindowWidth = tonumber(SKIN:GetVariable("ScaleWindowW"))
    local minWindowWidth = tonumber(SKIN:GetVariable("MinWindowW"))
    local minWindowHeight = tonumber(SKIN:GetVariable("MinWindowH"))
    local maxWindowWidth = tonumber(SKIN:GetVariable("MaxWindowW"))
    local maxWindowHeight = tonumber(SKIN:GetVariable("MaxWindowH"))
    -- local dragMargin = tonumber(SKIN:GetVariable("WindowDragMarginSize"))
    local dragMargin = 0

    local newWindowX = nil
    local newWindowY = nil
    local newWindowWidth = nil
    local newWindowHeight = nil

    if ResizeBorder == "DragMarginRight" or ResizeBorder == "DragMarginTopRight" or ResizeBorder == "DragMarginBottomRight" then
        newWindowWidth = (mouseX - skinPosX - dragMargin - offsetX)
    end

    if ResizeBorder == "DragMarginBottom" or ResizeBorder == "DragMarginBottomRight" or ResizeBorder == "DragMarginBottomLeft" then
        newWindowHeight = (mouseY - skinPosY - dragMargin - offsetY)
    end

    if ResizeBorder == "DragMarginLeft" or ResizeBorder == "DragMarginBottomLeft" or ResizeBorder == "DragMarginTopLeft" then
        newWindowX = mouseX - offsetX
        newWindowWidth = windowWidth + skinPosX - newWindowX
    end

    if ResizeBorder == "DragMarginTop" or ResizeBorder == "DragMarginTopLeft" or ResizeBorder == "DragMarginTopRight" then
        newWindowY = mouseY - offsetY
        newWindowHeight = windowHeight + skinPosY - newWindowY
    end

    local bang = ""

    if newWindowWidth ~= nil then -- if Width is changed then
        if newWindowX ~= nil then
            if newWindowWidth < minWindowWidth then
                newWindowX = newWindowX - minWindowWidth + newWindowWidth
            end
            if newWindowWidth > maxWindowWidth then
                newWindowX = newWindowX - maxWindowWidth + newWindowWidth
            end
        end
        -- ------------------- make boundaries with min max check ------------------- --
        if newWindowWidth < minWindowWidth then newWindowWidth = minWindowWidth end
        if newWindowWidth > maxWindowWidth then newWindowWidth = maxWindowWidth end
        -- --------------------------------- scalingW -------------------------------- --
        -- if true then
        -- when hori
        -- if newWindowWidth < scaleWindowWidth then
        -- 	-- when < sW
        -- 	local scaling = newWindowWidth / scaleWindowWidth
        -- 	bang = bang .. "[!SetOption S Formula " .. scaling .. "]" 
        -- 	-- When hori & >= sW & W exclusive movement : 1
        -- 	-- When hori & >= sW & WH movement : H
        -- elseif newWindowWidth >= scaleWindowWidth and newWindowHeight == nil then
        -- 	bang = bang .. "[!SetOption S Formula 1]" 
        -- end
        -- elseif windowHeight > newWindowWidth then
        -- -- when vert
        -- 	if 
        -- end
        if newWindowWidth < (windowHeight * resolution) then
            local scaling = round((newWindowWidth / scaleWindowWidth), 2)
            if scaling > 1 then scaling = 1 end
            bang = bang .. "[!SetOption S Formula " .. scaling .. "][!UpdateMeasure S]"
        end
        -- ------------------------- set variable for width ------------------------- --
        bang = bang .. "[!SetVariable W " .. newWindowWidth .. "]"
    end

    if newWindowHeight ~= nil then -- if Height is changed then
        if newWindowY ~= nil then
            if newWindowHeight < minWindowHeight then
                newWindowY = newWindowY - minWindowHeight + newWindowHeight
            end
            if newWindowHeight > maxWindowHeight then
                newWindowY = newWindowY - maxWindowHeight + newWindowHeight
            end
        end
        -- ------------------- make boundaries with min max check ------------------- --
        if newWindowHeight < minWindowHeight then newWindowHeight = minWindowHeight end
        if newWindowHeight > maxWindowHeight then newWindowHeight = maxWindowHeight end
        -- --------------------------------- scalingH -------------------------------- --
        if newWindowHeight < (windowWidth / resolution) then
            local scaling = round((newWindowHeight / (scaleWindowWidth / resolution)), 2)
            if scaling > 1 then scaling = 1 end
            bang = bang .. "[!SetOption S Formula " .. scaling .. "][!UpdateMeasure S]"
        end
        -- ------------------------- set variable for height ------------------------- --
        bang = bang .. "[!SetVariable H " .. newWindowHeight .. "]"
    end

    if bang ~= "" then
        --bang = bang .. "[!UpdateMeterGroup Window][!Redraw]" TODO When clip is implemented?
        bang = bang .. "[!UpdateMeter *][!Redraw]"

    end

    if newWindowX ~= nil and newWindowY ~= nil then
        bang = bang .. "[!Move " .. newWindowX .. " " .. newWindowY .. "]"
    elseif newWindowX ~= nil then
        bang = bang .. "[!Move " .. newWindowX .. " " .. skinPosY .. "]"
    elseif newWindowY ~= nil then
        bang = bang .. "[!Move " .. skinPosX .. " " .. newWindowY .. "]"
    end

    SKIN:Bang(bang)
end

function LeftMouseUpCallback(mouseX, mouseY)
    bang = "[!CommandMeasure ScriptMouseHandler UnsubscribeMouseEvent('WindowHandler','MouseMove')]"
    bang = bang .. "[!CommandMeasure ScriptMouseHandler UnsubscribeMouseEvent('WindowHandler','LeftMouseUp')]"
    local skinPosX = tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
    local skinPosY = tonumber(SKIN:GetVariable("CURRENTCONFIGY"))
    local windowWidth = tonumber(SKIN:GetVariable("W"))
    local windowHeight = tonumber(SKIN:GetVariable("H"))
    local currentScale = SKIN:GetMeasure("S"):GetValue()
    bang = bang .. "[!WriteKeyValue Variables WindowPosX " .. skinPosX .. ' "#@#Includes\\Window.inc"]' -- Write X pos in case of refresh
    bang = bang .. "[!WriteKeyValue Variables WindowPosY " .. skinPosY .. ' "#@#Includes\\Window.inc"]' -- Write Y pos in case of refresh
    bang = bang .. "[!WriteKeyValue Variables W " .. windowWidth .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]' -- Write Width in case of refresh
    bang = bang .. "[!WriteKeyValue Variables H " .. windowHeight .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]' -- Write Height in case of refresh
    bang = bang .. "[!WriteKeyValue S Formula " .. currentScale .. ' "#ROOTCONFIGPATH#Accessories\\GenericWindow\\Main.ini"]' -- Write Scale in case of refresh
    bang = bang .. "[!UpdateMeter *][!Redraw]"

    SKIN:Bang(bang)
    ResizeBorder = nil
    Resizing = false
end

function ResizeWindow(border, mouseX, mouseY)
    local mouseHandler = SKIN:GetMeasure("ScriptMouseHandler")
    if mouseHandler == nil then
        print("Mouse handler not found, include module ScriptMouseHandler")
        return
    end
    ResizeBorder = border
    Resizing = true
    bang = "[!CommandMeasure ScriptMouseHandler SubscribeMouseEvent('MouseMovedCallback','WindowHandler','MouseMove')]"
    bang = bang .. "[!CommandMeasure ScriptMouseHandler SubscribeMouseEvent('LeftMouseUpCallback','WindowHandler','LeftMouseUp')]"
    SKIN:Bang(bang)
    offsetX = mouseX
    offsetY = mouseY
end

function Initialize()
end
