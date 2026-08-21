
function Initialize()
	AccessoriesCacheVars = SKIN:GetVariable('@') .. 'CacheVars\\Accessories.inc'
end

-- -------------------------------------------------------------------------- --
--                               Inlinefunctions                              --
-- -------------------------------------------------------------------------- --

function GroupVar(SectionExtract, Option)
	local Option = Option or 'SecVar'
	local config = SectionExtract:match("(.+:).*")
	local Meter = SKIN:GetMeter(config)
	local GetVar = Meter:GetOption(Option, 'Error')
	return GetVar
end

function LocalVar(Section, Option, DefaultValue)
	local DefaultValue = DefaultValue or 'Error'
	local Meter = SKIN:GetMeter(Section)
	local GetVar = Meter:GetOption(Option, DefaultValue)
	return GetVar
end

function returnBool(Variable, Match, ReturnStringT, ReturnStringF)

	Var = SKIN:GetVariable(Variable)

	ReturnStringT = ReturnStringT or '1'
	ReturnStringF = ReturnStringF or '0'
	if Var == Match then
		return (ReturnStringT)
	else
		return (ReturnStringF)
	end
end

function trim(Text, Match, Replace)
	return Text:gsub(Match, Replace)
end

-- -------------------------------------------------------------------------- --
--                               Call functions                               --
-- -------------------------------------------------------------------------- --

-- Used by IdleStyle only. Legacy function
function start(variant, title, description, iconpath, timeout)
	local File = SKIN:GetVariable('SKINSPATH') .. '#MosaicShell\\Accessories\\Toast\\Main.ini'
	if variant ~= nil then variant = 'Standard' end
	if iconpath ~= nil then iconpath = '#SKINSPATH##MosaicShell\\@Resources\\Images\\CoreAssets\\' .. SKIN:GetVariable('Set.IconStyle') .. 'Logo.png' end
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Sec.Variant', variant, File)
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Sec.Title', title, File)
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Sec.Description', description, File)
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Sec.Icon', iconpath, File)
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Sec.Timeout', timeout, File)
	SKIN:Bang('!Activateconfig', '#MosaicShell\\Accessories\\Toast')
end

function startDrop(variant, handler, arg1, arg2)
	local function clamp(v, minValue, maxValue)
		if v < minValue then
			return minValue
		end
		return v
	end

	local scalemeasure = SKIN:GetMeasure('Set.S')
	if scalemeasure ~= nil then scale = scalemeasure:GetValue() else scale = tonumber(SKIN:GetVariable('Sec.S')) end
	local arg1 = arg1 or SKIN:GetVariable('Skin.Name')
	local MyMeter = SKIN:GetMeter(handler)
	local PosX = SKIN:GetX() + MyMeter:GetX()
	local PosY = SKIN:GetY() + MyMeter:GetY() - 5 * scale
	local DimW = clamp(MyMeter:GetW(), 150, 1000)
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.SkinName', arg1, AccessoriesCacheVars)
	if variant:match('\\') then
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', variant, AccessoriesCacheVars)
	else
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', 'Variants\\' .. arg1 .. variant .. '.inc', AccessoriesCacheVars)
	end
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Transfer.S', scale, AccessoriesCacheVars)
	SKIN:Bang('!WriteKeyvalue', 'Variables', 'Dimension.W', DimW, AccessoriesCacheVars)
	if arg2 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.2', arg2, AccessoriesCacheVars) end
	SKIN:Bang('!Activateconfig', '#MosaicShell\\Accessories\\Dropdown')
	SKIN:Bang('!Move', PosX, PosY, '#MosaicShell\\Accessories\\Dropdown')
end

