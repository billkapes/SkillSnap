using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Models;

namespace SkillSnap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
	private readonly SkillSnapContext _context;
	private readonly IMemoryCache _cache;

	public ProfilesController(SkillSnapContext context, IMemoryCache cache)
	{
		_context = context;
		_cache = cache;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<PortfolioUser>>> GetProfiles()
	{
		return Ok(await _context.PortfolioUsers
			.AsNoTracking()
			.OrderBy(profile => profile.Id)
			.ToListAsync());
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<PortfolioUser>> GetProfile(int id)
	{
		var profile = await _context.PortfolioUsers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
		return profile is null ? NotFound() : Ok(profile);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<PortfolioUser>> AddProfile(PortfolioUser profile)
	{
		profile.Id = 0;
		profile.Projects.Clear();
		profile.Skills.Clear();
		_context.PortfolioUsers.Add(profile);
		await _context.SaveChangesAsync();
		return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, profile);
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<PortfolioUser>> UpdateProfile(int id, PortfolioUser profile)
	{
		var existingProfile = await _context.PortfolioUsers.FindAsync(id);
		if (existingProfile is null)
		{
			return NotFound();
		}

		existingProfile.Name = profile.Name;
		existingProfile.Bio = profile.Bio;
		existingProfile.ProfileImageUrl = profile.ProfileImageUrl;

		await _context.SaveChangesAsync();
		return Ok(existingProfile);
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteProfile(int id)
	{
		var profile = await _context.PortfolioUsers.FindAsync(id);
		if (profile is null)
		{
			return NotFound();
		}

		_context.PortfolioUsers.Remove(profile);
		await _context.SaveChangesAsync();
		_cache.Remove("projects");
		_cache.Remove("skills");
		return NoContent();
	}
}
