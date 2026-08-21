function ToggleMaximize()
	SKIN:Bang("[!AutoSelectScreen 1]")
	local isMaximized = tonumber(SKIN:GetVariable("Set.Maximized"))
	local WindowPosX = tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
	local WindowPosY = tonumber(SKIN:GetVariable("CURRENTCONFIGY"))
	local windowWidth = tonumber(SKIN:GetVariable("Set.W"))
	local windowHeight = tonumber(SKIN:GetVariable("Set.H"))
	local windowScale = SKIN:GetMeasure("Set.S"):GetValue()
	local windowBorder = SKIN:GetVariable('Set.WinB')

	local workAreaX = tonumber(SKIN:GetVariable("WORKAREAX"))
	local workAreaY = tonumber(SKIN:GetVariable("WORKAREAY"))
	local dragMargin = tonumber(SKIN:GetVariable("WindowDragMarginSize"))

	isMaximized = math.abs(-isMaximized + 1) -- switch maximized

	if savedX == nil then
		savedX = SKIN:GetMeasure('Set.Windowsave'):GetOption('X')
		savedY = SKIN:GetMeasure('Set.Windowsave'):GetOption('Y')
		savedW = SKIN:GetMeasure('Set.Windowsave'):GetOption('W')
		savedH = SKIN:GetMeasure('Set.Windowsave'):GetOption('H')
		savedS = SKIN:GetMeasure('Set.Windowsave'):GetOption('S')
	end
		
	local bang = ""
	bang = bang .. "[!SetVariable Set.Maximized " .. isMaximized .. "]"
	bang = bang .. '[!WriteKeyValue Variables Set.Maximized "'.. isMaximized ..'" "#@#Vars.inc"]'
	if isMaximized == 1 then
		bang = bang .. '[!SetVariable WindowPosX "(#WORKAREAX#)"][!SetVariable WindowPosY "(#WORKAREAY#)"]'
		bang = bang .. '[!SetVariable Set.W "(#WORKAREAWIDTH#)"][!SetVariable Set.H "(#WORKAREAHEIGHT#)"][!SetOption Set.S Formula 1.0][!UpdateMeasure Set.S]'
		bang = bang .. '[!WriteKeyValue Variables WindowPosX  "(#WORKAREAX#)" "#@#Includes\\Window.inc"]'
		bang = bang .. '[!WriteKeyValue Variables WindowPosY  "(#WORKAREAY#)" "#@#Includes\\Window.inc"]'
		bang = bang .. '[!WriteKeyValue Variables Set.W "(#WORKAREAWIDTH#)" "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Variables Set.H "(#WORKAREAHEIGHT#)" "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Set.S Formula 1.0 "#@#Vars.inc"]'
		bang = bang .. '[!HideMeterGroup WindowDragMargin][!UpdateMeasureGroup UpdateWhenResized][!Updatemeter *][!Redraw]'
		bang = bang .. '[!Move "'.. (workAreaX - windowBorder) ..'" '.. (workAreaY - windowBorder)..']'
		bang = bang .. '[!Draggable 0]'

		-- Store positions for future use
		savedX = WindowPosX
		savedY = WindowPosY
		savedW = windowWidth 
		savedH = windowHeight
		savedS = windowScale
		bang = bang .. '[!WriteKeyValue Set.WindowSave X  '.. savedX ..' "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Set.WindowSave Y  '.. savedY ..' "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Set.WindowSave W  '.. savedW ..' "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Set.WindowSave H  '.. savedH ..' "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Set.WindowSave S  '.. savedS ..' "#@#Vars.inc"]'

	else
		bang = bang .. '[!SetVariable WindowPosX "'.. savedX ..'"][!SetVariable WindowPosY " '.. savedY ..'"]'
		bang = bang .. '[!SetVariable Set.W "'.. savedW ..'"][!SetVariable Set.H "' .. savedH .. '"][!SetOption Set.S Formula '..savedS..'][!UpdateMeasure Set.S]'
		bang = bang .. '[!WriteKeyValue Variables WindowPosX  '.. savedX ..' "#@#Includes\\Window.inc"]'
		bang = bang .. '[!WriteKeyValue Variables WindowPosY  '.. savedY ..' "#@#Includes\\Window.inc"]'
		bang = bang .. '[!WriteKeyValue Variables Set.W  '.. savedW ..' "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Variables Set.H  '.. savedH ..' "#@#Vars.inc"]'
		bang = bang .. '[!WriteKeyValue Set.S Formula '.. savedS ..' "#@#Vars.inc"]'
		bang = bang .. '[!ShowMeterGroup WindowDragMargin][!UpdateMeasureGroup UpdateWhenResized][!Updatemeter *][!Redraw]'
		bang = bang .. "[!Move \"" .. (savedX) .. "\" \"" .. (savedY) .. "\"]"
	end
	
	SKIN:Bang(bang)
