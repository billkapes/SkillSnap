using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Models;

namespace SkillSnap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
	private readonly SkillSnapContext _context;

	public SkillsController(SkillSnapContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
	{
		return await _context.Skills.ToListAsync();
	}

	[HttpPost]
	public async Task<ActionResult<Skill>> AddSkill(Skill skill)
	{
		_context.Skills.Add(skill);
		await _context.SaveChangesAsync();

		return CreatedAtAction(nameof(GetSkills), skill);
	}
}
