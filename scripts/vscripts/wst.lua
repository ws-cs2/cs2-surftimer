-- Credits: https://github.com/GameChaos/cs2_things/blob/main/scripts/vscripts/kz.lua For the PlayerTick mechanism & server variables & show_survival_respawn_status
--          https://github.com/Source2ZE/ZombieReborn/tree/main EHandleToHScript
--          https://github.com/surftimer/SurfTimer script_trigger_multiple mechanism for making a timer
--          https://github.com/Source2ZE/LuaUnlocker Lua Unlocker

local CURRENT_VERSION = "_1.2.0"

require("wst/wst-leaderboard")
require("wst/wst-chat")
require("wst/wst-cvars")
require("wst/wst-debug")
require("wst/wst-utils")
require("wst/wst-hud")
require("wst/wst-timers")

print("--------------------")
print("Will's SurfTimer " .. CURRENT_VERSION)
print("--------------------")
local CURRENT_MAP = GetMapName()
print("Map: " .. CURRENT_MAP)
-- Big assumption here that a new script VM is made on map change
-- Seems to be true for changelevel & map commands

-- surf_beginner
-- host_workshop_map 3070321829

-- surf_utopia_njv
-- host_workshop_map 3073875025


local DRAW_ZONES = false
local START_ZONE_SPEED_CAP_XY = 400

local PLUGIN_ACTIVATED = false
local WORLDENT = nil

local START_ZONE_V1 = nil
local START_ZONE_V2 = nil

-- Some maps have multiple endzones
local END_ZONE_V1 = nil
local END_ZONE_V2 = nil

local END_ZONE_2_V1 = nil
local END_ZONE_2_V2 = nil

local PLAYER_CONNECT_TABLE = {}

function SendTextToClientConsole(player, text)
    local ptClientCommand = Entities:FindByClassname(nil, "point_clientcommand")

    if ptClientCommand == nil then
        ptClientCommand = SpawnEntityFromTableSynchronous("point_clientcommand",
            { targetname = "wst_point_clientcommand" })
    end

    DoEntFireByInstanceHandle(ptClientCommand, "command", "echo " .. text, 0.1, player, player)
end

function SendTextToClientChat(player, text)
    local slot = UserIdToSlot(player.user_id)
    -- I don't like prefixing with [wst] but I can't figured out colored text for client chat and otherwise it gets lost
    SendToServerConsole("wst_mm_chat " .. slot .. " " .. "\"[WST] " .. text .. "\"")
end

function CreateStartZone(v1, v2)
    local OnStartTouch = function(a, b)
        local player = b.activator
        if player:IsAlive() == false then
            return
        end

        -- Not in prac when they enter the start zone
        if player.prac == true then
            player.prac = false
        end

        player.timer = nil
        player.is_in_start_zone = true
    end
    local OnEndTouch = function(a, b)
        local player = b.activator
        if player:IsAlive() == false then
            return
        end

        local speed = player:GetVelocity():Length2D()

        -- Fix if hud starts breaking and stops ticking
        if speed > START_ZONE_SPEED_CAP_XY then
            TeleportToStartZone(player)
            return
        end

        
        player.timer = Time()
        player.is_in_start_zone = false 
    end
    CreateZone("wst_trigger_startzone", v1, v2, 0, 230, 0, 10, OnStartTouch, OnEndTouch)
end