end

function Minimize()
	isMinimized = true
	SKIN:Bang("[!AutoSelectScreen 1]")
	SavedWindowPosX = tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
	SavedWindowPosY = tonumber(SKIN:GetVariable("CURRENTCONFIGY"))

	local bang = ""
	bang = bang .. '[!WriteKeyValue Set.WindowSave Set.Minimized true "#@#Vars.inc"]'
	bang = bang .. '[!WriteKeyValue Variables WindowPosX  '.. SavedWindowPosX ..' "#@#Includes\\Window.inc"]'
	bang = bang .. '[!WriteKeyValue Variables WindowPosY  '.. SavedWindowPosY ..' "#@#Includes\\Window.inc"]'
	bang = bang .. '[!SetTransparency 1][!SetWindowPosition 0 0 100% 100%]'
	SKIN:Bang(bang)
end

function UnMinimize()
	if isMinimized then
		isMinimized = false

		local bang = ""
		bang = bang .. '[!WriteKeyValue Set.WindowSave Set.Minimized false "#@#Vars.inc"]'
		bang = bang .. '[!SetWindowPosition '..SavedWindowPosX..' '..SavedWindowPosY..' 0 0][!SetTransparency 255]'
		SKIN:Bang(bang)
	elseif isMinimized == nil then
		local savedMinimizeState = SKIN:GetMeasure('Set.Windowsave'):GetOption('Set.Minimized')
		if savedMinimizeState == 'true' then
			isMinimized = false

			local LastWindowPosX = tonumber(SKIN:GetVariable("WindowPosX"))
			local LastWindowPosY = tonumber(SKIN:GetVariable("WindowPosY"))

			local bang = ""
			bang = bang .. '[!WriteKeyValue Set.WindowSave Set.Minimized false "#@#Vars.inc"]'
			bang = bang .. '[!SetWindowPosition '..LastWindowPosX..' '..LastWindowPosY..' 0 0][!SetTransparency 255]'
			SKIN:Bang(bang)
		end
	end
end


function ToggleDrag(toggle)
	local isMaximized = tonumber(SKIN:GetVariable("Set.Maximized"))
	if isMaximized == 0 then
		SKIN:Bang('[!Draggable '..toggle..']')
	end
end

local Resizing = false
local ResizeBorder = nil
local offsetX = 0
local offsetY = 0

function round(num, numDecimalPlaces)
	local mult = 10^(numDecimalPlaces or 0)
	return math.floor(num * mult + 0.5) / mult
  end

function MouseMovedCallback(mouseX, mouseY)
	if not Resizing then 
		print("Error, MouseMovedCallback called when not resizing")
		return
	end

	local skinPosX = CacheX or tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
	local skinPosY = CacheY or tonumber(SKIN:GetVariable("CURRENTCONFIGY"))
	local windowWidth = CacheW or tonumber(SKIN:GetVariable("Set.W"))
	local windowHeight = CacheH or tonumber(SKIN:GetVariable("Set.H"))
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
		if newWindowWidth < (windowHeight * resolution) then
			local scaling = round((newWindowWidth / scaleWindowWidth), 1)
			if scaling > 1 then scaling = 1 end
			CacheScale = scaling
			-- bang = bang .. "[!SetOption Set.S Formula " .. scaling .. "][!UpdateMeasure Set.S]" 
		end
		-- ------------------------- set variable for width ------------------------- --
		bang = bang .. "[!SetVariable Dimension.W " .. newWindowWidth .. " \"#MosaicShell\\Accessories\\Overlay\"]"
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
			local scaling = round((newWindowHeight / (scaleWindowWidth / resolution)), 1)
			if scaling > 1 then scaling = 1 end
			CacheScale = scaling
			-- bang = bang .. "[!SetOption Set.S Formula " .. scaling .. "][!UpdateMeasure Set.S]"
		end
		-- ------------------------- set variable for height ------------------------- --
		bang = bang .. "[!SetVariable Dimension.H " .. newWindowHeight .. " \"#MosaicShell\\Accessories\\Overlay\"]"
	end
	
	
	if newWindowX ~= nil and newWindowY ~= nil then
		bang = bang .. "[!Move " .. newWindowX .. " " .. newWindowY .. " \"#MosaicShell\\Accessories\\Overlay\"]"
	elseif newWindowX ~= nil then
		bang = bang .. "[!Move " .. newWindowX .. " " .. skinPosY .. " \"#MosaicShell\\Accessories\\Overlay\"]"
	elseif newWindowY ~= nil then
		bang = bang .. "[!Move " .. skinPosX .. " " .. newWindowY .. " \"#MosaicShell\\Accessories\\Overlay\"]"
	end

	bang = bang .. "[!UpdateMeter * \"#MosaicShell\\Accessories\\Overlay\"][!Redraw \"#MosaicShell\\Accessories\\Overlay\"]"
	CacheX = newWindowX
	CacheY = newWindowY
	CacheW = newWindowWidth
	CacheH = newWindowHeight

	SKIN:Bang(bang)	
