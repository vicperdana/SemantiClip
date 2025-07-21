using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace SemanticClip.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string STORAGE_KEY = "authUser";
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    
    // Hardcoded credentials
    private const string VALID_USERNAME = "admin";
    private const string VALID_PASSWORD = "SemanticClip2025!";
    
    public CustomAuthenticationStateProvider(ILogger<CustomAuthenticationStateProvider> logger)
    {
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userJson = await GetStoredUserAsync();
            if (string.IsNullOrEmpty(userJson))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var user = JsonSerializer.Deserialize<UserInfo>(userJson);
            if (user == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim("DisplayName", user.DisplayName),
                new Claim("LoginTime", user.LoginTime.ToString())
            };

            var identity = new ClaimsIdentity(claims, "custom");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            return new AuthenticationState(claimsPrincipal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            // Validate credentials
            if (username != VALID_USERNAME || password != VALID_PASSWORD)
            {
                _logger.LogWarning("Invalid login attempt for username: {Username}", username);
                return false;
            }

            var user = new UserInfo
            {
                Username = username,
                DisplayName = "Administrator",
                LoginTime = DateTime.UtcNow
            };

            var userJson = JsonSerializer.Serialize(user);
            await StoreUserAsync(userJson);

            // Notify authentication state changed
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            _logger.LogInformation("User {Username} logged in successfully", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await RemoveUserAsync();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            _logger.LogInformation("User logged out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    private Task<string?> GetStoredUserAsync()
    {
        // In a real app, you'd use localStorage or sessionStorage
        // For simplicity, we'll use a static field (will reset on page refresh)
        return Task.FromResult(_storedUser);
    }

    private Task StoreUserAsync(string userJson)
    {
        _storedUser = userJson;
        return Task.CompletedTask;
    }

    private Task RemoveUserAsync()
    {
        _storedUser = null;
        return Task.CompletedTask;
    }

    private static string? _storedUser;

    private class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
    }
}
