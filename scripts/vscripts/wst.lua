-- Credits: https://github.com/GameChaos/cs2_things/blob/main/scripts/vscripts/kz.lua For the PlayerTick mechanism & server variables & show_survival_respawn_status
--          https://github.com/Source2ZE/ZombieReborn/tree/main EHandleToHScript
--          https://github.com/surftimer/SurfTimer script_trigger_multiple mechanism for making a timer
--          https://github.com/Source2ZE/LuaUnlocker Lua Unlocker

require("wst-leaderboard")

print("--------------------")
print("Will's Surf Timer")
print("--------------------")


-- Only works on surf beginner
local start_zone_1 = Vector(-448.150726, -47.964149, 320)
local start_zone_2 = Vector(191.463989, 257.725311, 500)

-- End of S1
local end_zone_1 = Vector(-419.165283, 1707.492920, -383.968750)
local end_zone_2 = Vector(169.428925, 1999.968750, -200.96875)

-- End of map
-- local end_zone_1 = Vector(-4544.320312, 4868.677734, 3096)
-- local end_zone_2 = Vector(-6078.667480, 5248.378418, 3296)

function CalculateBoxFromVectors(v1, v2)
    local mins = Vector(math.min(v1.x, v2.x), math.min(v1.y, v2.y), math.min(v1.z, v2.z))
    local maxs = Vector(math.max(v1.x, v2.x), math.max(v1.y, v2.y), math.max(v1.z, v2.z))

    local center = Vector((mins.x + maxs.x) / 2, (mins.y + maxs.y) / 2, (mins.z + maxs.z) / 2)

    -- Adjust mins and maxs relative to center
    mins = mins - center
    maxs = maxs - center

    return center, mins, maxs
end

function IsPointInBox(point, minVec, maxVec)
    return (point.x >= minVec.x and point.x <= maxVec.x) and
        (point.y >= minVec.y and point.y <= maxVec.y) and
        (point.z >= minVec.z and point.z <= maxVec.z)
end

function CalculateExtentsFromMinsMaxs(mins, maxs)
    return Vector(math.max(math.abs(mins.x), math.abs(maxs.x)),
        math.max(math.abs(mins.y), math.abs(maxs.y)),
        math.max(math.abs(mins.z), math.abs(maxs.z)))
end

function FormatTime(time)
    local minutes = math.floor(time / 60)
    local seconds = time - minutes * 60
    local milliseconds = (time - math.floor(time)) * 1000
    return string.format("%02d:%02d:%03d", minutes, seconds, milliseconds)
end

function debugPrintTable(table)
    for k, v in pairs(table) do
        print(k, v)
    end
end

function debugPrintPlayer(player)
    local name = player:GetName()
    local className = player:GetClassname()
    local debugName = player:GetDebugName()
    local entityIndex = player:GetEntityIndex()
    local entidx = player:entindex()
    local location = player:GetAbsOrigin()
    print("[NAME] " .. name)
    print("[CLASSNAME] " .. className)
    print("[DEBUGNAME] " .. debugName)
    print("[ENTITYINDEX] " .. entityIndex)
    print("[ENTINDEX] " .. entidx)
    print("[LOCATION] " .. tostring(location))
    print("[USERID] " .. player.user_id)
    print("[STEAMID] " .. player.steam_id)
    print("[PLAYER_NAME] " .. player.name)
    print("[IP] " .. player.ip_address)
end

local pluginActivated = false
local g_worldent = nil

Convars:RegisterCommand("wst_cp", function()
    local player = Convars:GetCommandClient()

    player.cp_saved = true
    player.cp_origin = player:GetAbsOrigin()
    player.cp_angles = player:EyeAngles()
    player.cp_velocity = player:GetVelocity()
end, nil, 0)

Convars:RegisterCommand("wst_tele", function()
    local player = Convars:GetCommandClient()
    if player.cpSaved then
        player.timer = nil
        player:SetAbsOrigin(player.cp_origin)
        player:SetAngles(player.cp_angles.x, player.cp_angles.y, player.cp_angles.z)
        player:SetVelocity(player.cp_velocity)
    end
end, nil, 0)

