using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public partial class Wst
{
    [ConsoleCommand("r", "Restarts you back to the starting location of the current route")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        // Open your console and type `bind <key> sm_r` to bind this command to a key
        // Restarting via chat commands is annoying for players on the server!
        caller.PrintToChat(
            $" {CC.White}Open your console (~) and type {CC.Secondary}bind <key> sm_r{CC.White} to bind this command to a key");
        Restart(caller);
    }
    
    [ConsoleCommand("sm_stuck", "Sets a custom start position for the current route")]
    [ConsoleCommand("wst_stuck", "Sets a custom start position for the current route")]
    [ConsoleCommand("stuck", "Teleports you to the start of a stage")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnStuckCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (playerInfo.SecondaryRouteKey == null)
        {
            Restart(caller);
            return;
        }
        
        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        var route = mapZones.GetRoute(playerInfo.SecondaryRouteKey);
        
        if (route == null)
        {
            Restart(caller);
            return;
        }
        
        var startPos = route.StartPos;
        var startAngles = route.StartAngles;

        var startPosVector = new Vector(startPos.x, startPos.y, startPos.z);
        var startPosAngles = new QAngle(startAngles.x, startAngles.y, startAngles.z);
        var startVelocityVector = new Vector(0, 0, 0);

        
        caller.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
        caller.PlayerPawn.Value!.Teleport(startPosVector, startPosAngles, startVelocityVector);
        
    }
    
    [ConsoleCommand("sm_repeat", "Sets a custom start position for the current route")]
    [ConsoleCommand("wst_repeat", "Sets a custom start position for the current route")]
    [ConsoleCommand("repeat", "When your done with a route teleports you back to the start")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRepeatCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        
        playerInfo.RepeatMode = !playerInfo.RepeatMode;
        
        if (playerInfo.RepeatMode)
        {
            caller.PrintToChat($" {CC.White}Repeat mode {CC.Secondary}enabled");
        }
        else
        {
            caller.PrintToChat($" {CC.White}Repeat mode {CC.Secondary}disabled");
        }
    }
    
    [ConsoleCommand("normal", "Normal surf style")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnNormalCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.Style = "normal";
        
        
        Restart(caller);

        caller.PrintToChat($" {CC.White}Normal surf {CC.Main}enabled");
    }
    
    [ConsoleCommand("sw", "Sideways surf style")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSwCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.Style = playerInfo.Style == "sw" ? "normal" : "sw";
        
        
        Restart(caller);
        
        if (playerInfo.Style == "sw")
        {
            caller.PrintToChat($" {CC.White}Sideways surf {CC.Secondary}enabled");
        }
        else
        {
            caller.PrintToChat($" {CC.White}Sideways surf {CC.Secondary}disabled");
        }
    }
    
    [ConsoleCommand("hsw", "Half-Sideways surf style")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHswCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.Style = playerInfo.Style == "hsw" ? "normal" : "hsw";
        
        
        Restart(caller);
        
        if (playerInfo.Style == "hsw")
        {
            caller.PrintToChat($" {CC.White}Half-Sideways surf {CC.Secondary}enabled");
        }
        else
        {
            caller.PrintToChat($" {CC.White}Half-Sideways surf {CC.Secondary}disabled");
        }
    }
    
    [ConsoleCommand("lg", "Low gravity style")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnLgCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.Style = playerInfo.Style == "lg" ? "normal" : "lg";
        
        
        Restart(caller);
        
        if (playerInfo.Style == "lg")
        {
            caller.PrintToChat($" {CC.White}Low gravity {CC.Main}enabled");
        }
        else
        {
            caller.PrintToChat($" {CC.White}Low gravity {CC.Main}disabled");
        }
    }
    
    [ConsoleCommand("tm", "Turbomaster Style")]
    [ConsoleCommand("turbo", "Turbomaster Style")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTmCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.Style = playerInfo.Style == "tm" ? "normal" : "tm";
        
        
        Restart(caller);
        
        if (playerInfo.Style == "tm")
        {
            caller.PrintToChat($" {CC.White}Turbo {CC.Main}enabled");
        }
        else
        {
            caller.PrintToChat($" {CC.White}Turbo {CC.Main}disabled");
        }
    }

    [ConsoleCommand("sm_startpos", "Sets a custom start position for the current route")]
    [ConsoleCommand("wst_startpos", "Sets a custom start position for the current route")]
    [ConsoleCommand("startpos", "Sets a custom start position for the current route")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnStartposCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        StartPos(caller);
    }

    public void StartPos(CCSPlayerController caller)
    {
        var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        var route = mapZones.GetRoute(playerInfo.RouteKey);

        if (route == null)
        {
            caller.PrintToChat($" {CC.White}You must be on a route to set the start position");
            return;
        }

        var startZoneTriggerInfo = route.Start.TriggerInfo;

        if (startZoneTriggerInfo == null)
        {
            caller.PrintToChat($" {CC.White}Something went wrong");
            return;
        }

        // check player is on ground
        const int FL_ONGROUND = 1 << 0;

        if ((caller.Pawn.Value.Flags & FL_ONGROUND) == 0)
        {
            caller.PrintToChat($" {CC.White}You must be on the ground to set the start position");
            return;
        }


        var player = playerInfo.GetPlayer();
        var playerPos = player.Pawn.Value.AbsOrigin;
        var playerAngle = player.PlayerPawn.Value.EyeAngles;

        var startPos = new ZoneVector(playerPos.X, playerPos.Y, playerPos.Z);
        var startAngles = new ZoneVector(playerAngle.X, playerAngle.Y, playerAngle.Z);

        if (!startZoneTriggerInfo.IsInside(startPos))
        {
            caller.PrintToChat($" {CC.White}You must be inside the start zone to set the start position");
            return;
        }

        playerInfo.CustomStartPos = startPos;
        playerInfo.CustomStartAng = startAngles;

        caller.PrintToChat($" {CC.White}Start position set to your current position");
    }


    [ConsoleCommand("wst_r", "Restarts you back to the starting location of the current route")]
    [ConsoleCommand("sm_r", "Restarts you back to the starting location of the current route")]
    [ConsoleCommand("os_r", "Restarts you back to the starting location of the current route")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRestartCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        Restart(caller);
    }

    public void Restart(CCSPlayerController? caller)
    {
        try
        {
            var entity = _entityManager.FindEntity<SurfPlayer>(caller.Slot.ToString());
            var playerInfo = entity.GetComponent<PlayerComponent>();
            entity.RemoveComponent<PlayerTimerComponent>();
            entity.RemoveComponent<PlayerReplayComponent>();

            var map = _entityManager.FindEntity<Map>();
            var mapZones = map.GetComponent<MapZone>();
            var route = mapZones.GetRoute(playerInfo.RouteKey);

            ZoneVector startPos;
            ZoneVector startAngles;
            ZoneVector startVelocity;

            if (playerInfo.CustomStartPos != null && playerInfo.CustomStartAng != null)
            {
                startPos = playerInfo.CustomStartPos;
                startAngles = playerInfo.CustomStartAng;
                startVelocity = new ZoneVector(0, 0, 0);
            }
            else
            {
                startPos = route.StartPos;
                startAngles = route.StartAngles;
                startVelocity = route.StartVelocity;
            }

            var startPosVector = new Vector(startPos.x, startPos.y, startPos.z);
            var startPosAngles = new QAngle(startAngles.x, startAngles.y, startAngles.z);
            var startVelocityVector = new Vector(startVelocity.x, startVelocity.y, startVelocity.z);

            if (playerInfo.Style == "prac")
            {
                playerInfo.Style = "normal";
            }

            if (caller.TeamNum == (int)CsTeam.Spectator)
            {
                caller.PrintToChat(" You cannot restart while spectating, press M to join a team");
                // caller.ChangeTeam(CsTeam.CounterTerrorist);
                // Server.NextFrame(() =>
                // {
                //     caller.Respawn();
                // });
            }
            else
            {
                if (playerInfo.Style == "lg")
                {
                    caller.PlayerPawn.Value!.GravityScale = 0.5f;
                }
                else
                {
                    caller.PlayerPawn.Value!.GravityScale = 1f;
                }
                
                caller.PlayerPawn.Value!.MoveType = MoveType_t.MOVETYPE_WALK;
                caller.PlayerPawn.Value!.Teleport(startPosVector, startPosAngles, startVelocityVector);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}