function CreateEndZone(idx, v1, v2)
    local OnStartTouch = function(a, b)
        local player = b.activator
        if player:IsAlive() == false then
            return
        end

        if player.prac == true then
            SendTextToClientChat(player, "Finished in practice mode with a time of " .. FormatTime(Time() - player.timer) .. "!")
            return
        end

        if player.timer ~= nil then
            local wr = getWorldRecordTime()

            local previousPosition, _, previousTime = getPlayerPosition(player.steam_id)

            local time = Time() - player.timer
            updateLeaderboard(player, time)
            local position, total_players, _, tier = getPlayerPosition(player.steam_id)


            local tierColor = TIER_COLORS[tier]
            local tierString = " [" .. tierColor .. tier .. "<WHITE>]"

            local playerFinishMapMessage = "<GOLD>" ..
            player.name .. " <WHITE>finished in <GOLD>" .. FormatTime(time)
            local wrDiffString = ""
            if wr ~= nil then
                local wrDiff = time - wr
                if wrDiff > 0 then
                    wrDiffString = " <WHITE>[WR<RED> +" .. FormatTime(wrDiff) .. "<WHITE>]"
                elseif wrDiff <= 0 then
                    wrDiffString = " <WHITE>[WR<GREEN> -" .. FormatTime(wrDiff) .. "<WHITE>]"
                end
            end
            ScriptPrintMessageChatAll(ConvertTextToColoredChatString(playerFinishMapMessage .. wrDiffString))

            if previousPosition == nil then
                local newPlayerMessage = "<GOLD>" ..
                    player.name .. " <WHITE>is now rank <GOLD>" .. position .. "/" .. total_players .. tierString
                ScriptPrintMessageChatAll(ConvertTextToColoredChatString(newPlayerMessage))
            elseif time < previousTime then
                local improvementTime = previousTime - time

                local improvedPlayerMessage = "<GOLD>" ..
                    player.name ..
                    " <WHITE>improved with [<GREEN>-" ..
                    FormatTime(improvementTime) ..
                    "<WHITE>] Rank <GOLD>" .. position .. "/" .. total_players .. tierString
                ScriptPrintMessageChatAll(ConvertTextToColoredChatString(improvedPlayerMessage))
            else
                local worsePlayerMessage = "<GOLD>" ..
                    player.name ..
                    " <WHITE>missed their best time by [<RED>+" ..
                    FormatTime(time - previousTime) ..
                    "<WHITE>] Rank <GOLD>" .. position .. "/" .. total_players .. tierString
                ScriptPrintMessageChatAll(ConvertTextToColoredChatString(worsePlayerMessage))
            end

            local noPreviousRecords = wr == nil
            local betPreviousWr = wr ~= nil and time < wr

            if noPreviousRecords or betPreviousWr then
                local newWRMessage = "<GOLD>" .. player.name .. " <WHITE>set a new <GOLD>WR <WHITE>with <GOLD>" ..
                    FormatTime(time) .. "!"
                ScriptPrintMessageChatAll(ConvertTextToColoredChatString(newWRMessage))

                FireGameEvent("cs_win_panel_round", {
                    ["funfact_token"] = "<font class='fontSize-xl' color='#5BEB60'>" ..
                        player.name .. " </font><font class='fontSize-l'>just set a new server record!</font>",
                })
                TimerOnce(10, function()
                    FireGameEvent("round_start", nil)
                end, "clear_win_panel")
            end
        end
        player.timer = nil
    end
    local OnEndTouch = function(a, b)
        local player = b.activator
        if player:IsAlive() == false then
            return
        end

        player.timer = nil
    end
    CreateZone("wst_trigger_endzone_" .. idx, v1, v2, 230, 0, 0, 10, OnStartTouch, OnEndTouch)
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

    if DRAW_ZONES then
        trigger:SetContextThink(nil, function()
            local secondsToDrawBox = 5
            DebugDrawBox(center, mins, maxs, r, g, b, a, secondsToDrawBox)
            return secondsToDrawBox
        end, 0)
    end

    local scriptScope = trigger:GetOrCreatePublicScriptScope()

    scriptScope.OnStartTouch = OnStartTouch
    scriptScope.OnEndTouch = OnEndTouch
    trigger:RedirectOutput("OnStartTouch", "OnStartTouch", trigger)
    trigger:RedirectOutput("OnEndTouch", "OnEndTouch", trigger)
end

function SplitVectorString(str)
    -- split on comma
    -- x y z
    local split = {}
    for s in string.gmatch(str, "([^,]+)") do
        table.insert(split, s)
    end
    -- Convert to numbers
    for i, s in ipairs(split) do
        split[i] = tonumber(s)
    end
    -- Convert to vector
    return Vector(split[1], split[2], split[3])
end

function LoadZones(zone_file_table)
    print("Zones loaded from disk")
    print("Zones Version: ", zone_file_table.version)
    START_ZONE_V1 = SplitVectorString(zone_file_table.data.start.v1)
    START_ZONE_V2 = SplitVectorString(zone_file_table.data.start.v2)

    END_ZONE_V1 = SplitVectorString(zone_file_table.data['end'].v1)
    END_ZONE_V2 = SplitVectorString(zone_file_table.data['end'].v2)

    -- Support multi endzone.
    if zone_file_table.data['end2'] ~= nil then
        END_ZONE_2_V1 = SplitVectorString(zone_file_table.data['end2'].v1)
        END_ZONE_2_V2 = SplitVectorString(zone_file_table.data['end2'].v2)
    end
end

-- !version
function CommandVersion(player, SendText)
    SendText(player, "Will's SurfTimer " .. CURRENT_VERSION)
end

-- !getpos
function CommandGetPos(player, SendText)
    local location = player:GetAbsOrigin()
    SendText(player, location.x .. ", " .. location.y .. ", " .. location.z)
end

-- !r
function CommandR(player, SendText)
    TeleportToStartZone(player)
