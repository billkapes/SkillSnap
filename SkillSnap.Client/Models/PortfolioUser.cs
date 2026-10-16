using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Client.Models;

public class PortfolioUser
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Bio { get; set; } = string.Empty;

    [StringLength(500)]
    public string ProfileImageUrl { get; set; } = string.Empty;
}
