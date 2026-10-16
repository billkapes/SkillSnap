using System.Net.Http.Json;
using SkillSnap.Client.Models;

namespace SkillSnap.Client.Services;

public class PortfolioUserService
{
	private readonly HttpClient _httpClient;

	public PortfolioUserService(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<List<PortfolioUser>> GetProfilesAsync()
	{
		return await _httpClient.GetFromJsonAsync<List<PortfolioUser>>("api/profiles") ?? [];
	}

	public async Task<PortfolioUser?> GetProfileAsync(int id)
	{
		return await _httpClient.GetFromJsonAsync<PortfolioUser>($"api/profiles/{id}");
	}

	public async Task<PortfolioUser?> AddProfileAsync(PortfolioUser profile)
	{
		using var response = await _httpClient.PostAsJsonAsync("api/profiles", profile);
		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<PortfolioUser>();
	}

	public async Task<PortfolioUser?> UpdateProfileAsync(PortfolioUser profile)
	{
		using var response = await _httpClient.PutAsJsonAsync($"api/profiles/{profile.Id}", profile);
		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<PortfolioUser>();
	}

	public async Task DeleteProfileAsync(int id)
	{
		using var response = await _httpClient.DeleteAsync($"api/profiles/{id}");
		response.EnsureSuccessStatusCode();
	}
}
