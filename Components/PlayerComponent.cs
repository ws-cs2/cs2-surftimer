using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using WST.Game;

namespace WST;

public class SavedLocations
{
    public ZoneVector SaveLocPosition { get; set; }
    public ZoneVector SaveLocAngles { get; set; }
    public ZoneVector SaveLocVelocity { get; set; }
    public string Style { get; set; }
    public string RouteKey { get; set; }
    public PlayerTimerComponent Timer { get; set; } = null;
}

public class PlayerComponent
{
    public PlayerComponent(int slot, string steamId, string name, string style)
    {
        this.Slot = slot;
        this.SteamId = steamId;
        this.Name = name;
        this.Style = style;
    }
    public bool Teleporting { get; set; }
    public int Slot { get; set; }
    public string SteamId { get; set; }
    
    public string Name { get; set; }
    public string RouteKey { get; private set; } = "main";

    public string Style { get; set; } = "normal";
    public string? SecondaryRouteKey { get; set; } = null;
    
    public bool RepeatMode { get; set; } = false;
    
    public ZoneVector? CustomStartPos { get; set; }
    public ZoneVector? CustomStartAng { get; set; }
    
    public void ChangeRoute(string routeKey)
    {
        RouteKey = routeKey;
        Style = "normal";
        SecondaryRouteKey = null;
        CustomStartPos = null;
        CustomStartAng = null;
    }
    
    public void ChangeRoute(string routeKey, string style)
    {
        RouteKey = routeKey;
        Style = style;
        SecondaryRouteKey = null;
        CustomStartPos = null;
        CustomStartAng = null;
    }
    
    public void ChangeSecondaryRoute(string routeKey)
    {
        SecondaryRouteKey = routeKey;
    }
    
    public Dictionary<string, Dictionary<string, V_RankedRecord>> PlayerRecords { get; set; } = new();
    
    public List<SavedLocations> SavedLocations { get; set; } = new();
    public int CurrentSavelocIndex { get; set; } = 0;
    public V_Player PlayerStats { get; set; }
    
    public bool InPrac { get; set; } = false;

    public bool ShowHud { get; set; } = true;

    public bool ShowKeys { get; set; } = false;
    
    public bool ShowCustomHud { get; set; }
    
    public V_RankedRecord? PR
    {
        get
        {
            if (PlayerRecords.ContainsKey(RouteKey))
            {
                if (PlayerRecords[RouteKey].ContainsKey(Style))
                {
                    return PlayerRecords[RouteKey][Style];
                }
            }

            return null;
        }
    }

    public int StylePoints
    {
        get
        {
            if (PlayerStats == null)
            {
                return 0;
            }
            
            if (Style == "normal")
            {
                return PlayerStats.PPoints;
            }
            else if (Style == "lg")
            {
                return PlayerStats.LgPoints;
            }
            else if (Style == "tm")
            {
                return PlayerStats.TmPoints;
            }
            else if (Style == "sw")
            {
                return PlayerStats.SwPoints;
            }
            else if (Style == "hsw")
            {
                return PlayerStats.HswPoints;
            }
            else if (Style == "prac")
            {
                return PlayerStats.PracPoints;
            }
            else
            {
                return 0;
            }
        }
    }
    
    public int StyleRank
    {
        get
        {
            if (PlayerStats == null)
            {
                return 0;
            }
            
            Console.WriteLine(Style);
            
            if (Style == "normal")
            {
                return PlayerStats.Rank;
            }
            else if (Style == "lg")
            {
                return PlayerStats.LgRank;
            }
            else if (Style == "tm")
            {
                return PlayerStats.TmRank;
            }
            else if (Style == "sw")
            {
                return PlayerStats.SwRank;
            }
            else if (Style == "hsw")
            {
                return PlayerStats.HswRank;
            }
            else if (Style == "prac")
            {
                return PlayerStats.PracRank;
            }
            else
            {
                return 0;
            }
        }
    }

    public void InMemoryUpdatePR(V_RankedRecord pr, string route, string style)
    {
        PlayerRecords[route] = PlayerRecords.GetValueOrDefault(route, new Dictionary<string, V_RankedRecord>());
        PlayerRecords[route][style] = pr;
    }

    public string ChatRank()
    {
        if (PlayerStats != null && PlayerStats.CustomTag != null)
        {
            var x = Utils.RemoveColorNames(PlayerStats.CustomTag);
            return x;
        }
        return $"{SkillGroup.Name}";
    }

    public string ChatRankFormatted()
    {
        if (PlayerStats != null && PlayerStats.CustomTag != null)
        {
            return  $"{ChatColors.White}[{Utils.ColorNamesToTags(PlayerStats.CustomTag)}{ChatColors.White}]";
        }

        if (Style == "normal")
        {
            return $"{ChatColors.White}[{SkillGroup.ChatColor}{SkillGroup.Name}{ChatColors.White}]";
        }
        else
        {
            return $"{ChatColors.White}[{SkillGroup.ChatColor}{Style.ToUpper()}-{SkillGroup.Name}{ChatColors.White}{ChatColors.White}]";
            
        }
    }

    public SkillGroup SkillGroup
    {
        get
        {
            return SkillGroups.GetSkillGroup(this.StyleRank, this.StylePoints);
        }
    }

    public async Task LoadStats(Database db)
    {
        Console.WriteLine(SteamId);
        var playerStats =
            (await db.QueryAsync<V_Player>("SELECT * FROM v_players WHERE steam_id = @steamid",
                new { steamid = SteamId })).FirstOrDefault();
        if (playerStats == null)
        {
            Console.WriteLine("Player record is null");
            return;
        }
        
        Console.WriteLine(JsonSerializer.Serialize(playerStats));
        
        PlayerStats = playerStats;
    }

    public async Task LoadRecords(Database db, string currentMap)
    {
        var records = (await db.QueryAsync<V_RankedRecord>("SELECT * FROM v_ranked_records WHERE steam_id = @steamid and mapname = @mapname",
            new { steamid = SteamId, mapname = currentMap })).ToList();
        foreach (var record in records)
        {
            PlayerRecords[record.Route] = PlayerRecords.GetValueOrDefault(record.Route, new Dictionary<string, V_RankedRecord>());
            PlayerRecords[record.Route][record.Style] = record;
        }
    }

    public CCSPlayerController GetPlayer()
    {
        var player = Utilities.GetPlayerFromSlot(Slot);
        if (player == null) throw new Exception($"Player with slot {Slot} not found");
        return player;
    }

}