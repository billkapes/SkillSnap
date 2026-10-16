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
		if (!_cache.TryGetValue("projects", out List<Project>? projects))
		{
			projects = await _context.Projects
				.AsNoTracking()
				.ToListAsync();
			_cache.Set("projects", projects, TimeSpan.FromMinutes(5));
		}
		return Ok(projects);
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<Project>> GetProject(int id)
	{
		var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
		return project is null ? NotFound() : Ok(project);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Project>> AddProject(Project project)
	{
		if (!await _context.PortfolioUsers.AnyAsync(user => user.Id == project.PortfolioUserId))
		{
			return BadRequest($"Portfolio user {project.PortfolioUserId} does not exist.");
		}

		project.Id = 0;
		project.PortfolioUser = null;
		_context.Projects.Add(project);
		await _context.SaveChangesAsync();

		_cache.Remove("projects");
		return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<Project>> UpdateProject(int id, Project project)
	{
		var existingProject = await _context.Projects.FindAsync(id);
		if (existingProject is null)
		{
			return NotFound();
		}

		if (!await _context.PortfolioUsers.AnyAsync(user => user.Id == project.PortfolioUserId))
		{
			return BadRequest($"Portfolio user {project.PortfolioUserId} does not exist.");
		}

		existingProject.Title = project.Title;
		existingProject.Description = project.Description;
		existingProject.ImageUrl = project.ImageUrl;
		existingProject.PortfolioUserId = project.PortfolioUserId;

		await _context.SaveChangesAsync();
		_cache.Remove("projects");
		return Ok(existingProject);
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteProject(int id)
	{
		var project = await _context.Projects.FindAsync(id);
		if (project is null)
		{
			return NotFound();
		}

		_context.Projects.Remove(project);
		await _context.SaveChangesAsync();
		_cache.Remove("projects");
		return NoContent();
	}
}
