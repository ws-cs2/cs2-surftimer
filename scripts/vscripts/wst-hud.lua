local function formatHtml(color, label, value)
    return string.format("<font color=\"white\">%s:</font><font color=\"%s\"> %s</font>", label, color, value)
end

local function getRankHtml(position, total_players)
    local positionStr = position or "-"
    local totalPlayersStr = total_players or "-"
    return formatHtml("#7882dd", "Rank", positionStr .. "/" .. totalPlayersStr)
end

local function getSpeedHtml(speed)
    return formatHtml("#6EA6DD", "Speed", string.format("%06.2f", speed))
end

local function getTimeHtml(player)
    local color = "#F24E4E" -- Default red for negative condition
    local timeValue = 0

    if player.timer then
        color = "#2E9F65" -- Green for running timer
        timeValue = Time() - player.timer
    elseif player.is_in_start_zone then
        color = "#F2D94E" -- Yellow for start zone
    end

    return formatHtml(color, "Time", FormatTime(timeValue))
end

function BuildPlayerHudHtml(player, speed)
    local position, total_players = getPlayerPosition(player.steam_id)

    local htmlParts = {
        getTimeHtml(player),
        getSpeedHtml(speed),
        getRankHtml(position, total_players)
    }

    return table.concat(htmlParts, "<br>")
end
