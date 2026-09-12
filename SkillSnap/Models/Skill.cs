namespace SkillSnap.Models;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;

    public int PortfolioUserId { get; set; }
    public PortfolioUser? PortfolioUser { get; set; }
}
