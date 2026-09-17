using System.Net.Http.Json;
using SkillSnap.Client.Models;

namespace SkillSnap.Client.Services;

public class SkillService
{
	private readonly HttpClient _httpClient;

	public SkillService(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<List<Skill>> GetSkillsAsync()
	{
		return await _httpClient.GetFromJsonAsync<List<Skill>>("api/skills") ?? [];
	}

	public async Task<Skill?> AddSkillAsync(Skill newSkill)
	{
		using var response = await _httpClient.PostAsJsonAsync("api/skills", newSkill);
		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<Skill>();
	}
}
