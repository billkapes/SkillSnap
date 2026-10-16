using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SkillSnap.Client.Models;
using Xunit;

namespace SkillSnap.Tests;

public class CrudApiTests
{
	[Fact]
	public async Task Admin_can_create_read_update_and_delete_profiles_projects_and_skills()
	{
		using var factory = new CrudApiFactory();
		await factory.InitializeDatabaseAsync();
		using var client = factory.CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

		var profileResponse = await client.PostAsJsonAsync("api/profiles", new PortfolioUser
		{
			Name = "Initial profile",
			Bio = "Initial bio",
			ProfileImageUrl = "https://example.com/initial.png"
		});
		Assert.Equal(HttpStatusCode.Created, profileResponse.StatusCode);
		var profile = await profileResponse.Content.ReadFromJsonAsync<PortfolioUser>();
		Assert.NotNull(profile);

		var profiles = await client.GetFromJsonAsync<List<PortfolioUser>>("api/profiles");
		Assert.Contains(profiles!, item => item.Id == profile.Id);
		var profileReadResponse = await client.GetAsync($"api/profiles/{profile.Id}");
		Assert.Equal(HttpStatusCode.OK, profileReadResponse.StatusCode);

		var profileUpdateResponse = await client.PutAsJsonAsync($"api/profiles/{profile.Id}", new PortfolioUser
		{
			Name = "Updated profile",
			Bio = "Updated bio",
			ProfileImageUrl = "https://example.com/updated.png"
		});
		Assert.Equal(HttpStatusCode.OK, profileUpdateResponse.StatusCode);
		Assert.Equal("Updated profile", (await profileUpdateResponse.Content.ReadFromJsonAsync<PortfolioUser>())?.Name);

		var project = await CreateProjectAsync(client, profile.Id, "Initial project");
		var projectReadResponse = await client.GetAsync($"api/projects/{project.Id}");
		Assert.Equal(HttpStatusCode.OK, projectReadResponse.StatusCode);
		var projectsBeforeUpdate = await client.GetFromJsonAsync<List<Project>>("api/projects");
		Assert.Contains(projectsBeforeUpdate!, item => item.Id == project.Id);
		var projectUpdateResponse = await client.PutAsJsonAsync($"api/projects/{project.Id}", new Project
		{
			Title = "Updated project",
			Description = "Updated description",
			ImageUrl = "https://example.com/project.png",
			PortfolioUserId = profile.Id
		});
		Assert.Equal(HttpStatusCode.OK, projectUpdateResponse.StatusCode);
		Assert.Equal("Updated project", (await projectUpdateResponse.Content.ReadFromJsonAsync<Project>())?.Title);
		var projectsAfterUpdate = await client.GetFromJsonAsync<List<Project>>("api/projects");
		Assert.Contains(projectsAfterUpdate!, item => item.Id == project.Id && item.Title == "Updated project");

		var skill = await CreateSkillAsync(client, profile.Id, "Initial skill");
		var skillReadResponse = await client.GetAsync($"api/skills/{skill.Id}");
		Assert.Equal(HttpStatusCode.OK, skillReadResponse.StatusCode);
		var skillsBeforeUpdate = await client.GetFromJsonAsync<List<Skill>>("api/skills");
		Assert.Contains(skillsBeforeUpdate!, item => item.Id == skill.Id);
		var skillUpdateResponse = await client.PutAsJsonAsync($"api/skills/{skill.Id}", new Skill
		{
			Name = "Updated skill",
			Level = "Expert",
			PortfolioUserId = profile.Id
		});
		Assert.Equal(HttpStatusCode.OK, skillUpdateResponse.StatusCode);
		Assert.Equal("Updated skill", (await skillUpdateResponse.Content.ReadFromJsonAsync<Skill>())?.Name);
		var skillsAfterUpdate = await client.GetFromJsonAsync<List<Skill>>("api/skills");
		Assert.Contains(skillsAfterUpdate!, item => item.Id == skill.Id && item.Name == "Updated skill");

		Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/projects/{project.Id}")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/projects/{project.Id}")).StatusCode);
		Assert.DoesNotContain(await client.GetFromJsonAsync<List<Project>>("api/projects") ?? [],
			item => item.Id == project.Id);
		Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/skills/{skill.Id}")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/skills/{skill.Id}")).StatusCode);
		Assert.DoesNotContain(await client.GetFromJsonAsync<List<Skill>>("api/skills") ?? [],
			item => item.Id == skill.Id);

		var cascadingProject = await CreateProjectAsync(client, profile.Id, "Cascade project");
		var cascadingSkill = await CreateSkillAsync(client, profile.Id, "Cascade skill");
		await client.GetFromJsonAsync<List<Project>>("api/projects");
		await client.GetFromJsonAsync<List<Skill>>("api/skills");

		Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/profiles/{profile.Id}")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/profiles/{profile.Id}")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/projects/{cascadingProject.Id}")).StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/skills/{cascadingSkill.Id}")).StatusCode);
		Assert.DoesNotContain(await client.GetFromJsonAsync<List<Project>>("api/projects") ?? [],
			item => item.Id == cascadingProject.Id);
		Assert.DoesNotContain(await client.GetFromJsonAsync<List<Skill>>("api/skills") ?? [],
			item => item.Id == cascadingSkill.Id);
	}

	private static async Task<Project> CreateProjectAsync(HttpClient client, int profileId, string title)
	{
		var response = await client.PostAsJsonAsync("api/projects", new Project
		{
			Title = title,
			Description = "Project description",
			PortfolioUserId = profileId
		});
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		return (await response.Content.ReadFromJsonAsync<Project>())!;
	}

	private static async Task<Skill> CreateSkillAsync(HttpClient client, int profileId, string name)
	{
		var response = await client.PostAsJsonAsync("api/skills", new Skill
		{
			Name = name,
			Level = "Beginner",
			PortfolioUserId = profileId
		});
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		return (await response.Content.ReadFromJsonAsync<Skill>())!;
	}
}

internal sealed class CrudApiFactory : WebApplicationFactory<Program>
{
	private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"skillsnap-{Guid.NewGuid():N}.db");
	private readonly string? _originalConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
	private readonly string? _originalJwtKey = Environment.GetEnvironmentVariable("Jwt__Key");

	public CrudApiFactory()
	{
		Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source={_databasePath};Pooling=False");
		Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 32));
	}

	public async Task InitializeDatabaseAsync()
	{
		await using var scope = Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<SkillSnapContext>();
		await context.Database.EnsureCreatedAsync();
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");
		builder.ConfigureTestServices(services =>
		{
			services.AddAuthentication(options =>
				{
					options.DefaultAuthenticateScheme = "Test";
					options.DefaultChallengeScheme = "Test";
				})
				.AddScheme<AuthenticationSchemeOptions, CrudTestAuthHandler>("Test", _ => { });
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _originalConnectionString);
			Environment.SetEnvironmentVariable("Jwt__Key", _originalJwtKey);
			if (File.Exists(_databasePath))
			{
				File.Delete(_databasePath);
			}
		}
	}
}

internal sealed class CrudTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	public CrudTestAuthHandler(
		Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
		Microsoft.Extensions.Logging.ILoggerFactory logger,
		System.Text.Encodings.Web.UrlEncoder encoder)
		: base(options, logger, encoder)
	{
	}

	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue("X-Test-Role", out var role) || role != "Admin")
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}

		var claims = new[]
		{
			new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-admin"),
			new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin")
		};
		var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
		var principal = new System.Security.Claims.ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, Scheme.Name);
		return Task.FromResult(AuthenticateResult.Success(ticket));
	}
}
