using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public partial class Wst
{
    [ConsoleCommand("surftop", "Prints the top players by points")]
    [ConsoleCommand("ptop", "Prints the top players by points")]
    public void OnSurfTopCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        var style = playerInfo.Style;

        _ = Task.Run(async () =>
        {
            IEnumerable<V_Player> top10;
            if (style == "normal")
            {
                top10 = await _database.QueryAsync<V_Player>("SELECT * FROM v_players ORDER BY p_points DESC LIMIT 10");
            }
            else if (style == "lg")
            {
                top10 = await _database.QueryAsync<V_Player>("SELECT * FROM v_players ORDER BY lg_points DESC LIMIT 10");
            }
            else if (style == "tm")
            {
                top10 = await _database.QueryAsync<V_Player>("SELECT * FROM v_players ORDER BY tm_points DESC LIMIT 10");
            }
            else if (style == "sw")
            {
                top10 = await _database.QueryAsync<V_Player>("SELECT * FROM v_players ORDER BY sw_points DESC LIMIT 10");
            }
            else if (style == "hsw")
            {
                top10 = await _database.QueryAsync<V_Player>("SELECT * FROM v_players ORDER BY hsw_points DESC LIMIT 10");
            }
            else if (style == "prac")
            {
                top10 = await _database.QueryAsync<V_Player>("SELECT * FROM v_players ORDER BY prac_points DESC LIMIT 10");
            }
            else
            {
                throw new Exception("Unknown style");
            }

            Server.NextFrame(() =>
            {
                foreach (var player in top10)
                {
                    SkillGroup skillGroup;
                    if (style == "normal")
                    {
                        skillGroup = SkillGroups.GetSkillGroup(player.Rank, player.PPoints);
                    }
                    else if (style == "lg")
                    {
                        skillGroup = SkillGroups.GetSkillGroup(player.LgRank, player.LgPoints);
                    }
                    else if (style == "tm")
                    {
                        skillGroup = SkillGroups.GetSkillGroup(player.TmRank, player.TmPoints);
                    }
                    else if (style == "sw")
                    {
                        skillGroup = SkillGroups.GetSkillGroup(player.SwRank, player.SwPoints);
                    }
                    else if (style == "hsw")
                    {
                        skillGroup = SkillGroups.GetSkillGroup(player.HswRank, player.HswPoints);
                    } 
                    else if (style == "prac")
                    {
                        skillGroup = SkillGroups.GetSkillGroup(player.PracRank, player.PracPoints);
                    }
                    else
                    {
                        throw new Exception("Unknown style");
                    }
                    
                    int rank = 0;
                    if (style == "normal")
                    {
                        rank = player.Rank;
                    }
                    else if (style == "lg")
                    {
                        rank = player.LgRank;
                    }
                    else if (style == "tm")
                    {
                        rank = player.TmRank;
                    }
                    else if (style == "sw")
                    {
                        rank = player.SwRank;
                    }
                    else if (style == "hsw")
                    {
                        rank = player.HswRank;
                    } 
                    else if (style == "prac")
                    {
                        rank = player.PracRank;
                    }
                    else
                    {
                        throw new Exception("Unknown style");
                    }
                    
                    int points = 0;
                    if (style == "normal")
                    {
                        points = player.PPoints;
                    }
                    else if (style == "lg")
                    {
                        points = player.LgPoints;
                    }
                    else if (style == "tm")
                    {
                        points = player.TmPoints;
                    }
                    else if (style == "sw")
                    {
                        points = player.SwPoints;
                    }
                    else if (style == "hsw")
                    {
                        points = player.HswPoints;
                    }
                    else if (style == "prac")
                    {
                        points = player.PracPoints;
                    }
                    else
                    {
                        throw new Exception("Unknown style");
                    }

                    caller.PrintToChat(
                        $" {CC.Main}#{rank} {skillGroup.ChatColor}{player.Name} {CC.White} | {CC.Secondary}{points}{CC.White} points");
                }
            });
            return;
        });
    }

    [ConsoleCommand("mtop", "Prints the top 10 times on the current route")]
    [ConsoleCommand("top", "Prints the top 10 times on the current route")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTopCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        var currentRoute = playerInfo.RouteKey;
        var mapName = Server.MapName;

        _ = Task.Run(async () =>
        {
            var top10 = await _database.QueryAsync<V_RankedRecord>(
                "SELECT * FROM v_ranked_records WHERE mapname = @mapname and route = @route and style = @style ORDER BY position ASC LIMIT @n",
                new { mapname = mapName, route = currentRoute, n = 10, style = playerInfo.Style });
            Server.NextFrame(() =>
            {
                foreach (var record in top10)
                {
                    caller.PrintToChat($" {CC.White}#{record.Position}" + record.FormatForChat());
                }
            });
            return;
        });
        return;
    }

    [ConsoleCommand("mrank", "Prints your rank on the current route")]
    [ConsoleCommand("pr", "Prints your rank on the current route")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPrCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        var arg1 = command.GetArg(1);
        var arg2 = command.GetArg(2);

        var currentRoute = playerInfo.RouteKey;
        var mapName = Server.MapName;

        if (!string.IsNullOrEmpty(arg1))
        {
            mapName = arg1;
            currentRoute = "main";
        }

        if (!string.IsNullOrEmpty(arg2))
        {
            currentRoute = arg2;
        }

        _ = Task.Run(async () =>
        {
            Console.WriteLine(mapName + " " + currentRoute + " " + playerInfo.Style + " " + playerInfo.SteamId);
            var pr = await _database.QueryAsync<V_RankedRecord>(
                "SELECT * FROM v_ranked_records WHERE mapname = @mapname and route = @route and style = @style and steam_id = @steamid",
                new { mapname = mapName, route = currentRoute, steamid = playerInfo.SteamId, style = playerInfo.Style });

            Server.NextFrame(() =>
            {
                if (pr.Count() == 0)
                {
                    caller.PrintToChat($" {CC.White}You do not have a rank on this route");
                    return;
                }

                var record = pr.First();
                caller.PrintToChat(record.FormatForChat());
            });
            return;
        });
    }

    [ConsoleCommand("rank", "Prints your current server rank")]
    [ConsoleCommand("pinfo", "Prints your current server rank")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRankCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        var rank = playerInfo.StyleRank;
        var totalPlayers = playerInfo.PlayerStats.TotalPlayers;
        var totalPoints = playerInfo.StylePoints;

        var skillGroup = playerInfo.SkillGroup.Name;

        var style = "";
        if (playerInfo.Style != "normal")
        {
            style = $" {CC.White}Style: {CC.Secondary}{playerInfo.Style.ToUpper()} |";
        }
        
        var rankString = $" {CC.White}Rank: {CC.Secondary}{rank}{CC.White}/{CC.Secondary}{totalPlayers}{CC.White} | Points: {CC.Secondary}{totalPoints}{CC.White} | Skill Group: {playerInfo.SkillGroup.ChatColor}{skillGroup}";

        caller.PrintToChat(style + rankString);
            
    }

    [ConsoleCommand("wr", "Prints the current server record on the current would")]
    [ConsoleCommand("sr", "Prints the current server record on the current would")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnWrCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        var arg1 = command.GetArg(1);
        var arg2 = command.GetArg(2);
        
        Console.WriteLine(arg1);
        Console.WriteLine(arg2);

        var currentRoute = playerInfo.RouteKey;
        var mapName = Server.MapName;

        if (!string.IsNullOrEmpty(arg1))
        {
            mapName = arg1;
            currentRoute = "main";
        }

        if (!string.IsNullOrEmpty(arg2))
        {
            currentRoute = arg2;
        }

        _ = Task.Run(async () =>
        {
            var wr = await _database.QueryAsync<V_RankedRecord>(
                "SELECT * FROM v_ranked_records WHERE mapname = @mapname and route = @route and style = @style and position = 1",
                new { mapname = mapName, route = currentRoute, style = playerInfo.Style });

            Server.NextFrame(() =>
            {
                if (wr.Count() == 0)
                {
                    caller.PrintToChat($" {CC.White}There is no world record on this route");
                    return;
                }

                var record = wr.First();
                caller.PrintToChat(record.FormatForChat());
            });
            return;
        });
    }

    [ConsoleCommand("minfo", "Prints all the routes on the current map")]
    [ConsoleCommand("routes", "Prints all the routes on the current map")]
    public void OnRouteCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();

        caller.PrintToChat(" Available Routes:");
        foreach (var route in mapZones.Routes)
        {
            var cmd = $"!{route.Key}";
            var routeTier = route.Tier;
            var startVelocity = (new Vector(route.StartVelocity.x, route.StartVelocity.y, route.StartVelocity.z))
                .Length2D();
            caller.PrintToChat(
                $" {CC.Secondary}{cmd}{CC.White} | {CC.Secondary}{route.Name}{CC.White} | Tier: {CC.Secondary}{routeTier}{CC.White}");
        }
    }

    [ConsoleCommand("points", "Prints the points for the current route")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPointsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        var currentRoute = playerInfo.RouteKey;
        var mapName = Server.MapName;

        _ = Task.Run(async () =>
        {
            var positions = new List<int> { 1, 2, 3, 4, 5, 10, 50, 100, 500 };
            var positionParameter = string.Join(",", positions);
            var points = await _database.QueryAsync<V_RankedRecord>(
                "SELECT * FROM v_ranked_records WHERE mapname = @mapname and route = @route and style = @style and position in (" +
                positionParameter + ")",
                new { mapname = mapName, route = currentRoute, style = playerInfo.Style });


            Server.NextFrame(() =>
            {
                foreach (var position in positions)
                {
                    var record = points.FirstOrDefault(x => x.Position == position);
                    if (record == null)
                    {
                        caller.PrintToChat($" {CC.White}#{CC.Secondary}{position}{CC.White}: no record");
                        continue;
                    }

                    caller.PrintToChat(
                        $" {CC.White}#{CC.Secondary}{position}{CC.White}: {CC.Secondary}{record.Points}{CC.White} points");
                }
            });
            return;
        });
    }

    [ConsoleCommand("cpr", "Checkpoints difference between your time and the current server record")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCprCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var entity = _entityManager.FindEntity<SurfPlayer>(caller!.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        var currentRoute = playerInfo.RouteKey;

        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();

        var playerRecord = playerInfo.PR;
        var worldRecord = mapZones.GetWorldRecord(currentRoute, playerInfo.Style);

        if (playerRecord == null)
        {
            caller.PrintToChat($" {CC.White}You do not have a record on this route");
            return;
        }

        if (worldRecord == null)
        {
            caller.PrintToChat($" {CC.White}There is no world record on this route");
            return;
        }

        var playerCheckpoints = playerRecord.CheckpointsObject;
        var worldCheckpoints = worldRecord.CheckpointsObject;

        var playerCheckpointCount = playerCheckpoints.Count;
        var worldCheckpointCount = worldCheckpoints.Count;

        var checkpointCount = Math.Min(playerCheckpointCount, worldCheckpointCount);

        var checkpointTimeDifferences = new List<int> { 0 };
        var checkpointSpeedDifferences = new List<float> { worldRecord.VelocityStartXY - playerRecord.VelocityStartXY };
        for (var i = 0; i < checkpointCount; i++)
        {
            var playerCheckpoint = playerCheckpoints[i];
            var worldCheckpoint = worldCheckpoints[i];

            var checkpointDifference = worldCheckpoint.TimerTicks - playerCheckpoint.TimerTicks;
            checkpointTimeDifferences.Add(checkpointDifference);

            var playerCheckpointSpeed = worldCheckpoint.VelocityStartXY - playerCheckpoint.VelocityStartXY;
            checkpointSpeedDifferences.Add(playerCheckpointSpeed);
        }

        checkpointTimeDifferences.Add(worldRecord.Ticks - playerRecord.Ticks);
        checkpointSpeedDifferences.Add(worldRecord.VelocityEndXY - playerRecord.VelocityEndXY);


        var checkpointTimeDifferenceStrings =
            checkpointTimeDifferences.Select(x => Utils.FormatTimeWithPlusOrMinus(x)).ToList();

        var checkpointSpeedDifferenceStrings = checkpointSpeedDifferences.Select(x => x.ToString("0.00")).ToList();

        caller.PrintToChat($" {CC.White}CPR:");
        for (var i = 0; i < checkpointCount; i++)
        {
            var checkpointTimeDifferenceString = checkpointTimeDifferenceStrings[i];
            var checkpointSpeedDifferenceString = checkpointSpeedDifferenceStrings[i];
            caller.PrintToChat(
                $"  {CC.Main}#{i + 1}{CC.White}: {CC.Secondary}{checkpointTimeDifferenceString}{CC.White} | {CC.Secondary}{checkpointSpeedDifferenceString}{CC.White} u/s");
        }
    }

    [ConsoleCommand("ranks", "Prints info about all ranks")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRanksCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var ranks = SkillGroups.Groups;

        caller.PrintToChat(" Available Ranks:");
        foreach (var rank in ranks)
        {
            var idx = ranks.IndexOf(rank) + 1;
            var rankName = rank.Name;
            var rankColor = rank.ChatColor;
            var rankPoints = rank.Points;
            if (rankPoints == null)
            {
                var startPosition = rank.MinRank;
                var endPosition = rank.MaxRank;
                caller.PrintToChat(
                    $" {CC.Main}#{idx} {rankColor}{rankName}{CC.White} | {CC.Secondary}{startPosition}{CC.White} - {CC.Secondary}{endPosition}{CC.White}");
            }
            else
            {
                caller.PrintToChat(
                    $" {CC.Main}#{idx} {rankColor}{rankName}{CC.White} | {CC.Secondary}{rankPoints}{CC.White} points");
            }
        }
    }

    [ConsoleCommand("rr", "Recent Records")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRecentRecordsCommand(CCSPlayerController caller, CommandInfo info)
    {
        if (caller == null)
        {
            return;
        }


        _ = Task.Run(async () =>
        {
            var recentRecords = await _database.QueryAsync<RecentRecord>(
                "SELECT * FROM recent_records ORDER BY created_at DESC LIMIT 10");


            Server.NextFrame(() =>
            {
                foreach (var record in recentRecords)
                {
                    var mapName = record.MapName;
                    var route = record.Route;
                    var playerName = record.Name;
                    var time = Utils.FormatTime(record.Ticks);
                    var date = record.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                    var message =
                        $" {CC.Main}{mapName} {CC.White} | {CC.Secondary}{route} {CC.White} | {CC.Secondary}{playerName} {CC.White} | {CC.Secondary}{time} {CC.White}";

                    if (record.OldTicks.HasValue && record.OldName != null)
                    {
                        message += $"{CC.Main}(-{Utils.FormatTime(record.NewTicks - record.OldTicks.Value)})";
                        message += $" {CC.White}beating {CC.Secondary}{record.OldName} ";
                    }

                    caller.PrintToChat(message);
                }
            });
            return;
        });
    }

    public class IncompleteMap
    {
        public string Name { get; set; }
        public string Route { get; set; }
    }

    [ConsoleCommand("incomplete", "Prints incomplete maps to console")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnIncompleteCommand(CCSPlayerController caller, CommandInfo info)
    {
        if (caller == null)
        {
            return;
        }

        var type = info.GetArg(1);
        if (String.IsNullOrEmpty(type))
        {
            type = "main";
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller!.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        _ = Task.Run(async () =>
        {
            var incompleteMaps = await _database.QueryAsync<IncompleteMap>(
                @"SELECT
                      m.name,
                      m.route
                    FROM
                      maps_2 m
                    LEFT JOIN
                      records r ON m.name = r.mapname AND m.route = r.route AND r.steam_id = @steamid and style = @style
                    WHERE
                      r.mapname IS NULL", new { steamid = playerInfo.SteamId, style = playerInfo.Style });

            Server.NextFrame(() =>
            {
                var maps = incompleteMaps.Where(x => x.Route == "main" || x.Route.StartsWith("boost")).ToList();
                var bonuses = incompleteMaps.Where(x => x.Route.StartsWith("b")).ToList();
                var stages = incompleteMaps.Where(x => x.Route.StartsWith("s")).ToList();

                var toPrint = maps;
                if (type.StartsWith("b", StringComparison.InvariantCultureIgnoreCase))
                {
                    toPrint = bonuses;
                }
                else if (type.StartsWith("s", StringComparison.InvariantCultureIgnoreCase))
                {
                    toPrint = stages;
                }

                if (toPrint.Count == 0)
                {
                    caller.PrintToChat($" {CC.White}You have completed all {type} routes");
                    return;
                }

                if (toPrint.Count >= 9)
                {
                    // print first 8 to chat
                    foreach (var map in toPrint.Take(8))
                    {
                        caller.PrintToChat($" {CC.Main}{map.Name} {CC.White} | {CC.Secondary}{map.Route}");
                    }

                    caller.PrintToChat($" The remaining {toPrint.Count - 8} {type} routes will be printed to console");
                    foreach (var map in toPrint.Skip(8))
                    {
                        caller.PrintToConsole($" {map.Name}| {map.Route}");
                    }
                }
                else
                {
                    foreach (var map in toPrint)
                    {
                        caller.PrintToChat($" {CC.Main}{map.Name} {CC.White} | {CC.Secondary}{map.Route}");
                    }
                }
            });
        });
    }
}