end

-- !top
function CommandTop(player, SendText)
    local topPlayers = getTopPlayers(10)

    for i, p in ipairs(topPlayers)
    do
        local position, total_players = getPlayerPosition(p.steam_id)
        SendText(player, position .. " " .. p.name .. " " .. FormatTime(p.time))
    end
end

-- !wr
function CommandWr(player, SendText)
    local wrTime = getTopPlayers(1)[1]
    if wrTime ~= nil then
        SendText(player, "Server record " .. FormatTime(wrTime.time) .. " by " .. wrTime.name)
    else
        SendText(player, "No server record set yet!")
    end
end

-- !pr
function CommandPr(player, SendText)
    local previousPosition, _, previousTime = getPlayerPosition(player.steam_id)
    if previousPosition == nil then
        SendText(player, "You have no record on this map!")
    else
        SendText(player, "Your personal record was " .. FormatTime(previousTime))
    end
end

-- !cp
function CommandCp(player, SendText)
    player.cp_saved = true
    player.cp_origin = player:GetAbsOrigin()
    player.cp_angles = player:EyeAngles()
    player.cp_velocity = player:GetVelocity()
    player.cp_time_diff = Time()
    SendText(player, "Saved your current position! Use !tele to teleport back to it.")
end

-- !tele
function CommandTele(player, SendText)
    if player.cp_saved then
        player:SetAbsOrigin(player.cp_origin)
        player:SetAngles(player.cp_angles.x, player.cp_angles.y, player.cp_angles.z)
        player:SetVelocity(player.cp_velocity)
        player.prac = true
        player.timer = player.cp_time_diff
        player.cp_time_diff = Time()
    else
        SendText(player, "You have not saved a position yet!")
    end
end

-- !spec
function CommandSpec(player, SendText)
    player:SetTeam(1)
end

-- !hidehud
function CommandHideHud(player, SendText)
    player.hide_hud = true
    SendText(player, "Hud hidden")
end

-- !showhud
function CommandShowHud(player, SendText)
    player.hide_hud = false
    SendText(player, "Hud visible")
end

function CommandChatHelp(player, SendText)
    SendText(player, "Will\'s SurfTimer Commands")
    SendText(player, "Type !r to teleport to the start zone (bind t wst_r)")
    SendText(player, "Type !wr to see the server record")
    SendText(player, "Type !top to see the top 10 players on this map")
    SendText(player, "Type !cp to save your current position")
    SendText(player, "Type !tele to teleport to your saved position")
    SendText(player, "Type !spec to go to spectator team")
    SendText(player, "Type wst_help in console for more commands")
end

WST_COMMANDS = {
    ["version"] = {
        console = CommandVersion,
        chat = CommandVersion,
        help = "Shows the version of the plugin"
    },
    ["getpos"] = {
        console = CommandGetPos,
        chat = CommandGetPos,
        help = "Shows your current x, y, z location (for zoning maps)"
    },
    ["r"] = {
        console = CommandR,
        chat = CommandR,
        help = "Teleport to the start zone"
    },
    ["top"] = {
        console = CommandTop,
        chat = CommandTop,
        help = "Show the top 10 players on this map"
    },
    ["wr"] = {
        console = CommandWr,
        chat = CommandWr,
        help = "Show the server record"
    },
    ["sr"] = {
        console = CommandWr,
        chat = CommandWr,
        help = "Show the server record"
    },
    ["pr"] = {
        console = CommandPr,
        chat = CommandPr,
        help = "Show your personal record on this map"
    },
    ["cp"] = {
        console = CommandCp,
        chat = CommandCp,
        help = "Save your current position"
    },
    ["tele"] = {
        console = CommandTele,
        chat = CommandTele,
        help = "Teleport to your saved position (stops your timer)"
    },
    ["spec"] = {
        console = CommandSpec,
        chat = CommandSpec,
        help = "Go to spectator team"
    },
    ["hidehud"] = {
        console = CommandHideHud,
        chat = CommandHideHud,
        help = "Hide the hud"
    },
    ["showhud"] = {
        console = CommandShowHud,
        chat = CommandShowHud,
        help = "Show the hud"
    },
    ["help"] = {
        console = nil,
        chat = CommandChatHelp,
        help = "Shows the help menu"
    }
}

-- lua is retarded
-- tables are not ordered unless they are array tables
WST_COMMAND_ORDER = {
    "r",
    "top",
    "wr",
    "sr",
    "pr",
    "cp",
    "tele",
    "spec",
    "hidehud",
    "showhud",
    --    "version",
    "getpos",
    "help"
}

