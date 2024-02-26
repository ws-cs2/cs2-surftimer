using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public partial class Wst
{
        
    [ConsoleCommand("prac", "Prac Help")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPracCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null)
        {
            return;
        }

        // caller.PrintToChat($" {CC.White}Open your console (~) and type {CC.Secondary}bind <key> sm_r{CC.White} to bind this command to a key");
        
        // You can enter prac mode by first setting a saved location with sm_saveloc (while surfing in normal mode)
        // Then once you teleport to that saveloc with sm_tele your timer, position, and velocity will be restored
        // and you will be in prac mode.
        
        // To exit prac mode you can either type sm_r or sm_restart in console or you can type sm_saveloc to save
        
        caller.PrintToChat($" {CC.White}To enter prac mode you must first set a saveloc with {CC.Secondary}sm_saveloc");
        caller.PrintToChat($" {CC.White}Then you can teleport to that saveloc with {CC.Secondary}sm_tele");
        caller.PrintToChat($" {CC.White}To exit prac mode you can type {CC.Secondary}sm_r {CC.White}or enter the startzone");
        caller.PrintToChat($" {CC.White}If you make a bad saveloc you can go back/forward checkpoints with {CC.Secondary}sm_prevloc {CC.White}and {CC.Secondary}sm_nextloc");
        caller.PrintToChat($" {CC.White}To create saveloc binds type the following in console:");
        caller.PrintToChat($" {CC.Secondary}bind <key> sm_saveloc");
        caller.PrintToChat($" {CC.Secondary}bind <key> sm_tele");

    }
    
    [ConsoleCommand("wst_cp", "Save location")]
    [ConsoleCommand("wst_saveloc", "Save location")]
    [ConsoleCommand("saveloc", "Save location")]
    [ConsoleCommand("sm_saveloc", "Save location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSavelocCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        
        var map = _entityManager.FindEntity<Map>();
        var mapZones = map.GetComponent<MapZone>();
        var route = mapZones.GetRoute(playerInfo.RouteKey);

        if (route == null)
        {
            return;
        }

        var playerPosition = new ZoneVector
        {
            x = player.PlayerPawn.Value!.AbsOrigin.X, y = player.PlayerPawn.Value!.AbsOrigin.Y,
            z = player.PlayerPawn.Value!.AbsOrigin.Z
        };

        var startZoneTriggerInfo = route.Start.TriggerInfo;
        if (startZoneTriggerInfo.IsInside(playerPosition))
        {
            StartPos(playerInfo.GetPlayer());
            return;
        }

        var saveloc = new SavedLocations();
        saveloc.SaveLocPosition = playerPosition;
        saveloc.Style = playerInfo.Style;
        Console.WriteLine("Loc style " + saveloc.Style);
        saveloc.RouteKey = playerInfo.RouteKey;

        saveloc.SaveLocAngles = new ZoneVector
        {
            x = player.PlayerPawn.Value.EyeAngles.X, y = player.PlayerPawn.Value.EyeAngles.Y,
            z = player.PlayerPawn.Value.EyeAngles.Z
        };

        saveloc.SaveLocVelocity = new ZoneVector
        {
            x = player.PlayerPawn.Value.AbsVelocity.X, y = player.PlayerPawn.Value.AbsVelocity.Y,
            z = player.PlayerPawn.Value.AbsVelocity.Z
        };
        
        var timer = entity.GetComponent<PlayerTimerComponent>();
        if (timer != null && (saveloc.Style == "normal" || saveloc.Style == "prac"))
        {
            while (true)
            {
                var oneHourOfReplayFrames = 230400; // 10Mb
                var tenHoursOfReplayFrames = oneHourOfReplayFrames * 10; // 100Mb
                var replayFrameCount = 0;
                foreach (var locs in playerInfo.SavedLocations)
                {
                    replayFrameCount += locs.Timer.Primary.Recording.Frames.Count;
                    replayFrameCount += locs.Timer.Secondary?.Recording.Frames.Count ?? 0;
                }
        
                if (replayFrameCount > tenHoursOfReplayFrames)
                {
                    playerInfo.SavedLocations.RemoveAt(0);
                    player.PrintToChat($" {CC.White}You have reached the saveloc limit, removing oldest saveloc");
                }
                else
                {
                    break;
                }
            }
            saveloc.Timer = timer.DeepCopy();
        }

        if (playerInfo.SavedLocations.Count > 100)
        {
            playerInfo.SavedLocations.RemoveAt(0);
            player.PrintToChat($" {CC.White}You have reached the saveloc limit, removing oldest saveloc");
        }

        playerInfo.SavedLocations.Add(saveloc);

        var saveLocCount = playerInfo.SavedLocations.Count;
        playerInfo.CurrentSavelocIndex = saveLocCount - 1;

        player.PrintToChat(
            $" {CC.White}Saved location {CC.Secondary}#{saveLocCount}");
    }

    [ConsoleCommand("wst_tele", "Tp to saved location")]
    [ConsoleCommand("tele", "Tp to saved location")]
    [ConsoleCommand("sm_tele", "Tp to saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTeleCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (!player.NativeIsValidAliveAndNotABot())
        {
            return;
        }

        GoToSaveloc(player);
    }
    
    

    public void GoToSaveloc(CCSPlayerController player)
    {
        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (playerInfo.SavedLocations.Count == 0)
        {
            player.PrintToChat($" {CC.White}No saveloc's");
            return;
        }

        var saveloc = playerInfo.SavedLocations[playerInfo.CurrentSavelocIndex];
        if (saveloc == null)
        {
            player.PrintToChat($" {CC.White}No saveloc's");
            return;
        }

        var pos = new Vector(saveloc.SaveLocPosition.x, saveloc.SaveLocPosition.y, saveloc.SaveLocPosition.z);
        var ang = new QAngle(saveloc.SaveLocAngles.x, saveloc.SaveLocAngles.y,
            saveloc.SaveLocAngles.z);
        var vel = new Vector(saveloc.SaveLocVelocity.x, saveloc.SaveLocVelocity.y,
            saveloc.SaveLocVelocity.z);


        entity.RemoveComponent<PlayerTimerComponent>();
        
        var style = saveloc.Style;
        // only goto prac if you were in normal or prac style
        if (saveloc.Timer != null)
        {
            // if we are restoring a timer we always go to prac
            style = "prac";
            entity.AddComponent(saveloc.Timer.DeepCopy());
        }
        playerInfo.ChangeRoute(saveloc.RouteKey, style);
        
        if (playerInfo.Style == "lg")
        {
            player.PlayerPawn.Value!.GravityScale = 0.5f;
        }
        else
        {
            player.PlayerPawn.Value!.GravityScale = 1f;
        }
        

        playerInfo.InPrac = true;
        playerInfo.Teleporting = true;
        player.PlayerPawn.Value!.Teleport(pos, ang, vel);
        Console.WriteLine("TELE");
        player.PrintToChat($" {CC.White}Teleported to saveloc {CC.Secondary}#{playerInfo.CurrentSavelocIndex + 1}");
        
        Server.NextFrame(() =>
        {
            Console.WriteLine("NOT TELE");
            playerInfo.Teleporting = false;
        });
    }

    [ConsoleCommand("wst_prevloc", "Tp to saved location")]
    [ConsoleCommand("prevloc", "Tp to saved location")]
    [ConsoleCommand("sm_prevloc", "Tp to saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPrevLocCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (!player.NativeIsValidAliveAndNotABot())
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (playerInfo.SavedLocations.Count == 0)
        {
            player.PrintToChat($" {CC.White}No saveloc's");
            return;
        }
        
        // Remove the current loc
        playerInfo.SavedLocations.RemoveAt(playerInfo.CurrentSavelocIndex);
        
        playerInfo.CurrentSavelocIndex--;
        if (playerInfo.CurrentSavelocIndex < 0)
        {
            playerInfo.CurrentSavelocIndex = playerInfo.SavedLocations.Count - 1;
        }

        GoToSaveloc(player);
    }
    
    [ConsoleCommand("wst_backloc", "Tp to saved location")]
    [ConsoleCommand("backloc", "Tp to saved location")]
    [ConsoleCommand("sm_backloc", "Tp to saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBackLocCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (!player.NativeIsValidAliveAndNotABot())
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (playerInfo.SavedLocations.Count == 0)
        {
            player.PrintToChat($" {CC.White}No saveloc's");
            return;
        }
        
        playerInfo.CurrentSavelocIndex--;
        if (playerInfo.CurrentSavelocIndex < 0)
        {
            playerInfo.CurrentSavelocIndex = playerInfo.SavedLocations.Count - 1;
        }

        GoToSaveloc(player);
    }
    
    [ConsoleCommand("wst_nextloc", "Tp to next saved location")]
    [ConsoleCommand("nextloc", "Tp to next saved location")]
    [ConsoleCommand("sm_nextloc", "Tp to next saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnNextLocCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        if (!player.NativeIsValidAliveAndNotABot())
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (playerInfo.SavedLocations.Count == 0)
        {
            player.PrintToChat($" {CC.White}No saveloc's");
            return;
        }

        // Increment the current index and wrap around if it exceeds the number of saved locations
        playerInfo.CurrentSavelocIndex = (playerInfo.CurrentSavelocIndex + 1) % playerInfo.SavedLocations.Count;

        GoToSaveloc(player);
    }
}