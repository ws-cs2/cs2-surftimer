using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public partial class Wst
{

    // admins can change map always
    // vips can only change map on vip server
    public bool HasMapChangePermission(bool isVip, bool isAdmin, bool isVipServer)
    {
        if (isAdmin || isVip)
        {
            return true;
        }

        return false;
    }
    
    
    [ConsoleCommand("map", "Change Map")]
    [ConsoleCommand("wst_map", "Change map")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnChangeMap(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (!HasMapChangePermission(playerInfo.PlayerStats.IsVip, playerInfo.PlayerStats.IsAdmin,
                _currentGameServer.Vip))
        {
            return;
        }
        

        var mapName = info.GetArg(1);
        

        var map = _workshopMaps.Where(x => x.Title.Equals(mapName, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        if (map.Count == 0)
        {
            player.PrintToChat($" {ChatColors.Purple}[VIP] {CC.White}map not found");
            return;
        }

        var mapId = map.First().Publishedfileid;

        Server.ExecuteCommand($"host_workshop_map {mapId}");
    }
    
    [ConsoleCommand("chattag", "Change Chat Tag")]
    [ConsoleCommand("wst_chattag", "Change chat tag")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnChangeChatTag(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }
        
        Console.WriteLine(info.GetCommandString);
        Console.WriteLine(info.ArgString);
        Console.WriteLine(info.GetArg(1));

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (!playerInfo.PlayerStats.IsVip)
        {
            player.PrintToChat(NoVipMessage);
            return;
        }

        var chatTag = info.ArgString;
        
        if (string.IsNullOrEmpty(chatTag))
        {
            playerInfo.PlayerStats.CustomTag = null;
            Task.Run(async () =>
            {
                await _database.ExecuteAsync("UPDATE players SET custom_tag = NULL WHERE steam_id = @steamId",
                    new { steamId = playerInfo.SteamId });
            });
            player.PrintToChat($" {ChatColors.Purple}[VIP] {CC.White}Chat tag reset");
            return;
        }
        
        // check chattag incldues whitespace
        if (chatTag.Contains(" "))
        {
            player.PrintToChat($" {ChatColors.Purple}[VIP] {CC.White}Chat tag cannot include whitespace");
            return;
        }
        
        var withoutColor = Utils.RemoveColorNames(chatTag);
        if (withoutColor.Length > 15)
        {
            player.PrintToChat($" {ChatColors.Purple}[VIP] {CC.Secondary}{playerInfo.Name} {CC.White}chat tag cannot be longer than 15 characters");
            return;
        }
        
        playerInfo.PlayerStats.CustomTag = chatTag;
        Task.Run(async () =>
        {
            await _database.ExecuteAsync("UPDATE players SET custom_tag = @customTag WHERE steam_id = @steamId",
                new { customTag = chatTag, steamId = playerInfo.SteamId });
        });
        // player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Chat color reset");
        player.PrintToChat($" {ChatColors.Purple}[VIP] {CC.White}chat tag set to {Utils.ColorNamesToTags(chatTag)}");
        
    }
    
    [ConsoleCommand("chatcolor", "Change Chat Color")]
    [ConsoleCommand("wst_chatcolor", "Change chat color")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnChangeChatColor(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (!playerInfo.PlayerStats.IsVip)
        {
            player.PrintToChat(NoVipMessage);
            return;
        }
        
        var chatColor = info.ArgString;
        if (string.IsNullOrEmpty(chatColor))
        {
            playerInfo.PlayerStats.ChatColor = null;
            Task.Run(async () =>
            {
                await _database.ExecuteAsync("UPDATE players SET chat_color = NULL WHERE steam_id = @steamId",
                    new { steamId = playerInfo.SteamId });
            });
            player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Chat color reset");
            return;
        }

        if (chatColor.Contains(" "))
        {
            player.PrintToChat($" {ChatColors.Purple}[VIP] {CC.White}Chat tag cannot include whitespace");
            return;
        }

        if (!Utils.IsStringAColorName(chatColor))
        {
            player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Invalid color, try !chatcolor {{LIME}}");
            return;
        }     
        
        playerInfo.PlayerStats.ChatColor = chatColor;
        Task.Run(async () =>
        {
            await _database.ExecuteAsync("UPDATE players SET chat_color = @chatColor WHERE steam_id = @steamId",
                new { chatColor, steamId = playerInfo.SteamId });
        });
        player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Chat color set to {Utils.ColorNamesToTags(chatColor)}{chatColor}");
    }
    
    [ConsoleCommand("namecolor", "Change Name Color")]
    [ConsoleCommand("wst_namecolor", "Change Name color")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnChangeNameColor(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (!playerInfo.PlayerStats.IsVip)
        {
            player.PrintToChat(NoVipMessage);
            return;
        }
        
        var nameColor = info.ArgString;
        if (string.IsNullOrEmpty(nameColor))
        {
            playerInfo.PlayerStats.NameColor = null;
            Task.Run(async () =>
            {
                await _database.ExecuteAsync("UPDATE players SET name_color = NULL WHERE steam_id = @steamId",
                    new { steamId = playerInfo.SteamId });
            });
            player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Name color reset");
            return;
        }
        

        if (!Utils.IsStringAColorName(nameColor))
        {
            player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Invalid color, try !namecolor {{LIME}}");
            return;
        }     
        
        playerInfo.PlayerStats.NameColor = nameColor;
        Task.Run(async () =>
        {
            await _database.ExecuteAsync("UPDATE players SET name_color = @nameColor WHERE steam_id = @steamId",
                new { nameColor, steamId = playerInfo.SteamId });
        });
        player.PrintToChat($" {ChatColors.Purple}[VIP] {ChatColors.White}Name color set to {Utils.ColorNamesToTags(nameColor)}{nameColor}");
    }
    
    
    
    [ConsoleCommand("extend", "Extend map time")]
    [ConsoleCommand("wst_extend", "Extend map time")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnExtendMap(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsVip)
        {
            player.PrintToChat(NoVipMessage);
            return;
        }

        Server.ExecuteCommand("c_cs2f_extend 15");
        Server.ExecuteCommand("c_extend 15");
    }
    
    [ConsoleCommand("replay", "Replay")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnReplayCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        
        var position = 1;

        var target = info.GetArg(1);
        Console.WriteLine(target);
        if (target != null)
        {
            if (target.StartsWith("@"))
            {
                var removeAt = target.Replace("@", "");
                if (!int.TryParse(removeAt, out position))
                {
                    player.PrintToChat($" {CC.White}Invalid position");
                    return;
                }
            }
          
        }

        if (position != 1)
        {
            if (!playerInfo.PlayerStats.IsVip)
            {
                player.PrintToChat(NoVipMessage);
                return;
            }
        }
        
        _eventManager.Publish(new LoadReplayEvent { MapName = Server.MapName, PlayerSlot = player.Slot, Position = position });
    }
    
    [ConsoleCommand("prreplay", "Your PR")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPrReplayCommand(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
        {
            return;
        }
        
        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();
        if (!playerInfo.PlayerStats.IsVip)
        {
            player.PrintToChat(NoVipMessage);
            return;
        }

        _eventManager.Publish(new LoadReplayEvent { MapName = Server.MapName, PlayerSlot = player.Slot, Position = -1 });
    }
}