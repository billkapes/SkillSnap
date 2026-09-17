using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Models;

namespace SkillSnap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
	private readonly SkillSnapContext _context;

	public ProjectsController(SkillSnapContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
	{
		return await _context.Projects.ToListAsync();
	}

	[HttpPost]
	public async Task<ActionResult<Project>> AddProject(Project project)
	{
		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		return CreatedAtAction(nameof(GetProjects), project);
	}
}
