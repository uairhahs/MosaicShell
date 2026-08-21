sectionTable = {}
leftAlignContent=""
rightAlignContent=""
variableContent="[Variables]\n"
returnButtonBool=0

function Initialize()
end

function ReadIni(inputfile)
    local file = assert(io.open(inputfile, 'r'), 'Unable to open ' .. inputfile)
    local tbl, section = {}
    local sectionReadOrder, keyReadOrder = {}, {}
    local num = 0
    local sectionNum = 0
    for line in file:lines() do
        --print(line)
        num = num + 1
        if not line:match('^%s-;') then
            local key, command = line:match('^([^=]+)=(.+)')
            if line:match('^%s-%[.+') then
                section = line:match('^%s-%[([^%]]+)')
                if not tbl[section] then
                    sectionNum = sectionNum + 1
                    table.insert(sectionTable, section)
                    tbl[section]={}
                    table.insert(sectionReadOrder, section)
                    if not keyReadOrder[section] then keyReadOrder[section]={} end
                end
            elseif key and command and section then
                tbl[section][key:match('^%s*(%S*)%s*$')] = command:match('^%s*(.-)%s*$')
                if not tbl[section]['keys'] then tbl[section]['keys']={} end
                if not tbl[section]['values'] then tbl[section]['values']={} end
                table.insert(tbl[section]['keys'], key:match('^%s*(%S*)%s*$'))
                table.insert(tbl[section]['values'], command:match('^%s*(.-)%s*$'))
            elseif #line > 0 and section and not key or command then
                print(num .. ': Invalid property or value.')
            end
        end
    end
    file:close()

    if not section then print('No sections found in ' .. inputfile) return end

    local finalTable={}
    finalTable.INI=tbl
    finalTable.SectionOrder=sectionReadOrder
    finalTable.KeyOrder=keyReadOrder
    return finalTable
end

function tablelength(T)
    local count = 0
    for _ in pairs(T) do count = count + 1 end
    -- print(count)
    return count
  end

function dump(o)
    if type(o) == 'table' then
        local s = '{ '
        for k,v in pairs(o) do
            if type(k) ~= 'number' then k = '"'..k..'"' end
            s = s .. '['..k..'] = ' .. dump(v) .. ','
        end
        return s .. '} '
    else
        return tostring(o)
    end
end

function string.starts(String,Start)
    return string.sub(String,1,string.len(Start))==Start
end

