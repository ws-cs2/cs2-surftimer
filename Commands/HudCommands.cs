using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace WST;

public partial class Wst
{
    [ConsoleCommand("hidehud")]
    [ConsoleCommand("showhud")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHideHud(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        if (!player.IsValid || player.IsBot)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.ShowHud = !playerInfo.ShowHud;

        player.PrintToChat($" {CC.White}Hud is now {CC.Secondary}{(playerInfo.ShowHud ? "visible" : "hidden")}");
    }

    [ConsoleCommand("keys")]
    [ConsoleCommand("showkeys")]
    [ConsoleCommand("hidekeys")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnShowKeys(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        if (!player.IsValid || player.IsBot)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        playerInfo.ShowKeys = !playerInfo.ShowKeys;

        player.PrintToChat($" {CC.White}Keys is now {CC.Secondary}{(playerInfo.ShowHud ? "visible" : "hidden")}");
    }

    [ConsoleCommand("customhud")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCustomHud(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }

        if (!player.IsValid || player.IsBot)
        {
            return;
        }

        var entity = _entityManager.FindEntity<SurfPlayer>(player.Slot.ToString());
        var playerInfo = entity.GetComponent<PlayerComponent>();

        if (playerInfo.PlayerStats == null)
        {
            return;
        }

        var customHudExists = String.IsNullOrEmpty(playerInfo.PlayerStats.CustomHudUrl);
        if (customHudExists)
        {
            player.PrintToChat(
                $" {CC.White}You do not have a custom hud. Custom huds are a VIP only feature. Join the discord and message {CC.Secondary}will {CC.White}to get VIP.");
            return;
        }


        playerInfo.ShowCustomHud = !playerInfo.ShowCustomHud;

        player.PrintToChat(
            $" {CC.White}CustomHud is now {CC.Secondary}{(playerInfo.ShowHud ? "visible" : "hidden")} {CC.White}(It is always visible to spectators or in replays)");
    }
}