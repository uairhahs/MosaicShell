function ChangeTargetTo(index)
    ChangeIndex('Shortcut'..index..'.Shape')
end

function ChangeIndex(handler)
    local bang = ''
    local handlerString = handler
    local handler = SKIN:GetMeter(handler)
    G_CurrentIndex = tonumber(handlerString:match('%d+'))
    print('Swaped to index: '.. G_CurrentIndex)
    bang = bang .. '[!EnableMouseActionGroup "LeftMouseUpAction | MouseOverAction | MouseLeaveAction" Editor.Button.Shape][!SetOptionGroup Editor.Button.String FontColor "#Font_Color#"][!UpdateMeterGroup Editor.Button]'
    bang = bang .. '[!ShowMeter Overlay.Background.Shape][!MoveMeter '..handler:GetX()..' '..handler:GetY()..' Overlay.Background.Shape]'
    bang = bang .. '[!Redraw]'
    SKIN:Bang(bang)
end

function ReturnShortcutNewShapeMeterStyle()
    local rowitems = tonumber(SKIN:GetVariable('process.module_shortcuts.rowitems_count'))
    local totalitems = tonumber(SKIN:GetVariable('module_shortcuts.totalitems_count'))
    local style = 'Shortcut.Shape:S'
    if totalitems == 0 then
        style = style .. ' | Shortcut1.Shape'
    elseif totalitems % rowitems == 0 then
        style = style .. ' | Shortcut.Shape.NewLine:S'
    end
    return style
end

function ShortcutMove(direction)
    local rowitems = tonumber(SKIN:GetVariable('process.module_shortcuts.rowitems_count'))
    local totalitems = tonumber(SKIN:GetVariable('module_shortcuts.totalitems_count'))
    if G_CurrentIndex <= rowitems and direction == 'Up' then
        print('Can\'t move up')
    elseif rowitems > totalitems - G_CurrentIndex and direction == 'Down' then
        print('Can\'t move down')
    elseif G_CurrentIndex % rowitems == 1 and direction == 'Left' then
        print('Can\'t move left')
    elseif G_CurrentIndex % rowitems == 0 and  direction == 'Right' then
        print('Can\'t move right')
    else
        local i1 = G_CurrentIndex
        local i2 = -1
        if direction == 'Up' then
            i2 = i1 - rowitems
        elseif direction == 'Down' then
            i2 = i1 + rowitems
        elseif direction == 'Left' then
            i2 =  i1 - 1
        else
            i2 = i1 + 1
        end

        G_CurrentIndex = i2

        local handler = SKIN:GetMeter('Shortcut'..G_CurrentIndex..'.Shape')
        SKIN:Bang('[!MoveMeter '..handler:GetX()..' '..handler:GetY()..' Overlay.Background.Shape]')
            
        SKIN:Bang('[!CommandMeasure p.PSRM "ShortcutSwapBetween -i1 '..i1..' -i2 '..i2..' "Inlay\\Main\\Accessories\\ShortcutsEditor\\Background""]')
    end
end

function ShortcutSelect(type)
    SKIN:Bang('[!SetVariable process.index '..G_CurrentIndex..'][!UpdateMeasure p.FileChoose]')
    if type == 'File' then
        SKIN:Bang('[!CommandMeasure p.FileChoose "ChooseFile 1"]')
    elseif type == 'Image' then
        SKIN:Bang('[!CommandMeasure p.FileChoose "ChooseImage 2"]')
    end
end

function ShortcutChange(type)
    SKIN:Bang('[!SetVariable process.index '..G_CurrentIndex..']')
    if type == 'Name' then
        local DefaultValue = SKIN:GetMeter('Shortcut'..G_CurrentIndex..'.String'):GetOption('Text')
        SKIN:Bang('[!SetOption p.InputText DefaultValue """'..DefaultValue..'"""][!CommandMeasure p.InputText "ExecuteBatch 1"]')
    end
end

function ShortcutNew()
    local totalitems = tonumber(SKIN:GetVariable('module_shortcuts.totalitems_count'))
    SKIN:Bang('[!SetVariable process.index '..(totalitems + 1)..'][!UpdateMeasure p.FileChoose]')
    SKIN:Bang('[!CommandMeasure p.FileChoose "ChooseFile 3"]')
end

function ShortcutRemove()
    local rowitems = tonumber(SKIN:GetVariable('process.module_shortcuts.rowitems_count'))
    local totalitems = tonumber(SKIN:GetVariable('module_shortcuts.totalitems_count'))
    SKIN:Bang('[!CommandMeasure p.PSRM """ShortcutRemove -index '..G_CurrentIndex..' -ti '..totalitems..' -ri '..rowitems..'""" "Inlay\\Main\\Accessories\\ShortcutsEditor\\Background"]')
end