Convars:RegisterCommand("wst_top", function()
    local player = Convars:GetCommandClient()
    local topPlayers = getTopPlayers(10)

    for i, p in ipairs(topPlayers) do
        print(i, p)
        debugPrintTable(p)
        local position, total_players = getPlayerPosition(p.steam_id)
        print(position, total_players)

        -- TODO: This doesn't work?
        -- UTIL_MessageText(player.user_id, "[WST] " .. position .. "/" .. total_players .. " " .. p.name .. " " .. p.time,
        --     255, 255,
        --     255, 0)

        ScriptPrintMessageChatAll("[WST] " .. position .. "/" .. total_players .. " " .. p.name .. " " .. p.time)
    end
end, nil, 0)

-- Development only
-- Convars:RegisterCommand("wst_debug_reload", function()
--     Activate()
--     SendToServerConsole("bot_kick all")
--     SendToServerConsole("mp_restartgame 1")
-- end, nil, 0)

Convars:RegisterCommand("wst_r", function()
    local player = Convars:GetCommandClient()
    TeleportToStartZone(player)
end, nil, 0)

function TeleportToStartZone(player)
    local center, _, _ = CalculateBoxFromVectors(start_zone_1, start_zone_2)
    player:SetAbsOrigin(center)
    player:SetVelocity(Vector(0, 0, 0))
end

function PlayerTick(player)
    local velocity = player:GetVelocity()
    local speed = velocity:Length2D()
    local location = player:GetAbsOrigin()

    if player.is_in_start_zone == true then
        if speed > 350 then
            TeleportToStartZone(player)
        end
    end


    local speedHTML = "<font color=\"white\">Speed:</font><font color=\"#6EA6DD\"> " ..
        string.format("%06.2f", speed) .. "</font>"
    local br = "<br>"

    local html = ""
    if player.timer ~= nil then
        local currentTime = Time()
        local timeHTML = "<font color=\"white\">Time:</font><font color=\"#2E9F65\"> " ..
            FormatTime(currentTime - player.timer) .. "</font>"
        html = timeHTML .. br .. speedHTML
    else
        html = speedHTML
    end

    FireGameEvent("show_survival_respawn_status",
        {
            ["loc_token"] = html,
            ["duration"] = 5,
            ["userid"] = player.user_id
        }
    )
end

function Tick()
    local players = Entities:FindAllByClassname("player")
    for i, player in ipairs(players)
    do
        PlayerTick(players[i])
    end
    return FrameTime()
end

function CreateStartZone(v1, v2)
    local OnStartTouch = function(a, b)
        local player = b.activator
        player.timer = nil
        player.is_in_start_zone = true
    end
    local OnEndTouch = function(a, b)
        local player = b.activator
        player.timer = Time()
        player.is_in_start_zone = false
    end
    CreateZone("wst_trigger_startzone", v1, v2, 0, 230, 0, 10, OnStartTouch, OnEndTouch)
end

function CreateEndZone(v1, v2)
    local OnStartTouch = function(a, b)
        local player = b.activator
        if player.timer ~= nil then
            local time = Time() - player.timer
            updateLeaderboard(player, time)
            local position, total_players = getPlayerPosition(player.steam_id)
            ScriptPrintMessageChatAll("[WST] " ..
                player.name .. " finished in " .. time .. " seconds. Position: " .. position .. "/" .. total_players)
            player.timer = nil
        end
    end
    local OnEndTouch = function(a, b)
        local player = b.activator
        player.timer = nil
    end
    CreateZone("wst_trigger_endzone", v1, v2, 230, 0, 0, 10, OnStartTouch, OnEndTouch)
end

function CreateZone(name, v1, v2, r, g, b, a, OnStartTouch, OnEndTouch)
    local existing = Entities:FindByName(nil, name)
    if existing then
        -- Kill trigger
        existing:Kill()
    end

    local center, mins, maxs = CalculateBoxFromVectors(v1, v2)

    local extents = CalculateExtentsFromMinsMaxs(mins, maxs)

    ---@type CBaseTrigger
    local trigger = SpawnEntityFromTableSynchronous("script_trigger_multiple", {
        wait = 0,
        targetname = name,
        spawnflags = 257,
        StartDisabled = false,
        extent = extents
    })
    trigger:SetAbsOrigin(center)

    trigger:SetContextThink(nil, function()
        print("ACTIVE")
        local secondsToDrawBox = 5
        DebugDrawBox(center, mins, maxs, r, g, b, a, secondsToDrawBox)
        return secondsToDrawBox
    end, 0)

    local scriptScope = trigger:GetOrCreatePublicScriptScope()

    scriptScope.OnStartTouch = OnStartTouch
    scriptScope.OnEndTouch = OnEndTouch
    trigger:RedirectOutput("OnStartTouch", "OnStartTouch", trigger)
    trigger:RedirectOutput("OnEndTouch", "OnEndTouch", trigger)
