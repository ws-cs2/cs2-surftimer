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
    print ("Zone:init: center = " .. tostring(self.center))
end

function Zone:CreateOrFindTrigger()
    print("CreateOrHookTrigger called from base Zone (shouldn't happen)")
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

function TriggerZone:CreateOrFindTrigger()
    local existing = Entities:FindByName(nil, self.targetname)
    if existing == nil then
        print("WST: TriggerZone:CreateOrFindTrigger: Couldn't find trigger " .. self.targetname)
        print("WST: The plugin will not load for this map")
        return nil
    end
        
    return existing
end
