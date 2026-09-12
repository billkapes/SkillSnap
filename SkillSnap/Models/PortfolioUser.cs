namespace SkillSnap.Models;

public class PortfolioUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;

    public List<Project> Projects { get; set; } = new();
    public List<Skill> Skills { get; set; } = new();
}
