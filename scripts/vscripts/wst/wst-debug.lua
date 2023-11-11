function DebugPrintTable(table)
    for k, v in pairs(table) do
        print(k, v)
    end
end

function DebugPrintPlayer(player)
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