function startSide(variant, index, saveLocation, arg1)
	local scale = SKIN:GetMeasure('Set.S'):GetValue()
	local PosX = SKIN:GetX() + SKIN:GetW() - 400 * scale
	local PosY = SKIN:GetY()
	local DimH = SKIN:GetH()
	local SkinName = SKIN:GetVariable('Skin.Name') or '-'
	index = index or ''
	arg1 = arg1 or ''
	local bang = ''
	bang = bang .. '[!WriteKeyvalue "#MosaicShell\\Accessories\\Overlay" FadeDuration "0" "#SETTINGSPATH#Rainmeter.ini"]'
	bang = bang .. '[!WriteKeyvalue "#MosaicShell\\Accessories\\Overlay" AlwaysOnTop "2" "#SETTINGSPATH#Rainmeter.ini"]'
	bang = bang .. '[!WriteKeyvalue Variables Transfer.S ' .. scale .. ' "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!WriteKeyvalue Variables Dimension.H ' .. DimH .. ' "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!WriteKeyvalue Variables Arg.SkinName ' .. SkinName .. ' "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!WriteKeyvalue Variables Arg.Index "' .. index .. '" "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!WriteKeyvalue Variables Arg.1 "' .. arg1 .. '" "' .. AccessoriesCacheVars .. '"]'
	if variant:match('\\') then
		bang = bang .. '[!WriteKeyvalue Variables Arg.VariantPath """' .. variant .. '""" "' .. AccessoriesCacheVars .. '"]'
	else
		bang = bang .. '[!WriteKeyvalue Variables Arg.VariantPath "Variants\\' .. variant .. '.inc" "' .. AccessoriesCacheVars .. '"]'
	end
	if variant == 'Hotkey' then
		if saveLocation == nil or saveLocation == '' then
			bang = bang .. '[!WriteKeyvalue Variables Arg.SaveLocation """' .. [[#SKINSPATH##Skin.Name#\@Resources\Actions\HotKeys.ini]] .. '""" "' .. AccessoriesCacheVars .. '"]'
		else
			bang = bang .. '[!WriteKeyvalue Variables Arg.SaveLocation "' .. saveLocation .. '" "' .. AccessoriesCacheVars .. '"]'
		end
	end
	bang = bang .. '[!Activateconfig #MosaicShell\\Accessories\\Overlay' .. ']'
	bang = bang .. '[!Move ' .. PosX .. ' ' .. PosY .. ' ' .. '#MosaicShell\\Accessories\\Overlay' .. ']'
	SKIN:Bang(bang)
end

function interactionBox(variant, arg1, arg2, arg3, arg4)
	if variant:match('\\') then
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', variant, AccessoriesCacheVars)
	else
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', 'Variants\\' .. variant .. '.inc', AccessoriesCacheVars)
	end
	if arg1 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.1', arg1, AccessoriesCacheVars) end
	if arg2 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.2', arg2, AccessoriesCacheVars) end
	if arg3 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.3', arg3, AccessoriesCacheVars) end
	if arg4 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.4', arg4, AccessoriesCacheVars) end
	SKIN:Bang('!Activateconfig #MosaicShell\\Accessories\\GenericInteractionBox')
end

function window(variant, arg1, arg2, arg3, arg4)
	if variant:match('\\') then
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', variant, AccessoriesCacheVars)
	else
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', 'Variants\\' .. variant .. '.inc', AccessoriesCacheVars)
	end
	if arg1 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.1', arg1, AccessoriesCacheVars) end
	if arg2 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.2', arg2, AccessoriesCacheVars) end
	if arg3 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.3', arg3, AccessoriesCacheVars) end
	if arg4 then SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.4', arg4, AccessoriesCacheVars) end
	SKIN:Bang('!Activateconfig', '#MosaicShell\\Accessories\\GenericWindow')
end

function startOverlay(variant)
	local PosX = SKIN:GetX()
	local PosY = SKIN:GetY()
	local bang = ""
	bang = bang .. '[!WriteKeyvalue Variables Arg.VariantPath "Variants\\'..variant..'.inc" "' .. AccessoriesCacheVars .. '"]'
	bang = bang .. '[!Activateconfig #MosaicShell\\Accessories\\Overlay]'
	bang = bang .. '[!Move ' .. PosX .. ' ' .. PosY .. ' ' .. '#MosaicShell\\Accessories\\Overlay' .. ']'
	SKIN:Bang(bang)
end

function corepage(skinname, closeAfter)
	SKIN:Bang('[!WriteKeyvalue Variables Skin.Name ' .. skinname .. ' "#@#CacheVars\Configurator.inc"]')
	if skinname == '#MosaicShell' then
		SKIN:Bang('[!WriteKeyvalue Variables Skin.Set_Page General "#@#CacheVars\Configurator.inc"]')
	else
		SKIN:Bang('[!WriteKeyvalue Variables Skin.Set_Page Info "#@#CacheVars\Configurator.inc"]')
	end
	local isActiveMeasure = SKIN:GetMeasure('ActiveChecker')
	local isActive
	if isActiveMeasure ~= nil then
		isActive = isActiveMeasure:GetValue()
	end
	if isActive ~= nil and isActive == 1 then
		SKIN:Bang('[!DeactivateConfig "#MosaicShell\\Main"]')
	end
	SKIN:Bang('[!activateConfig "#MosaicShell\\Main" "Settings.ini"]')
	if closeAfter == 1 then
		SKIN:Bang('[!DeactivateConfig]')
	end
end

function PopupImageMagick(meter, option)
	if SKIN:GetVariable('AlreadyHasImageMagick') == '0' then
		SKIN:Bang('!WriteKeyvalue', 'Variables', 'Arg.VariantPath', 'Variants\\ImageMagick.inc', AccessoriesCacheVars)
		SKIN:Bang('!Activateconfig #MosaicShell\\Accessories\\GenericInteractionBox')
	else 
		SKIN:Bang(SKIN:ReplaceVariables(SKIN:GetMeter(meter):GetOption(option)))
	end
end

-- -------------------------------------------------------------------------- --
--                                    Misc                                    --
-- -------------------------------------------------------------------------- --

function processInput(Step, variableToEdit, EditedVal, meter, IsPacityValue)
	Step = Step or 1
	if variableToEdit ~= '' then EditingVar = variableToEdit end
	if IsPacityValue ~= '' then cachedIsPacityValue = IsPacityValue end
	if Step == '0' then
		SKIN:Bang('[!SetVariable Editing "' .. EditingVar .. '"]')
		SaveLocation = SKIN:GetVariable('Sec.SaveLocation' .. SKIN:GetMeter(meter):GetOption('SaveLocation', ''))
	else
		local Valuetype = ''
		if cachedIsPacityValue ~= '' then
			Valuetype = 'Any'
		else
			Valuetype = SKIN:GetMeter(EditingVar):GetOption('Type', 'Any')
		end
		local Clamp1 = tonumber(Valuetype:match('.*|(.*)|.*'))
		local Clamp2 = tonumber(Valuetype:match('.*|.*|(.*)'))

		local function saveAndProceed()
			SKIN:Bang('!WriteKeyValue', 'Variables', EditingVar, EditedVal, SaveLocation)
			SKIN:Bang('!SetVariable', EditingVar, EditedVal)
			SKIN:Bang('!UpdateMeter', '*')
			SKIN:Bang('!Redraw')
			SKIN:Bang('!UpdateMeasure', 'Auto_Refresh:M')
		end

		-- ------------------------------ any / no type ----------------------------- --
		if Valuetype:match('Any') then
			saveAndProceed()
			-- ------------------------------ integers type ----------------------------- --
		elseif Valuetype:match('Int') then
			if EditedVal:match("^%-?%d+$") ~= nil then
				if Clamp1 ~= nil then
					if tonumber(EditedVal) >= Clamp1 and tonumber(EditedVal) <= Clamp2 then
						saveAndProceed()
					else
						start('', 'Format error', 'You can only input integers between ' .. Clamp1 .. ' and ' .. Clamp2, '', '1000')
					end
				else
					saveAndProceed()
				end
			else
				start('', 'Format error', 'You can only input integers in this field', '', '1000')
			end
			-- ------------------------------ Num type ------------------------------ --
		elseif Valuetype:match('Num') then
			if EditedVal:match("^%.%d*$") ~= nil then
				EditedVal = '0'..EditedVal
			elseif EditedVal:match("^%-%.%d*$") ~= nil then
				EditedVal = '-0'..EditedVal:gsub('^%-', '')
			end
			if EditedVal:match("^%-?%d+%.*%d*$") ~= nil then
				if Clamp1 ~= nil then
					if tonumber(EditedVal) >= Clamp1 and tonumber(EditedVal) <= Clamp2 then
						saveAndProceed()
					else
						start('', 'Format error', 'You can only input numbers between ' .. Clamp1 .. ' and ' .. Clamp2, '', '1000')
					end
				else
					saveAndProceed()
				end
			else
				start('', 'Format error', 'You can only input numbers in this field', '', '1000')
			end
			-- -------------------------------- time type ------------------------------- --
		elseif Valuetype:match('Time') then
			if EditedVal:find('^%d+[hms]%d*[hms]?%d*[hms]?') then
				saveAndProceed()
			else
				start('', 'Format error', 'You can only input time durations in this field, example: 1h20m30s', '', '1000')
			end
			-- -------------------------------- Text type ------------------------------- --
		elseif Valuetype:match('Txt') then
			if not EditedVal:find('[%d.]') then
				saveAndProceed()
			else
				start('', 'Format error', 'You can only input text in this field', '', '1000')
			end
		end
	end
end
