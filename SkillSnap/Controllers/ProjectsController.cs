using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SkillSnap.Models;

namespace SkillSnap.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
	private readonly SkillSnapContext _context;
	private readonly IMemoryCache _cache;

	public ProjectsController(SkillSnapContext context, IMemoryCache cache)
	{
		_context = context;
		_cache = cache;
	}

	[HttpGet]
	public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
	{
		if (!_cache.TryGetValue("projects", out List<Project> projects))
		{
			projects = await _context.Projects
				.AsNoTracking()
				.ToListAsync();
			_cache.Set("projects", projects, TimeSpan.FromMinutes(5));
		}
		return Ok(projects);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Project>> AddProject(Project project)
	{
		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		_cache.Remove("projects");
		return CreatedAtAction(nameof(GetProjects), project);
	}
}
