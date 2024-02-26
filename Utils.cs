using System.Drawing;
using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public static class Utils
{
    public static string NativeSteamId3(this CCSPlayerController controller)
    {
        var steamId64 = controller.SteamID;
        var steamId32 = (steamId64 - 76561197960265728).ToString();
        var steamId3 = $"[U:1:{steamId32}]";
        return steamId3;
    }
    
    public static string SteamId3ToSteamId64(string steamId3)
    {
        steamId3 = steamId3.Replace("[", "");
        steamId3 = steamId3.Replace("]", "");
        var steamId3Split = steamId3.Split(":");
        var steamId32 = int.Parse(steamId3Split[2]);
        var steamId64 = steamId32 + 76561197960265728;
        return steamId64.ToString();
    }

    public static bool NativeIsValidAliveAndNotABot(this CCSPlayerController? controller)
    {
        return controller != null && controller is { IsValid: true, IsBot: false, PawnIsAlive: true };
    }

    // public static string FormatTime(decimal time)
    // {
    //     var doubleTime = (double)time;
    //     double abs = Math.Abs(doubleTime);
    //     int minutes = (int)(abs / 60);
    //     int seconds = (int)(abs - minutes * 60);
    //     int milliseconds = (int)((abs - Math.Floor(abs)) * 1000);
    //     return string.Format("{0:D2}:{1:D2}:{2:D3}", minutes, seconds, milliseconds);
    // }

    public static string FormatTime(int ticks)
    {
        var timeSpan = TimeSpan.FromSeconds(Math.Abs(ticks) / 64.0);

        // Format seconds with three decimal points
        var secondsWithMilliseconds = $"{timeSpan.Seconds:D2}.{Math.Abs(ticks) % 64 * (1000.0 / 64.0):000}";

        return $"{timeSpan.Minutes:D2}:{secondsWithMilliseconds}";
    }
    
    public static string FormatTimeWithPlusOrMinus(int ticks)
    {
        var timeSpan = TimeSpan.FromSeconds(Math.Abs(ticks) / 64.0);

        // Format seconds with three decimal points
        var secondsWithMilliseconds = $"{timeSpan.Seconds:D2}.{Math.Abs(ticks) % 64 * (1000.0 / 64.0):000}";

        return $"{(ticks > 0 ? "-" : "+")}{timeSpan.Minutes:D2}:{secondsWithMilliseconds}";
    }
    
    public static float DistanceTo(this Vector v1, Vector v2)
    {
        var dx = v1.X - v2.X;
        var dy = v1.Y - v2.Y;
        var dz = v1.Z - v2.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
    
    public static void AdjustPlayerVelocity(CCSPlayerController? player, float velocity)
    {
        if (!player.NativeIsValidAliveAndNotABot()) return;

        var currentX = player.PlayerPawn.Value.AbsVelocity.X;
        var currentY = player.PlayerPawn.Value.AbsVelocity.Y;
        var currentSpeed2D = Math.Sqrt(currentX * currentX + currentY * currentY);
        var normalizedX = currentX / currentSpeed2D;
        var normalizedY = currentY / currentSpeed2D;
        var adjustedX = normalizedX * velocity; // Adjusted speed limit
        var adjustedY = normalizedY * velocity; // Adjusted speed limit
        player.PlayerPawn.Value.AbsVelocity.X = (float)adjustedX;
        player.PlayerPawn.Value.AbsVelocity.Y = (float)adjustedY;
    }
    

    public static string ColorNamesToTags(string message)
    {
        string modifiedValue = message;
        foreach (FieldInfo field in typeof(ChatColors).GetFields())
        {
            string pattern = $"{{{field.Name}}}";
            if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                modifiedValue = modifiedValue.Replace(pattern, field.GetValue(null)!.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }
        return modifiedValue;
    }
    
    // reverse of ReplaceTags
    public static string TagsToColorNames(string message)
    {
        string modifiedValue = message;
        foreach (FieldInfo field in typeof(ChatColors).GetFields())
        {
            string pattern = field.GetValue(null)!.ToString();
            if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                modifiedValue = modifiedValue.Replace(pattern, $"{{{field.Name}}}", StringComparison.OrdinalIgnoreCase);
            }
        }
        return modifiedValue;
    }
    
    public static string RemoveColorNames(string message)
    {
        string modifiedValue = message;
        foreach (FieldInfo field in typeof(ChatColors).GetFields())
        {
            string pattern = $"{{{field.Name}}}";
            if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                modifiedValue = modifiedValue.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
            }
        }
        return modifiedValue;
    }

    public static bool IsStringAColorName(string message)
    {
        foreach (FieldInfo field in typeof(ChatColors).GetFields())
        {
            string pattern = $"{{{field.Name}}}";
            // THE STRING MUST BE EXACTLY THE SAME AS THE FIELD NAME
            // {GOLD} == TRUE
            // {Gold} == TRUE
            // {gold}cat == FALSE
            Console.WriteLine(pattern);
            Console.WriteLine(message);
            if (message.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}