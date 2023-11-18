-- Table to store players
local current_map = GetMapName()

local vdfLeaderboard = LoadKeyValues('scripts/wst_records/' .. current_map .. '.txt')
local leaderboard = {}
local leaderboardOrder = {}
local leaderboardOrderIdxBySteamId = {}
local leaderboardSize = 0

if vdfLeaderboard ~= nil then
    print('Leaderboard loaded from disk')
    print('Leaderboard Version: ', vdfLeaderboard.version)

    if vdfLeaderboard.version ~= '_1.0' then
        print('Leaderboard version is not 1.0, ignoring')
        return
    end

    local data = vdfLeaderboard.data
    for key, value in pairs(data) do
        local entry = {
            name = value.name,
            time = value.time
        }
        leaderboard[key] = entry

        table.insert(leaderboardOrder, key)
    end
else
    print('Leaderboard not found, creating new one')
end

function sortLeaderboard()
    table.sort(leaderboardOrder, function(a, b)
        return tonumber(leaderboard[a].time) < tonumber(leaderboard[b].time)
    end)
    local count = 0
    for i, steam_id in ipairs(leaderboardOrder) do
        leaderboardOrderIdxBySteamId[steam_id] = i
        count = count + 1
    end
    leaderboardSize = count
end

sortLeaderboard()

local TIER_THRESHOLDS = {
    { tier = "Elite",    percentile = 0.98, min_players = 10 },
    { tier = "Diamond",  percentile = 0.95, min_players = 0 },
    { tier = "Platinum", percentile = 0.85, min_players = 0 },
    { tier = "Gold",     percentile = 0.70, min_players = 0 },
    { tier = "Silver",   percentile = 0.40, min_players = 0 },
    { tier = "Bronze",   percentile = 0.0,  min_players = 0 }
}

TIER_COLORS = {
    ["Elite"] = "<DARKPURPLE>",
    ["Diamond"] = "<DARKBLUE>",
    ["Platinum"] = "<BLUE>",
    ["Gold"] = "<YELLOW>",
    ["Silver"] = "<DARKGREY>",
    ["Bronze"] = "<LIGHTGREEN>",
    ["Unknown"] = "<DARKBLUE>"
}


function determinePlayerTier(position, total_players)
    local percentile = 1 - (position / total_players)
    for _, tier_info in ipairs(TIER_THRESHOLDS) do
        if percentile >= tier_info.percentile then
            return tier_info.tier
        end
    end
    return "Unknown"
end

print('-----------------')
print('wst-leaderboard.lua loaded')

-- Function to insert or update a player in the leaderboard
function updateLeaderboard(player, time)
    -- wst_mm_save_record surf_beginner "STEAM_0:1:123456789" 50 "player name"
    SendToServerConsole('wst_mm_save_record ' .. current_map .. ' "' ..
        player.steam_id .. '" ' .. time .. ' "' .. player.name .. '"')

    -- Check if the player already exists and update their time
    local leaderboardPlayer = leaderboard[player.steam_id]
    if leaderboardPlayer ~= nil then
        if leaderboardPlayer.time > time then
            leaderboardPlayer.time = time
            sortLeaderboard()
            return
        end
        return
    end


    -- If player is new, insert them into the leaderboard
    local entry = {
        name = player.name,
        time = time,
    }
    leaderboard[player.steam_id] = entry
    table.insert(leaderboardOrder, player.steam_id)
    sortLeaderboard()
end

-- Function to get a player's position
function getPlayerPosition(steam_id)
    local total_players = leaderboardSize
    local leaderboardEntry = leaderboard[steam_id]
    if leaderboardEntry ~= nil then
        local position = leaderboardOrderIdxBySteamId[steam_id]
        local tier = determinePlayerTier(position, total_players)
        return position, total_players, leaderboardEntry.time, tier
    end

    return nil, total_players, nil, nil -- player not found
end

-- Function to get the top N players
function getTopPlayers(n)
    local topPlayers = {}
    for i = 1, n do
        local steam_id = leaderboardOrder[i]
        if steam_id ~= nil then
            local entry = leaderboard[steam_id]
            if entry ~= nil then
                local player = {
                    steam_id = steam_id,
                    name = entry.name,
                    time = entry.time
                }
                table.insert(topPlayers, player)
            end
        end
    end
    return topPlayers
end

function getWorldRecordTime()
    local topPlayers = getTopPlayers(1)
    if topPlayers == nil then
        return nil
    end
    if topPlayers[1] ~= nil then
        return topPlayers[1].time
    end
    return nil
end
