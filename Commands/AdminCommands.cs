using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace WST;

public partial class Wst
{
    [ConsoleCommand("wst_set_tier", "Set map tier")]
    [CommandHelper(minArgs: 1, usage: "[tier]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetMapTierCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsAdmin)
        {
            return;
        }

        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        var route = mapZones.GetRoute(playerInfo.RouteKey);
        if (route == null)
        {
            return;
        }

        var tier = info.GetArg(1);
        if (!int.TryParse(tier, out var tierInt))
        {
            return;
        }

        route.Tier = tierInt;
        _ = mapZones.Save(_database, Server.MapName);

        Server.PrintToChatAll(
            $" {CC.Main}[ADMIN] {CC.Secondary}{playerInfo.Name} {CC.White}set tier for {CC.Secondary}{route.Name} {CC.White}to {CC.Secondary}{tierInt}");
    }

    [ConsoleCommand("wst_set_startpos", "Set map tier")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetStartpos(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsAdmin)
        {
            return;
        }

        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        var route = mapZones.GetRoute(playerInfo.RouteKey);
        if (route == null)
        {
            return;
        }

        route.StartPos = new ZoneVector
        {
            x = player.PlayerPawn.Value!.AbsOrigin.X, y = player.PlayerPawn.Value!.AbsOrigin.Y,
            z = player.PlayerPawn.Value!.AbsOrigin.Z
        };
        route.StartAngles = new ZoneVector
        {
            x = player.PlayerPawn.Value.EyeAngles.X, y = player.PlayerPawn.Value.EyeAngles.Y,
            z = player.PlayerPawn.Value.EyeAngles.Z
        };
        route.StartVelocity = new ZoneVector
        {
            x = player.PlayerPawn.Value.AbsVelocity.X, y = player.PlayerPawn.Value.AbsVelocity.Y,
            z = player.PlayerPawn.Value.AbsVelocity.Z
        };

        _ = mapZones.Save(_database, Server.MapName);

        Server.PrintToChatAll(
            $" {CC.Main}[ADMIN] {CC.Secondary}{playerInfo.Name} {CC.White}update startpos for {CC.Secondary}{route.Name}");
    }

    [ConsoleCommand("wst_add_cfg", "Add map CFG")]
    [CommandHelper(minArgs: 1, usage: "[cmd] [val]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAddCfg(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsAdmin)
        {
            return;
        }

        var mapName = Server.MapName;

        var cmd = "";
        for (var i = 1; i < info.ArgCount; i++)
        {
            cmd += info.GetArg(i) + " ";
        }

        cmd = cmd.Trim();

        _ = _database.ExecuteAsync("INSERT INTO map_cfg (mapname, command) VALUES (@mapname, @command)",
            new { mapname = mapName, command = cmd });

        Server.ExecuteCommand(cmd);

        Server.PrintToChatAll(
            $" {CC.Main}[ADMIN] {CC.Secondary}{playerInfo.Name} {CC.White}added map CFG {CC.Secondary}{cmd}");
    }

    [ConsoleCommand("wst_show_cfg", "Shows map CFG")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnShowCfg(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsAdmin)
        {
            return;
        }

        var mapName = Server.MapName;

        _ = Task.Run(async () =>
        {
            var cfgs = await _database.QueryAsync<MapCfg>("SELECT * FROM map_cfg WHERE mapname = @mapname",
                new { mapname = mapName });

            Server.NextFrame(() =>
            {
                foreach (var cfg in cfgs)
                {
                    player.PrintToChat($" {CC.Main}[CFG] {CC.Secondary}{cfg.Command}");
                }
            });
            return;
        });
    }

    

    [ConsoleCommand("wst_ent_debug", "Debug entites")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnEntDebug(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsAdmin)
        {
            return;
        }

        var debug = _entityManager.DebugEntities();

        foreach (var d in debug)
        {
            Server.PrintToChatAll(d);
        }
    }
}