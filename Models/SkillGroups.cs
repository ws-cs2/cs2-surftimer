using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class SkillGroup
{
    public string Name { get; }
    public int? MinRank { get; }
    public int? MaxRank { get; }
    public int? Points { get; }
    
    public char ChatColor { get; }

    public SkillGroup(string name, int? minRank, int? maxRank, int? points, char chatColor)
    {
        Name = name;
        MinRank = minRank;
        MaxRank = maxRank;
        Points = points;
        ChatColor = chatColor;
    }
}

public static class SkillGroups
{
    public static readonly List<SkillGroup> Groups = new List<SkillGroup>
    {
        new SkillGroup("King", 1, 1, null, ChatColors.Darkred),
        new SkillGroup("Godly", 2, 2, null, ChatColors.Green),
        new SkillGroup("Legendary", 3, 3, null, ChatColors.Magenta),
        new SkillGroup("Mythical", 4, 4, null, ChatColors.Yellow),
        new SkillGroup("Phantom", 5, 5, null, ChatColors.BlueGrey),
        new SkillGroup("Master", 6, 10, null, ChatColors.LightRed),
        new SkillGroup("Elite", 11, 25, null, ChatColors.Red),
        new SkillGroup("Veteran", 26, 50, null, ChatColors.DarkBlue),
        new SkillGroup("Pro", 51, 100, null, ChatColors.Blue),
        new SkillGroup("Expert", 101, 200, null, ChatColors.Purple),
        new SkillGroup("Ace", 201, 400, null, ChatColors.Orange),
        new SkillGroup("Exceptional", null, null, 1750, ChatColors.LightPurple),
        new SkillGroup("Skilled", null, null, 1500, ChatColors.LightBlue),
        new SkillGroup("Advanced", null, null, 1250, ChatColors.LightYellow),
        new SkillGroup("Casual", null, null, 1000, ChatColors.Grey),
        new SkillGroup("Average", null, null, 750, ChatColors.Olive),
        new SkillGroup("Rookie", null, null, 500, ChatColors.Lime),
        new SkillGroup("Beginner", null, null, 250, ChatColors.Silver),
        
        
        new SkillGroup("Unranked", null, null, 0, ChatColors.White)
    };
    public static SkillGroup GetSkillGroup(int rank, int points)
    {
        if (points == 0 || rank == 0)
        {
            return Groups[^1];
        }

        foreach (var group in Groups)
        {
            if (group.MinRank.HasValue && group.MaxRank.HasValue)
            {
                if (rank >= group.MinRank && rank <= group.MaxRank)
                {
                    return group;
                }
            }
            else if (group.Points.HasValue && points >= group.Points)
            {
                return group;
            }
        }

        return Groups[^1];
    }
    
}