function Generate(type, s, i)
    local function checkHidden(var)
        if var == nil then
            return('\n')
        elseif string.starts(var, '!') then
            return('Hidden=(1-#'..var:gsub('!','')..'#)\n')
        else
            return('Hidden=#'..var..'#\n')
        end
    end
    local function checkHiddenLeft(var)
        if var == nil then
            return('\n')
        elseif string.starts(var, '!') then
            return('FontColor=#Set.Text_Color#,(#'..var:gsub('!','')..'# = 1 ? 255 : 150)\n')
        else
            return('FontColor=#Set.Text_Color#,(#'..var..'# = 0 ? 255 : 150)\n')
        end
    end
    local function checkSaveLocation(var)
        if var == nil then
            return('\n')
        else
            return('SaveLocation="'..var..'"\n')
        end
    end
    local function checkSaveLocationInline(var)
        if var == nil then
            return('#Sec.SaveLocation#')
        else
            return(var)
        end
    end
    local function checkLimit(var)
        if var == nil then
            return('\n')
        else
            return('Type='..var..'\n')
        end
    end
    local function Separate(str)
        local o = {}
        for m in str:gmatch('[^%s%|%s]+') do
            table.insert(o, m)
        end
        return o
    end
        


    
    i = string.format("%02d",i)
    if type == 'Header' then
        leftAlignContent = leftAlignContent ..
            '[Header'..i..']\n'..
            'Meter=String\n'..
            'Text='..s..'\n'..
            'MeterStyle=Set.String:S | Set.OptionCat:S\n'
        
        if ini.INI[s]['Info'] == '1' then
        rightAlignContent = rightAlignContent ..
            '[Info:Header'..i..']\n'..
            'Meter=Shape\n'..
            'MeterStyle=Set.RectButton:S\n'..
            checkHiddenLeft(ini.INI[s]['Hidden'])..
            '[Info'..i..']\n'..
            'Meter=String\n'..
            'MeterStyle=Set.String:S | Set.Icon:S\n'
        end
    elseif type == 'Return' then
        returnButtonBool = 1
    elseif type == 'Include' then
        variableContent = variableContent..
        '@include'..i..'Custom='..ini.INI[s]['Variable']..'\n'
    else
        if i ~= '01' then
            if ini.INI[sectionTable[(tonumber(i)-1)]]['Type'] ~= 'Header' then
                leftAlignContent = leftAlignContent..
                '[Divider'..i..']\n'..
                'Meter=Shape\n'..
                'MeterStyle=Set.Div:S\n'
            end
        end
        leftAlignContent = leftAlignContent ..
            '[Option'..i..']\n'..
            'Meter=String\n'..
            'Text='..s..'\n'..
            checkHiddenLeft(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.OptionName:S\n'
    end
    if type == 'Hotkey' then
        if ini.INI[s]['Variable'] == '1' then keyIndex = '' else keyIndex = tonumber(ini.INI[s]['Variable']) end
        rightAlignContent =rightAlignContent ..
            '[Button'..i..']\n'..
            'Meter=Shape\n'..
            'MeterStyle=Set.Button:S\n'..
            'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
            'Act=[!CommandMeasure Func "startSide(\'Hotkey\', \''..keyIndex..'\')"]\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            '[Value'..i..']\n'..
            'Meter=String\n'..
            'Text=#Key'..keyIndex..'InString#\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.Value:S\n'
        if not variableContent:find('includeHotkeys') then
            variableContent = variableContent..
            '@includeHotkeys=#SKINSPATH##Skin.Name#\\@Resources\\Actions\\HotKeys.ini\n'
        end
    elseif type == 'Bool' then
        rightAlignContent =rightAlignContent ..
            '['..ini.INI[s]['Variable']..']\n'..
            'Meter=Shape\n'..
            'MeterStyle=Set.Bool:S\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            checkSaveLocation(ini.INI[s]['SaveLocation'])..
            'Y=([Option'..i..':Y]-#Set.P#+(-20/2+8)*[Set.S])\n'
    elseif type == 'String' then
        local hiddenVar = ini.INI[s]['Hidden']
        rightAlignContent =rightAlignContent ..
            '['..ini.INI[s]['Variable']..']\n'..
            'Meter=Shape\n'..
            'MeterStyle=Set.Textbox:S\n'..
            checkLimit(ini.INI[s]['Limit'])..
            checkHidden(ini.INI[s]['Hidden'])..
            checkSaveLocation(ini.INI[s]['SaveLocation'])..
            'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
            '[Value'..i..']\n'..
            'Meter=String\n'..
            'Text=#'..ini.INI[s]['Variable']..'#\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.Value:S\n'
    elseif type == 'Switch' then
        local StringArray = Separate(ini.INI[s]['String'])
        local StringSetArray = Separate(ini.INI[s]['StringSet'])
        if ini.INI[s]['Limit'] == '3' then
            rightAlignContent =rightAlignContent ..
                '['..ini.INI[s]['Variable']..':]\n'..
                'Meter=Shape\n'..
                '0Act=[!WriteKeyValue Variables '..ini.INI[s]['Variable']..' "'..StringSetArray[1]..'" "'..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'"]\n'..
                '1Act=[!WriteKeyValue Variables '..ini.INI[s]['Variable']..' "'..StringSetArray[2]..'" "'..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'"]\n'..
                '2Act=[!WriteKeyValue Variables '..ini.INI[s]['Variable']..' "'..StringSetArray[3]..'" "'..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'"]\n'..
                'MeterStyle=Set.Pick3:S\n'..
                'Shape2=Rectangle (100*[Set.S]*([&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[2]..'\')]+[&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[3]..'\')]*2)),0,(300/3*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0\n'..
                'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                '['..ini.INI[s]['Variable']..':0]\n'..
                'Meter=String\n'..
                'Text='..StringArray[1]..'\n'..
                'FontColor=[&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[1]..'\', \'#Set.Pri_color#\', \'#Set.Text_Color#\')]\n'..
                'MeterStyle=Set.String:S | Set.PickOption3_0:S\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                '['..ini.INI[s]['Variable']..':1]\n'..
                'Meter=String\n'..
                'Text='..StringArray[2]..'\n'..
                'FontColor=[&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[2]..'\', \'#Set.Pri_color#\', \'#Set.Text_Color#\')]\n'..
                'MeterStyle=Set.String:S | Set.PickOption3_1:S\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                '['..ini.INI[s]['Variable']..':2]\n'..
                'Meter=String\n'..
                'Text='..StringArray[3]..'\n'..
                'FontColor=[&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[3]..'\', \'#Set.Pri_color#\', \'#Set.Text_Color#\')]\n'..
                'MeterStyle=Set.String:S | Set.PickOption3_2:S\n'..
                checkHidden(ini.INI[s]['Hidden'])..'\n'
        else
            rightAlignContent =rightAlignContent ..
                '['..ini.INI[s]['Variable']..':]\n'..
                'Meter=Shape\n'..
                'Shape2=Rectangle ([&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[2]..'\')] = 0 ? 0 : (150*[Set.S])),0,(150*[Set.S]),(30*[Set.S]),(7*[Set.S]) | Fill Color #Set.Text_Color# | StrokeWidth 0 |\n'..
                '0Act=[!WriteKeyValue Variables '..ini.INI[s]['Variable']..' "'..StringSetArray[1]..'" "'..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'"]\n'..
                '1Act=[!WriteKeyValue Variables '..ini.INI[s]['Variable']..' "'..StringSetArray[2]..'" "'..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'"][!WriteKeyValue Variables Location "(#Location# = 1 ? 2 : #Location#)" "'..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'"]\n'..
                'MeterStyle=Set.Pick:S\n'..
                'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                '['..ini.INI[s]['Variable']..':0]\n'..
                'Meter=String\n'..
                'Text='..StringArray[1]..'\n'..
                'FontColor=[&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[1]..'\', \'#Set.Pri_color#\', \'#Set.Text_Color#\')]\n'..
                'MeterStyle=Set.String:S | Set.PickOption_0:S\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                '['..ini.INI[s]['Variable']..':1]\n'..
                'Meter=String\n'..
                'Text='..StringArray[2]..'\n'..
                'FontColor=[&Func:returnBool(\''..ini.INI[s]['Variable']..'\', \''..StringSetArray[2]..'\', \'#Set.Pri_color#\', \'#Set.Text_Color#\')]\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                'MeterStyle=Set.String:S | Set.PickOption_1:S\n'
        end
    elseif type == 'Color' then
        if ini.INI[s]['Limit'] == nil or ini.INI[s]['Limit'] == '1' then
            rightAlignContent =rightAlignContent ..
                '['..ini.INI[s]['Variable']..']\n'..
                'Meter=Shape\n'..
                'MeterStyle=Set.Color:S\n'..
                'LeftMouseUpAction=["#@#Addons\\RainRGB4.exe" "VarName=#CURRENTSECTION#" "FileName='..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'" "RefreshConfig=#CURRENTCONFIG# | #Skin.Name#\\Main"]\n'..
                'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
                checkHidden(ini.INI[s]['Hidden'])
        elseif ini.INI[s]['Limit'] == '2' then
            rightAlignContent =rightAlignContent ..
                '['..ini.INI[s]['Variable']..']\n'..
                'Meter=Shape\n'..
                'MeterStyle=Set.ColorPacity:S\n'..
                'LeftMouseUpAction=["#@#Addons\\RainRGB4.exe" "VarName=#CURRENTSECTION#Color" "FileName='..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'" "RefreshConfig=#CURRENTCONFIG# | #Skin.Name#\\Main"]\n'..
                'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
                'Type=Num|1|255\n'..
                checkHidden(ini.INI[s]['Hidden'])..
                '[Value'..i..']\n'..
                'Meter=String\n'..
                'Text=#'..ini.INI[s]['Variable']..'Opacity#\n'..
                'MeterStyle=Set.String:S | Set.Pacity:S\n'..
                checkHidden(ini.INI[s]['Hidden'])
        end
    elseif type == 'Page' then
        rightAlignContent =rightAlignContent ..
            '[Button'..i..']\n'..
            'Meter=Shape\n'..
            'MeterStyle=Set.Button:S\n'..
            'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
            'OverColor=#Set.Accent_color#,150\n'..
            'LeaveColor=#Set.Accent_color#,50\n'..
            'Act=[!WriteKeyvalue Variables Sec.Subpage '..ini.INI[s]['SetString']..' "#SKINSPATH##Skin.Name#\\Core\\#Set.Set_Page#.inc"][!Refresh]\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            '[Icon'..i..']\n'..
            'Meter=String\n'..
            'Text=[\\xe895]\n'..
            'FontFace=Material Icons Round\n'..
            'FontSize=(14*[Set.S])\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.Value:S\n'..
            '[Value'..i..']\n'..
            'Meter=String\n'..
            'X=(-25*[Set.S])r\n'..
            'Y=r\n'..
            'Text=Open\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.Value:S\n'
    elseif type == 'Button' then
        rightAlignContent =rightAlignContent ..
            '[Button'..i..']\n'..
            'Meter=Shape\n'..
            'MeterStyle=Set.Button:S\n'..
            'Y=([Option'..i..':Y]-#Set.P#+(-30/2+8)*[Set.S])\n'..
            'OverColor=#Set.Accent_color#,150\n'..
            'LeaveColor=#Set.Accent_color#,50\n'..
            'Act='..ini.INI[s]['Act']..'\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            '[Icon'..i..']\n'..
            'Meter=String\n'..
            'Text=[\\x'..ini.INI[s]['Icon']..']\n'..
            'FontFace=Material Icons Round\n'..
            'FontSize=(14*[Set.S])\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.Value:S\n'..
            '[Value'..i..']\n'..
            'Meter=String\n'..
            'X=(-25*[Set.S])r\n'..
            'Y=r\n'..
            'Text='..ini.INI[s]['String']..'\n'..
            checkHidden(ini.INI[s]['Hidden'])..
            'MeterStyle=Set.String:S | Set.Value:S\n'
    elseif type == 'Direction' then
        rightAlignContent = rightAlignContent..
        '[Left]\n'..
        'Meter=Shape\n'..
        'X=(#Set.W#-#Set.L#-#Set.P#*2-((26*4+5*3)*[Set.S]))\n'..
        'Y=([Option'..i..':Y]-#Set.P#+(-26/2+8)*[Set.S])\n'..
        'MeterStyle=Sec.DirButton:S\n'..
        '[Right]\n'..
        'Meter=Shape\n'..
        'Rotation=Rotate 180\n'..
        'MeterStyle=Sec.DirButton:S\n'..
        '[Top]\n'..
        'Meter=Shape\n'..
        'Rotation=Rotate 90\n'..
        'MeterStyle=Sec.DirButton:S\n'..
        '[Bottom]\n'..
        'Meter=Shape\n'..
        'Rotation=Rotate 270\n'..
        'MeterStyle=Sec.DirButton:S\n'
        if not variableContent:find('DirButton') then
            variableContent = variableContent..
            '[Sec.DirButton:S]\n'..
            'X=((26+5)*[Set.S])r\n'..
            'Y=r\n'..
            'Shape=Rectangle 0,0,26,26,3 | StrokeWidth 0 | Extend Fill | Scale [Set.S],[Set.S],0,0\n'..
            'Shape2=Path Arrow | StrokeWidth (2*[Set.S]) | StrokeStartCap Round | StrokeEndCap Round | Fill Color 0,0,0,1 | Stroke Color #Set.Text_Color# | Scale [Set.S],[Set.S],0,0 | Extend Rotation\n'..
            'Arrow=8,6 | LineTo 18,12 | LineTo 8,18\n'..
            'OverColor=[&Func:returnBool(\'AniDir\', \'#CURRENTSECTION#\', \'#Set.Accent_Color#\', \'#Set.Text_Color#\')],200\n'..
            'LeaveColor=[&Func:returnBool(\'AniDir\', \'#CURRENTSECTION#\', \'#Set.Accent_Color#\', \'#Set.Text_Color#\')],150\n'..
            'LeftMouseUpAction=[!WriteKeyValue Variables AniDir "#CURRENTSECTION#" '..checkSaveLocationInline(ini.INI[s]['SaveLocation'])..'][!UpdateMeasure Auto_Refresh:M][!Refresh]\n'..
            'Fill=Fill Color [&Func:LocalVar(\'#CURRENTSECTION#\', \'LeaveColor\')]\n'..
            'MouseOverAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar(\'#CURRENTSECTION#\', \'OverColor\')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]\n'..
            'MouseLeaveAction=[!SetOption #CURRENTSECTION# Fill "Fill Color [&Func:LocalVar(\'#CURRENTSECTION#\', \'LeaveColor\')]"][!UpdateMeter #CURRENTSECTION#][!Redraw]\n'..
            'Container=ContentContainer\n'..
            'DynamicVariables=1\n'..
            checkHidden(ini.INI[s]['Hidden'])
        end
    end
end

function realAllnComment(file)
    local f = assert(io.open(file, "r"))
    local content = f:read("*a")
    f:close()
    return ';'..content:gsub('\n','\n;')
end

-- -------------------------------------------------------------------------- --
--                                    Code                                    --
-- -------------------------------------------------------------------------- --

function appendFile(var, path)
    SKIN:Bang('[!WriteKeyvalue Variables '..var..' '..path..' "'..SKIN:GetVariable('ROOTCONFIGPATH')..'Accessories\\GenericInteractionBox\\Variants\\GenerateCorePage.inc"]')
    SKIN:Bang('[!SetVariable '..var..' "'..path..'"][!UpdateMeter *][!Redraw]')
end

function GeneratePageContent()
    local inpath = SKIN:GetVariable('LastSelectedFile')
    local outskin = SKIN:GetVariable('LastOutputSkin')
    local outpage = SKIN:GetVariable('LastOutputPage')
    local outpath = SKIN:GetVariable('SKINSPATH')..outskin..'\\Core\\'..outpage..'.inc'
    ini = ReadIni(inpath)

    for i = 1, tablelength(ini.INI) do
        local s = sectionTable[i]
        Generate(ini.INI[s]['Type'], s, i)
    end

    file = io.open(outpath, 'w')
    file:write(variableContent)
    file:write(leftAlignContent)
    file:write(rightAlignContent)
    if returnButtonBool == 1 then
        file:write(
            '[Return.Button]\n',
            'Meter=Shape\n',
            'MeterStyle=Set.Button:S\n',
            'Y=([Header:Y]-#Set.P#+(-20/2+8)*[Set.S])\n',
            'OverColor=#Set.Accent_color#,150\n',
            'LeaveColor=#Set.Accent_color#,50\n',
            'Act=[!WriteKeyvalue Variables Sec.Subpage 1 "#SKINSPATH##Skin.Name#\Core\General.inc"][!Refresh]\n',
            '[Return.Icon]\n',
            'Meter=String\n',
            'Text=[\\xe31b]\n',
            'FontFace=Material Icons Round\n',
            'FontSize=(14*[Set.S])\n',
            'MeterStyle=Set.String:S | Set.Value:S\n',
            '[Return.Text]\n',
            'Meter=String\n',
            'X=(-25*[Set.S])r\n',
            'Y=r\n',
            'Text=Return\n',
            'MeterStyle=Set.String:S | Set.Value:S\n'
        )
    end
    file:write('\n\n\n'..realAllnComment(inpath))
    file:close()
end
