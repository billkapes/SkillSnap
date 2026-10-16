using System.ComponentModel.DataAnnotations;

namespace SkillSnap.Client.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int PortfolioUserId { get; set; }
}