end

function LeftMouseUpCallback(mouseX, mouseY)
	local bang = "[!CommandMeasure ScriptMouseHandler UnsubscribeMouseEvent('WindowHandler','MouseMove')]"
	bang = bang .. "[!CommandMeasure ScriptMouseHandler UnsubscribeMouseEvent('WindowHandler','LeftMouseUp')]"
	CacheScale = CacheScale or tonumber((SKIN:GetMeasure("Set.S")):GetValue())
	CacheX = CacheX or tonumber(SKIN:GetVariable("CURRENTCONFIGX"))
	CacheY = CacheY or tonumber(SKIN:GetVariable("CURRENTCONFIGY"))
	CacheW = CacheW or tonumber(SKIN:GetVariable("Set.W"))
	CacheH = CacheH or tonumber(SKIN:GetVariable("Set.H"))
	-- ---------------------------------- Write --------------------------------- --
	bang = bang .. "[!WriteKeyValue Variables WindowPosX " .. CacheX .. ' "#@#Includes\\Window.inc"]' 							   			-- Write X pos in case of refresh
	bang = bang .. "[!WriteKeyValue Variables WindowPosY " .. CacheY .. ' "#@#Includes\\Window.inc"]' 							   			-- Write Y pos in case of refresh
	bang = bang .. "[!WriteKeyValue Variables Set.W " .. CacheW .. ' "#@#Vars.inc"]' 					   			-- Write Width in case of refresh
	bang = bang .. "[!WriteKeyValue Variables Set.H " .. CacheH .. ' "#@#Vars.inc"]' 					   			-- Write Height in case of refresh
	bang = bang .. "[!WriteKeyValue Set.S Formula " .. CacheScale .. ' "#@#Vars.inc"]' 					   			-- Write Scale in case of refresh
	-- ----------------------------------- Set ---------------------------------- --
	bang = bang .. "[!SetOption Set.S Formula " .. CacheScale .. "][!UpdateMeasure Set.S]" 
	bang = bang .. "[!SetVariable Set.W " .. CacheW .. "]" 
	bang = bang .. "[!SetVariable Set.H " .. CacheH .. "]" 
	bang = bang .. "[!UpdateMeter *][!UpdateMeasureGroup UpdateWhenResized][!Redraw][!Move "..CacheX.." "..CacheY.."][!Deactivateconfig #MosaicShell\\Accessories\\Overlay][!Delay 400][!EnableMouseAction * *]"

	SKIN:Bang(bang)
	ResizeBorder = nil
	Resizing = false
end

function ResizeWindow(border, mouseX, mouseY)
	offsetX = mouseX
	offsetY = mouseY
	ResizeBorder = border
	Resizing = true
	local bang = "[!CommandMeasure ScriptMouseHandler SubscribeMouseEvent('MouseMovedCallback','WindowHandler','MouseMove')]"
	bang = bang .. "[!CommandMeasure ScriptMouseHandler SubscribeMouseEvent('LeftMouseUpCallback','WindowHandler','LeftMouseUp')]"
	bang = bang .. "[!DisableMouseAction * *]"
	-- ----------------------------- launch overlay ----------------------------- --
	local AccessoriesCacheVars = SKIN:GetVariable('@') .. 'CacheVars\\Accessories.inc'
	local PosX = SKIN:GetX()
	local PosY = SKIN:GetY()
	local W = SKIN:GetVariable('Set.W')
	local H = SKIN:GetVariable('Set.H')
	bang = bang .. '[!WriteKeyvalue "#MosaicShell\\Accessories\\Overlay" FadeDuration "100" "#SETTINGSPATH#Rainmeter.ini"]'
	bang = bang .. '[!WriteKeyvalue "#MosaicShell\\Accessories\\Overlay" AlwaysOnTop "1" "#SETTINGSPATH#Rainmeter.ini"]'
	bang = bang .. '[!WriteKeyvalue Variables Arg.VariantPath "Variants\\LazyResizer.inc" "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!WriteKeyvalue Variables Dimension.W '..W..' "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!WriteKeyvalue Variables Dimension.H '..H..' "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!Activateconfig #MosaicShell\\Accessories\\Overlay]'
	bang = bang .. '[!Move ' .. PosX .. ' ' .. PosY .. ' ' .. '#MosaicShell\\Accessories\\Overlay' .. ']'
	SKIN:Bang(bang)
end

function Initialize()
end