function GetLongestCommandNameLength()
    local longest = 0
    for name, cmd in pairs(WST_COMMANDS) do
        if string.len(name) > longest then
            longest = string.len(name)
        end
    end
    return longest
end

-- do help seperately since it needs to reference the command list


-- local zones = LoadKeyValues('scripts/wst_zones/surf_beginner_debug.txt')
local zones = LoadKeyValues('scripts/wst_zones/' .. CURRENT_MAP .. '.txt')
if zones == nil then
    print("Failed to load WST, there is no zone file for this map: " .. CURRENT_MAP)
    return
end

LoadZones(zones)

Convars:RegisterCommand("wst_help", function()
    local player = Convars:GetCommandClient()
    local text = "Will's SurfTimer"
    local border = string.rep("-", string.len(text) + 4)
    local middleLine = "| " .. text .. " |"
    SendTextToClientConsole(player, border)
    SendTextToClientConsole(player, middleLine)
    SendTextToClientConsole(player, border)
    SendTextToClientConsole(player, "\"https://github.com/ws-cs2/cs2-surftimer\"")
    SendTextToClientConsole(player, "\"https://discord.gg/ujMfSwSXCy\"")
    SendTextToClientConsole(player, "")
    SendTextToClientConsole(player, "Respawn Bind:")
    SendTextToClientConsole(player, "bind t wst_r - Binds t on your keyboard to respawn, you can change t to any key")
    SendTextToClientConsole(player, "")
    SendTextToClientConsole(player, "Commands:")
    local length = GetLongestCommandNameLength()

    for i, name in ipairs(WST_COMMAND_ORDER)
    do
        local cmd = WST_COMMANDS[name]
        local padding = string.rep(" ", length - string.len(name))
        SendTextToClientConsole(player, "!" .. name .. padding .. " / wst_" .. name .. padding .. " - " .. cmd.help)
    end
    SendTextToClientConsole(player, "")
    SendTextToClientConsole(player, "Credits:")
    SendTextToClientConsole(player, "will - Plugin creator")
    SendTextToClientConsole(player, "b3nny (FutureGN) - Zoning maps and testing")
end, nil, 0)

-- Register commands
for name, cmd in pairs(WST_COMMANDS) do
    if cmd.console ~= nil then
        Convars:RegisterCommand("wst_" .. name, function(...)
            local player = Convars:GetCommandClient()
            cmd.console(player, SendTextToClientConsole)
        end, nil, 0)
    end
end


function TeleportToStartZone(player)
    local center, _, _ = CalculateBoxFromVectors(START_ZONE_V1, START_ZONE_V2)
    player:SetAbsOrigin(center)
    player:SetVelocity(Vector(0, 0, 0))
end

function ConvertTSpawnsToCTSpawns()
    print("[WST] ConvertTSpawnsToCTSpawns.start")
    -- Convert T spawns to CT spawns
    for i, entity in pairs(Entities:FindAllByClassname("info_player_terrorist")) do
        SpawnEntityFromTableSynchronous("info_player_counterterrorist", { origin = entity:GetOrigin() })
        entity:Kill()
    end

    -- Get the current number of CT spawns
    local ctSpawns = Entities:FindAllByClassname("info_player_counterterrorist")
    local numberOfCTSpawns = #ctSpawns
    -- Check if there are less than 64 CT spawns
    if numberOfCTSpawns < 64 then
        -- Get the origin of the first CT spawn
        local firstCTSpawnOrigin
        if ctSpawns[1] then
            firstCTSpawnOrigin = ctSpawns[1]:GetOrigin()
        else
            print("Error: No CT spawns found.")
            return
        end

        -- Add additional CT spawns at the origin of the first CT spawn until there are 64
        for i = numberOfCTSpawns + 1, 64 do
            SpawnEntityFromTableSynchronous("info_player_counterterrorist", { origin = firstCTSpawnOrigin })
        end
    end
    print("[WST] ConvertTSpawnsToCTSpawns.end")
end

