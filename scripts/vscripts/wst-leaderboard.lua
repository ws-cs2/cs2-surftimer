-- Table to store players
local current_map = GetMapName()

local vdfLeaderboard = LoadKeyValues('scripts/wst_records/' .. current_map .. '.vdf')
local leaderboard = {}

if vdfLeaderboard ~= nil then
    print('Leaderboard loaded from disk')
    print('Leaderboard Version: ', vdfLeaderboard.version)

    if vdfLeaderboard.version ~= '_1.0' then
        print('Leaderboard version is not 1.0, ignoring')
        return
    end

    local data = vdfLeaderboard.data
    for key, value in pairs(data) do
        local value = {
            steam_id = key,
            name = value.name,
            time = value.time
        }
        table.insert(leaderboard, value)
    end
else
    print('Leaderboard not found, creating new one')
end

function sortLeaderboard()
    table.sort(leaderboard, function(a, b) return tonumber(a.time) < tonumber(b.time) end)
end

sortLeaderboard()

function printleaderboard()
    for i, player in ipairs(leaderboard) do
        print(i, player.steam_id, player.name, player.time)
    end
end

print('-----------------')
print('wst-leaderboard.lua loaded')



-- Function to insert or update a player in the leaderboard
function updateLeaderboard(player, time)
    local leaderboardPlayer = {
        steam_id = player.steam_id,
        name = player.name,
        time = time
    }
    print('[WST_MSG] map_complete ' .. current_map .. ' ' ..
        leaderboardPlayer.steam_id .. ' ' .. leaderboardPlayer.time .. ' "' .. leaderboardPlayer.name .. '"')
    -- Check if the player already exists and update their time
    for i, p in ipairs(leaderboard) do
        if p.steam_id == leaderboardPlayer.steam_id then
            -- If the player already exists and their time is better, update it
            if p.time > leaderboardPlayer.time then
                leaderboard[i].time = leaderboardPlayer.time
                sortLeaderboard()
                return
            end
            -- If the player already exists and their time is worse, do nothing
            return
        end
    end
    -- If player is new, insert them into the leaderboard
    table.insert(leaderboard, leaderboardPlayer)
    sortLeaderboard()
end

-- Function to get a player's position
function getPlayerPosition(steam_id)
    local total_players = tablelength(leaderboard)

    for pos, player in ipairs(leaderboard) do
        if player.steam_id == steam_id then
            return pos, total_players
        end
    end
    return nil -- player not found
end

function tablelength(T)
    local count = 0
    for _ in pairs(T) do count = count + 1 end
    return count
end

-- Function to get the top N players
function getTopPlayers(n)
    local topPlayers = {}
    for i = 1, n do
        if leaderboard[i] ~= nil then
            table.insert(topPlayers, leaderboard[i])
        end
    end
    return topPlayers
end
