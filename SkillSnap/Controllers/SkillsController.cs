using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Models;

namespace SkillSnap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
	private readonly SkillSnapContext _context;
	private readonly IMemoryCache _cache;

	public SkillsController(SkillSnapContext context, IMemoryCache cache)
	{
		_context = context;
		_cache = cache;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
	{
		if (!_cache.TryGetValue("skills", out List<Skill>? skills))
		{
			skills = await _context.Skills
				.AsNoTracking()
				.ToListAsync();
			_cache.Set("skills", skills, TimeSpan.FromMinutes(5));
		}
		return Ok(skills);
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<Skill>> GetSkill(int id)
	{
		var skill = await _context.Skills.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
		return skill is null ? NotFound() : Ok(skill);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Skill>> AddSkill(Skill skill)
	{
		if (!await _context.PortfolioUsers.AnyAsync(user => user.Id == skill.PortfolioUserId))
		{
			return BadRequest($"Portfolio user {skill.PortfolioUserId} does not exist.");
		}

		skill.Id = 0;
		skill.PortfolioUser = null;
		_context.Skills.Add(skill);
		await _context.SaveChangesAsync();

		_cache.Remove("skills");
		return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Skill>> UpdateSkill(int id, Skill skill)
	{
		var existingSkill = await _context.Skills.FindAsync(id);
		if (existingSkill is null)
		{
			return NotFound();
		}

		if (!await _context.PortfolioUsers.AnyAsync(user => user.Id == skill.PortfolioUserId))
		{
			return BadRequest($"Portfolio user {skill.PortfolioUserId} does not exist.");
		}

		existingSkill.Name = skill.Name;
		existingSkill.Level = skill.Level;
		existingSkill.PortfolioUserId = skill.PortfolioUserId;

		await _context.SaveChangesAsync();
		_cache.Remove("skills");
		return Ok(existingSkill);
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteSkill(int id)
	{
		var skill = await _context.Skills.FindAsync(id);
		if (skill is null)
		{
			return NotFound();
		}

		_context.Skills.Remove(skill);
		await _context.SaveChangesAsync();
		_cache.Remove("skills");
		return NoContent();
	}
}
