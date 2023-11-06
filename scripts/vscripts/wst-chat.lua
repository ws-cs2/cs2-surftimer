function ConvertTextToColoredChatString(text)
    local colorMap = {
        ["<GOLD>"] = "\x10",  -- Gold color
        ["<WHITE>"] = "\x01", -- White color
        ["<GREEN>"] = "\x04", -- Green color
    }

    for colorCode, escapeSequence in pairs(colorMap) do
        text = text:gsub(colorCode, escapeSequence)
    end

    return " " .. text
end
