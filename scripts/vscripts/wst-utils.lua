function EHandleToHScript(iPawnId)
    return EntIndexToHScript(bit.band(iPawnId, 0x3FFF))
end

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
