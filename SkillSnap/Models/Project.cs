namespace SkillSnap.Models;

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    public int PortfolioUserId { get; set; }
    public PortfolioUser? PortfolioUser { get; set; }
}
