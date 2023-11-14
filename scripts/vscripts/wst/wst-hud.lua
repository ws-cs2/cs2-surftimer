local function formatHtml(color, label, value, size)
    if size then
        return string.format("<font color=\"white\">%s:</font><font class='%s' color=\"%s\"> %s</font>", label, size, color, value)
    end
    return string.format("<font color=\"white\">%s:</font><font color=\"%s\"> %s</font>", label, color, value)
end


local function getPr(time)
    if time == nil then
        return nil
    end
    local pr = FormatTime(time)
    return formatHtml("#7882dd", " (PB", pr) .. ")"
end


local function getRankHtml(player)
    local position, total_players, time = getPlayerPosition(player.steam_id)
    local positionStr = position or "-"
    local totalPlayersStr = total_players or "-"
    local prStr = getPr(time) or ""
    return formatHtml("#7882dd", "Rank", positionStr .. "/" .. totalPlayersStr) .. prStr
end

local function getSpeedHtml(speed)
    return formatHtml("#6EA6DD", "Speed", string.format("%06.2f", speed), 'fontSize-l') .. " u/s"
end

local function getTimeHtml(player)
    local color = "#F24E4E" -- Default red for negative condition
    local timeValue = 0

    local message = "Time"

    if player.timer then
        if player.prac then
            color = "#7882dd" -- Blue for prac timer
            message = "Prac"
        else 
            color = "#2E9F65" -- Green for running timer

        end
        timeValue = Time() - player.timer
    elseif player.is_in_start_zone then
        color = "#F2D94E" -- Yellow for start zone
    end

    return formatHtml(color, message, FormatTime(timeValue), 'fontSize-l')
end

function BuildPlayerHudHtml(player, speed)
    local htmlParts = {
        getTimeHtml(player),
        getSpeedHtml(speed),
        getRankHtml(player)
    }

    return table.concat(htmlParts, "<br>")
end
