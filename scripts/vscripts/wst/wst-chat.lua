function ConvertTextToColoredChatString(text)
    local colorMap = {
        ["<GOLD>"] = "\x10",       -- Gold color
        ["<WHITE>"] = "\x01",      -- White color
        ["<GREEN>"] = "\x04",      -- Green color
        ["<RED>"] = "\x02",        -- Red color
        ["<DARKRED>"] = "\x02",    -- Dark Red color
        ["<PURPLE>"] = "\x03",     -- Purple color
        ["<DARKGREEN>"] = "\x04",  -- Dark Green color
        ["<LIGHTGREEN>"] = "\x05", -- Light Green color
        ["<LIGHTGREY>"] = "\x08",  -- Light Grey color
        ["<YELLOW>"] = "\x09",     -- Yellow color
        ["<ORANGE>"] = "\x10",     -- Orange color
        ["<DARKGREY>"] = "\x0A",   -- Dark Grey color
        ["<BLUE>"] = "\x0B",       -- Blue color
        ["<DARKBLUE>"] = "\x0C",   -- Dark Blue color
        ["<GRAY>"] = "\x0D",       -- Gray color
        ["<DARKPURPLE>"] = "\x0E", -- Dark Purple color
        ["<LIGHTRED>"] = "\x0F"    -- Light Red color
    }
    for colorCode, escapeSequence in pairs(colorMap) do
        text = text:gsub(colorCode, escapeSequence)
    end

    return " " .. text
end
