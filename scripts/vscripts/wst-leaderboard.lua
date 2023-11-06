-- Todo: Figure out how to seralize this to disk.
--       If there is nothing within the vscript API then my idea is to have a external program thats
--       reading the output of SRCDS. When we finish a map we can write the times to console (stdout) and then
--       serialize it as a leaderboard.lua which is loaded on map start.

-- Table to store players
local leaderboard = {}


-- Function to insert or update a player in the leaderboard
function updateLeaderboard(player, time)
    local leaderboardPlayer = {
        steam_id = player.steam_id,
        name = player.name,
        time = time
    }
    -- Check if the player already exists and update their time
    for i, p in ipairs(leaderboard) do
        if p.steam_id == leaderboardPlayer.steam_id then
            leaderboard[i].time = leaderboardPlayer.time
            return
        end
    end
    -- If player is new, insert them into the leaderboard
    table.insert(leaderboard, leaderboardPlayer)
    sortLoaderboard()
end

-- Function to sort the leaderboard
function sortLeaderboard()
    table.sort(leaderboard, function(a, b) return a.time < b.time end)
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