end

function Activate()
    SendToServerConsole("sv_cheats 1")

    SendToServerConsole("mp_solid_teammates 0")


    SendToServerConsole("mp_ct_default_secondary weapon_usp_silencer")
    SendToServerConsole("mp_t_default_secondary weapon_usp_silencer")

    SendToServerConsole("sv_holiday_mode 0")
    SendToServerConsole("sv_party_mode 0")

    SendToServerConsole("mp_respawn_on_death_ct 1")
    SendToServerConsole("mp_respawn_on_death_t 1")
    SendToServerConsole("mp_respawn_immunitytime -1")

    -- Dropping guns
    SendToServerConsole("mp_death_drop_c4 1")
    SendToServerConsole("mp_death_drop_defuser 1")
    SendToServerConsole("mp_death_drop_grenade 1")
    SendToServerConsole("mp_death_drop_gun 1")
    SendToServerConsole("mp_drop_knife_enable 1")
    SendToServerConsole("mp_weapons_allow_map_placed 1")
    SendToServerConsole("mp_disconnect_kills_players 0")

    -- Hide money
    SendToServerConsole("mp_playercashawards 0")
    SendToServerConsole("mp_teamcashawards 0")

    -- Surf & BHOP
    SendToServerConsole("sv_airaccelerate 150")
    SendToServerConsole("sv_enablebunnyhopping 1")
    SendToServerConsole("sv_autobunnyhopping 1")
    SendToServerConsole("sv_falldamage_scale 0")
    SendToServerConsole("sv_staminajumpcost 0")
    SendToServerConsole("sv_staminalandcost 0")

    SendToServerConsole("mp_roundtime 60")
    SendToServerConsole("mp_roundtime_defuse 60")
    SendToServerConsole("mp_roundtime_hostage 60")
    SendToServerConsole("mp_round_restart_delay 0")
    SendToServerConsole("mp_warmuptime_all_players_connected 0")
    SendToServerConsole("mp_freezetime 0")
    SendToServerConsole("mp_team_intro_time 0")
    SendToServerConsole("mp_warmup_end")
    SendToServerConsole("mp_warmuptime 0")
    SendToServerConsole("bot_quota 0")
    SendToServerConsole("mp_autoteambalance 0")
    SendToServerConsole("mp_limitteams 0")
    SendToServerConsole("mp_spectators_max 64")
    SendToServerConsole("sv_cheats 0")


    if g_worldent ~= nil then
        g_worldent:SetContextThink(nil, nil, 0)
    end


    g_worldent = Entities:FindByClassname(nil, "worldent")
    g_worldent:SetContextThink(nil, Tick, 0)

    CreateStartZone(start_zone_1, start_zone_2)
    CreateEndZone(end_zone_1, end_zone_2)
end

local player_connect_table = {}

function EHandleToHScript(iPawnId)
    return EntIndexToHScript(bit.band(iPawnId, 0x3FFF))
end

Convars:RegisterCommand("wst_debug", function()
    local player = Convars:GetCommandClient()
    debugPrintPlayer(player)

    DeepPrintTable(player_connect_table)
end, nil, 0)


ListenToGameEvent("player_connect", function(event)
    player_connect_table[event.userid] = event
    print("player_connect" .. event.userid)
end, nil)

ListenToGameEvent("player_disconnect", function(event)
    player_connect_table[event.userid] = nil
    print("player_disconnect" .. event.userid)
end, nil)

ListenToGameEvent("player_spawn", function(event)
    local player_connect = player_connect_table[event.userid]
    local user = EHandleToHScript(event.userid_pawn)
    user.user_id = event.userid
    user.steam_id = player_connect.networkid
    user.name = player_connect.name
    user.ip_address = player_connect.address
end, nil)

if not pluginActivated then
    ListenToGameEvent("round_start", Activate, nil)
    pluginActivated = true
end
