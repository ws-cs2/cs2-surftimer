using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class SurfEntity : Entity
{
    public CCSPlayerController GetPlayer()
    {
        var player = Utilities.GetPlayerFromSlot(Int32.Parse(this.Id));
        if (player == null) throw new Exception($"Player with slot {this.Id} not found");
        return player;
    }
}

public class SurfPlayer : SurfEntity
{
}

public class SurfBot : SurfEntity
{
}

public class Map : Entity
{
}

public class ReplayEntity : Entity
{
}

public class Entity
{
    public readonly Dictionary<Type, object?> Components = new();
    public string Id;

    public void AddComponent<T>(T? component)
    {
        Components[typeof(T)] = component;
    }

    public T? GetComponent<T>()
    {
        if (!HasComponent<T>()) return default;
        return (T)Components[typeof(T)]!;
    }

    public bool HasComponent<T>()
    {
        return Components.ContainsKey(typeof(T));
    }

    public void RemoveComponent<T>()
    {
        Components.Remove(typeof(T));
    }
}