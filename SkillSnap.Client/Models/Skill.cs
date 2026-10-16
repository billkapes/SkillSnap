using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Client.Models;

public class Skill
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Level { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int PortfolioUserId { get; set; }
}