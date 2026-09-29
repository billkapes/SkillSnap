using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.JSInterop;

namespace SkillSnap.Client.Services;

public sealed class AuthService
{
    public const string AccessTokenStorageKey = "skillsnap.access_token";

    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private bool _initialized;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public event Action? AuthStateChanged;

    public ClaimsPrincipal CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser.Identity?.IsAuthenticated == true;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializeLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenStorageKey);
            if (!string.IsNullOrWhiteSpace(token) && TryCreatePrincipal(token, out var principal))
            {
                _currentUser = principal;
            }
            else if (!string.IsNullOrWhiteSpace(token))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenStorageKey);
            }

            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        await InitializeAsync();
        using var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
        {
            email,
            password
        });

        if (!response.IsSuccessStatusCode)
        {
            return AuthResult.Failure(await ReadErrorAsync(response));
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result is null || !TryCreatePrincipal(result.AccessToken, out var principal))
        {
            return AuthResult.Failure("The server returned an invalid access token.");
        }

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenStorageKey, result.AccessToken);
        _currentUser = principal;
        AuthStateChanged?.Invoke();
        return AuthResult.Success();
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
        {
            email,
            password
        });

        return response.IsSuccessStatusCode
            ? AuthResult.Success()
            : AuthResult.Failure(await ReadErrorAsync(response));
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenStorageKey);
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        AuthStateChanged?.Invoke();
    }

    private static bool TryCreatePrincipal(string token, out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = document.RootElement;

            if (root.TryGetProperty("exp", out var expiration) &&
                expiration.TryGetInt64(out var expiresAt) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiresAt)
            {
                return false;
            }

            var claims = new List<Claim>();
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name is "sub" or ClaimTypes.NameIdentifier && property.Value.ValueKind == JsonValueKind.String)
                {
                    var userId = property.Value.GetString()!;
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                    claims.Add(new Claim(ClaimTypes.Name, userId));
                }
                else if (property.Name is "email" or ClaimTypes.Email && property.Value.ValueKind == JsonValueKind.String)
                {
                    claims.Add(new Claim(ClaimTypes.Email, property.Value.GetString()!));
                }
                else if (property.Name is "role" or ClaimTypes.Role)
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        claims.AddRange(property.Value.EnumerateArray()
                            .Where(role => role.ValueKind == JsonValueKind.String)
                            .Select(role => new Claim(ClaimTypes.Role, role.GetString()!)));
                    }
                    else if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, property.Value.GetString()!));
                    }
                }
            }

            principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role));
            return true;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString()!;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                return string.Join(" ", errors.EnumerateArray()
                    .Where(error => error.ValueKind == JsonValueKind.String)
                    .Select(error => error.GetString()));
            }
        }
        catch (JsonException)
        {
        }

        return "The request could not be completed. Please try again.";
    }

    private sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
}

public sealed record AuthResult(bool Succeeded, string? ErrorMessage = null)
{
    public static AuthResult Success() => new(true);

    public static AuthResult Failure(string message) => new(false, message);
}

internal sealed class BrowserTokenHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserTokenHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            cancellationToken,
            AuthService.AccessTokenStorageKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}