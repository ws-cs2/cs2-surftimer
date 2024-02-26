using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public partial class Wst
{
    [ConsoleCommand("servers", "Prints servers")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnServersCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            var gameServers = await _database.QueryAsync<GameServer>("select * from servers");
            Server.NextFrame(() =>
            {
                foreach (var server in gameServers)
                {
                    if (server.IsPublic)
                    {
                        caller.PrintToChat(
                            $" {CC.Main}{server.Hostname} {CC.White} | {CC.Secondary}{server.CurrentMap} | {CC.Secondary}{server.PlayerCount}/{server.TotalPlayers} | {CC.Secondary}connect {server.IP}");
                    }
                }
            });
        });
    }

    class StyleInfo
    {
        public string Command { get; set; }
        public string Name { get; set; }
        public string Explaination { get; set; }
    }
    
    [ConsoleCommand("style", "Prints styles")]
    [ConsoleCommand("styles", "Prints styles")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnStylesCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var styles = new List<StyleInfo>
        {
            new StyleInfo {Command = "normal", Name = "Normal", Explaination = "Normal"},
            new StyleInfo {Command = "turbo", Name = "Turbo", Explaination = "Left click to go faster, right click to go slow"},
            new StyleInfo {Command = "lg", Name = "Low Gravity", Explaination = "Play the map with half of normal gravity"},
            new StyleInfo {Command = "sw", Name = "Sideways Surf", Explaination = "Play the map with sideways surf (only use W and S)"},
            new StyleInfo {Command = "hsw", Name = "Half-Sideways Surf", Explaination = "Play the map with HSW surf (hold two movement keys to surf half sideways ie W and A)"},
            new StyleInfo {Command = "prac", Name = "Practice Mode/TAS", Explaination = "Use checkpoints to create the ultimate run"},
        };
        
        caller.PrintToChat(" Available Styles:");
        foreach (var style in styles)
        {
            // var cmd = $"!{styles}";
            // var routeTier = route.Tier;
            // var startVelocity = (new Vector(route.StartVelocity.x, route.StartVelocity.y, route.StartVelocity.z))
            //     .Length2D();
            // caller.PrintToChat(
            //     $" {CC.Secondary}{cmd}{CC.White} | {CC.Secondary}{route.Name}{CC.White} | Tier: {CC.Secondary}{routeTier}{CC.White}");
            
            caller.PrintToChat(
                $" {CC.Secondary}!{style.Command}{CC.White} | {CC.Main}{style.Name}{CC.White} | {CC.White}{style.Explaination}");
        }

    }

    [ConsoleCommand("help", "help")]
    [ConsoleCommand("commands", "comamnds")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHelpCommand(CCSPlayerController player, CommandInfo info)
    {
        var commands = new List<string>
        {
            "r", "styles", "wr", "pr", "rank", "cpr", "top", "surftop", "replay", "routes", "points", "ranks", "help", "rr",
            "incomplete"
        };
        var str = String.Join(", ", commands);
        player.PrintToChat($" Commands: {CC.Secondary}{str}");
    }

  

    private bool IsInSpec(CCSPlayerController? caller)
    {
        if (caller == null) return false;
        return caller.TeamNum == (int)CsTeam.Spectator;
    }

    [ConsoleCommand("spec", "Moves you to spec.")]
    [ConsoleCommand("wst_spec", "Moves you to spec.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSpecCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        entity.RemoveComponent<PlayerTimerComponent>();
        entity.RemoveComponent<PlayerReplayComponent>();

        if (caller.TeamNum == (int)CsTeam.Spectator)
        {
            return;
        }

        if (caller.TeamNum == (int)CsTeam.Terrorist || caller.TeamNum == (int)CsTeam.CounterTerrorist)
        {
            caller.ChangeTeam(CsTeam.Spectator);
        }

        return;
    }
}