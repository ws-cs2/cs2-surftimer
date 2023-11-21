function CreateZoneFromFile(key, value)
    if value.v1 ~= nil and value.v2 ~= nil then
        local targetname = "wst_trigger_" .. key
        return VectorZone.new(key, targetname, value.v1, value.v2)
    end
    if value.targetname ~= nil then
        return TriggerZone.new(key, value.targetname)
    end
    print("WST: CreateZoneFromFile: Invalid zone definition for " .. key)
    return nil
end

Zone = {}
Zone.__index = Zone

function Zone.new(name)
    local self = setmetatable({}, Zone)
    self.name = name
    return self
end

function Zone:init(OnStartTouch, OnEndTouch)
    print("Zone:init " .. self.name)

    self.trigger = self:CreateOrFindTrigger()
    local scriptScope = self.trigger:GetOrCreatePublicScriptScope()

    scriptScope.OnStartTouch = OnStartTouch
    scriptScope.OnEndTouch = OnEndTouch
    self.trigger:RedirectOutput("OnStartTouch", "OnStartTouch", self.trigger)
    self.trigger:RedirectOutput("OnEndTouch", "OnEndTouch", self.trigger)

    self.center = self.trigger:GetAbsOrigin()
    print("Zone:init: center = " .. tostring(self.center))
end

function Zone:CreateOrFindTrigger()
    print("CreateOrHookTrigger called from base Zone (shouldn't happen)")
end

function Zone:DrawZone(color)
    -- Calculate corners
    local v1, v2 = self:GetBounds()
    local corners = CalculateBoxCorners(v1, v2)

    -- https://developer.valvesoftware.com/wiki/Env_beam
    -- info_target, info_landmark and path_corner might not work as start/end points,
    -- info_teleport_destination is a suitable workaround.
    -- The reason is that those 3 entities are hardcoded to be considered "static" points.
    for i, corner in ipairs(corners) do
        local cornerName = "wst_zone_corner_" .. self.name .. "_" .. i
        local cornerExisting = Entities:FindByName(nil, cornerName)
        if cornerExisting then
            cornerExisting:Kill()
        end
        local cornerEnt = SpawnEntityFromTableSynchronous("info_teleport_destination", { targetname = cornerName })
        cornerEnt:SetOrigin(corner)
    end

    -- Indexs of pairs of corners to connect with beams to make a box
    local cornerPairs = {
        {1, 2},
        {1, 3},
        {1, 5},
        {2, 4},
        {2, 6},
        {3, 4},
        {3, 7},
        {4, 8},
        {5, 6},
        {5, 7},
        {6, 8},
        {7, 8}
    }
    for i, pair in ipairs(cornerPairs) do
        local beamName = "wst_zone_beam_" .. self.name .. "_" .. pair[1] .. "_" .. pair[2]
        local beamExisting = Entities:FindByName(nil, beamName)
        if beamExisting then
            beamExisting:Kill()
        end

        -- Spawn the beam
        SpawnEntityFromTableSynchronous("env_beam", {
            targetname = beamName,
            LightningStart = "wst_zone_corner_" .. self.name .. "_" .. pair[1],
            LightningEnd = "wst_zone_corner_" .. self.name .. "_" .. pair[2],
            rendercolor = color,
            renderamt = 255,
            renderfx = 14,
            life = 0,
            BoltWidth = 2
        })
    end
end

VectorZone = setmetatable({}, Zone)
VectorZone.__index = VectorZone

function VectorZone.new(name, targetname, v1, v2)
    local self = setmetatable(Zone.new(name), VectorZone)
    self.targetname = targetname
    self.v1 = SplitVectorString(v1)
    self.v2 = SplitVectorString(v2)
    return self
end

function VectorZone:GetBounds()
    -- script_trigger_multiple does not have its bounds set in engine for some reason
    return self.v1, self.v2
end

function VectorZone:CreateOrFindTrigger()
    local existing = Entities:FindByName(nil, self.targetname)
    if existing then
        -- Kill trigger
        existing:Kill()
    end
    local center, mins, maxs = CalculateBoxFromVectors(self.v1, self.v2)
    local extents = CalculateExtentsFromMinsMaxs(mins, maxs)

    ---@type CBaseTrigger
    local trigger = SpawnEntityFromTableSynchronous("script_trigger_multiple", {
        wait = 0,
        targetname = self.targetname,
        spawnflags = 257,
        StartDisabled = false,
        extent = extents
    })
    trigger:SetAbsOrigin(center)
    return trigger
end

TriggerZone = setmetatable({}, Zone)
TriggerZone.__index = TriggerZone

function TriggerZone.new(name, targetname)
    local self = setmetatable(Zone.new(name), TriggerZone)
    self.targetname = targetname
    return self
end

function TriggerZone:GetBounds()
    local mins = self.trigger:GetBoundingMins()
    local maxs = self.trigger:GetBoundingMaxs()
    local center = self.trigger:GetAbsOrigin()
    local v1, v2 = CalculateVectorsFromBox(center, mins, maxs)
    return v1, v2
end

function TriggerZone:CreateOrFindTrigger()
    local existing = Entities:FindByName(nil, self.targetname)
    if existing == nil then
        print("WST: TriggerZone:CreateOrFindTrigger: Couldn't find trigger " .. self.targetname)
        print("WST: The plugin will not load for this map")
        return nil
    end

    return existing
end
