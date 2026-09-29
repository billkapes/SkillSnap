using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using SkillSnap.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp =>
{
	var handler = new BrowserTokenHandler(sp.GetRequiredService<IJSRuntime>())
	{
		InnerHandler = new HttpClientHandler()
	};

	return new HttpClient(handler)
	{
		BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
	};
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SkillService>();

await builder.Build().RunAsync();