function PlayerTick(player)
    local velocity = player:GetVelocity()
    local speed = velocity:Length2D()

    if player:IsAlive() == false then
        player.timer = nil
        player.is_in_start_zone = false
        return
    end

    if player.is_in_start_zone == true then
        if speed > START_ZONE_SPEED_CAP_XY then
            TeleportToStartZone(player)
        end
    end

    -- TODO: See if I can space out the hud with unicode chars
    -- local UNICODE_EMPTY_CHARACTER = "⠀"

    if player.hide_hud == true then
        return
    end

    local playerHtml = BuildPlayerHudHtml(player, speed)

    FireGameEvent("show_survival_respawn_status",
        {
            ["loc_token"] = playerHtml,
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

function Activate()
    print("[WST] Activate.start")

    SurfCVars()

    if WORLDENT ~= nil then
        WORLDENT:SetContextThink(nil, nil, 0)
    end

    WORLDENT = Entities:FindByClassname(nil, "worldent")
    if WORLDENT == nil then
        print("No world ent")
        return
    end
    WORLDENT:SetContextThink(nil, Tick, 0)

    ConvertTSpawnsToCTSpawns()
    CreateStartZone(START_ZONE_V1, START_ZONE_V2)
    CreateEndZone("1", END_ZONE_V1, END_ZONE_V2)
    if END_ZONE_2_V1 ~= nil and END_ZONE_2_V2 ~= nil then
        CreateEndZone("2", END_ZONE_2_V1, END_ZONE_2_V2)
    end

    TimerOnce(1, function()
        ScriptPrintMessageChatAll(ConvertTextToColoredChatString(
            "<GREEN>Will's SurfTimer <GOLD>loaded"))
        ServerMessageHelp()
    end, "activation_msg")
    print("[WST] Activate.end")
end

ListenToGameEvent("player_connect", function(event)
    PLAYER_CONNECT_TABLE[event.userid] = event
    print("player_connect" .. event.userid)
end, nil)

ListenToGameEvent("player_disconnect", function(event)
    PLAYER_CONNECT_TABLE[event.userid] = nil
    print("player_disconnect" .. event.userid)
end, nil)

ListenToGameEvent("player_spawn", function(event)
    local player_connect = PLAYER_CONNECT_TABLE[event.userid]
    -- print("PLAYER_SPAWN [userid]: " .. event.userid)
    -- print("PLAYER_SPAWN [userid_slot]: " .. UserIdToSlot(event.userid))
    -- print("PLAYER_SPAWN [userid_pawn]: " .. event.userid_pawn)
    -- if player_connect ~= nil then
    --     print("PLAYER_SPAWN [networkid]: " .. player_connect.networkid)
    --     print("PLAYER_SPAWN [name]: " .. player_connect.name)
    --     print("PLAYER_SPAWN [address]: " .. player_connect.address)
    -- end

    local user = EHandleToHScript(event.userid_pawn)
    user.user_id = event.userid

    if player_connect ~= nil then
        user.steam_id = player_connect.networkid
        user.name = player_connect.name
        user.ip_address = player_connect.address
    end
end, nil)

ListenToGameEvent("player_chat", function(event)
    -- Slot is the index of the player between 0 and 63
    -- EntityIndex for playercontroller is slot+1
    -- slot = userid & 0xFF
    -- in our code because I didn't know what I was doing at the time thought user_id was the slot

    -- Another issue is that some plugins are sending player_chat with the ent index of the playercontroller
    -- instead of the userid/slot

    -- This will majorly screw chat commands

    print("PLAYER_CHAT [userid]: " .. event.userid)
    local chatPlayer = GetPlayerByUserId(event.userid)

    for name, cmd in pairs(WST_COMMANDS) do
        if event.text == "!" .. name or event.text == "/" .. name then
            cmd.chat(chatPlayer, SendTextToClientChat)
            return
        end
    end
end, nil)


function ServerMessageHelp()
    ScriptPrintMessageChatAll(ConvertTextToColoredChatString(
        "<GOLD>Type <GREEN>!help<GOLD> in chat for <GREEN>Will's SurfTimer<GOLD> commands"))
    return 90
end

function ServerMessageTop()
    local wrTime = getTopPlayers(1)[1]
    if wrTime ~= nil then
        ScriptPrintMessageChatAll(ConvertTextToColoredChatString(
            "<GOLD>Server record <GREEN>" .. FormatTime(wrTime.time) .. "<GOLD> by <GREEN>" .. wrTime.name))
    end
    return 90
end

function CreateServerMessageTimer()
    local entities = {
        ["wst_server_message_welcome"] = {
            ["callback"] = function()
                return nil
            end,
            ["delay"] = 15
        },
        ["wst_server_message_top"] = {
            ["callback"] = ServerMessageTop,
            ["delay"] = 30
        },
        ["wst_server_message_help"] = {
            ["callback"] = ServerMessageHelp,
            ["delay"] = 60
        },
    }

    for name, cb in pairs(entities) do
        TimerOnce(cb.delay, function()
            print("MAKING TIMER FOR " .. name .. " " .. cb.delay)
            Timers:CreateTimer(cb.callback, name)
        end, name)
    end
end

if not PLUGIN_ACTIVATED then
    ListenToGameEvent("round_start", Activate, nil)
    CreateServerMessageTimer()
    PLUGIN_ACTIVATED = true
end
