using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Pow.Domain.DTOs;

namespace Pow.Web.Services;

/// <summary>
/// API service for backend communication.
/// Marked as partial to allow integrations (GitHub OAuth, etc.) to add methods.
/// </summary>
public partial class ApiService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public ApiService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        try
        {
            var response = await _http.GetAsync($"api/auth/check-email?email={Uri.EscapeDataString(email)}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<bool>();
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result ?? new AuthResponse(false, Message: "Unknown error");
    }

    public async Task<AuthResponse> RegisterAsync(string email, string password, string? phoneNumber = null)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, password, phoneNumber));
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result ?? new AuthResponse(false, Message: "Unknown error");
    }

    public async Task<AuthResponse> RefreshTokenAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
            return new AuthResponse(false, Message: "No tokens available");
        }

        var response = await _http.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest(token, refreshToken));
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result ?? new AuthResponse(false, Message: "Unknown error");
    }

    public async Task LogoutAsync()
    {
        await SetAuthHeaderAsync();
        await _http.PostAsync("api/auth/logout", null);
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _http.GetFromJsonAsync<UserInfo>("api/auth/me");
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserInfo?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PutAsJsonAsync("api/auth/profile", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<UserInfo>();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserInfo?> GetPublicProfileAsync(Guid userId)
    {
        try
        {
            return await _http.GetFromJsonAsync<UserInfo>($"api/auth/profile/{userId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAccountAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.DeleteAsync("api/auth/delete-account");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/change-password", new ChangePasswordDto(currentPassword, newPassword));
            if (response.IsSuccessStatusCode)
                return (true, "Password changed successfully");

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Message ?? "Failed to change password");
        }
        catch
        {
            return (false, "An error occurred");
        }
    }

    private record ErrorResponse(string? Message);

    // ===== SITE SETTINGS =====

    public async Task<SiteSettingDto?> GetSettingAsync(string key)
    {
        try
        {
            return await _http.GetFromJsonAsync<SiteSettingDto>($"api/settings/{key}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<SiteSettingDto>> GetAllSettingsAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _http.GetFromJsonAsync<List<SiteSettingDto>>("api/settings") ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<SiteSettingDto?> UpdateSettingAsync(string key, string value, string? description = null)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PutAsJsonAsync($"api/settings/{key}", new { Value = value, Description = description });
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SiteSettingDto>();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<VideoSettingsDto?> GetVideoSettingsAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _http.GetFromJsonAsync<VideoSettingsDto>("api/settings/video");
        }
        catch
        {
            return null;
        }
    }

    public async Task<VideoSettingsDto?> UpdateVideoSettingsAsync(string provider, string? muxTokenId, string? muxTokenSecret, string? vimeoAccessToken)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _http.PutAsJsonAsync("api/settings/video", new { Provider = provider, MuxTokenId = muxTokenId, MuxTokenSecret = muxTokenSecret, VimeoAccessToken = vimeoAccessToken });
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<VideoSettingsDto>();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ===== ADMIN =====

    public async Task<List<UserInfo>> GetUsersAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<UserInfo>>("api/admin/users") ?? new();
    }

    // XipeLib:Methods
    // ===== MEMBERSHIP - PUBLIC =====

    public async Task<List<PlanDto>> GetPlansAsync()
        => await _http.GetFromJsonAsync<List<PlanDto>>("api/membership/plans") ?? new();

    public async Task<bool> HasPlanAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _http.GetFromJsonAsync<bool>("api/membership/has-plan");
        }
        catch
        {
            return false;
        }
    }

    // ===== MEMBERSHIP - AUTHENTICATED =====

    public async Task<bool> SelectPlanAsync(Guid planId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/membership/select-plan", new { planId });
        return response.IsSuccessStatusCode;
    }

    public async Task<PlanDto?> GetMyPlanAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<PlanDto?>("api/membership/my-plan");
    }

    public async Task<List<string>> GetMyPermissionsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<string>>("api/membership/my-permissions") ?? new();
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<bool>($"api/membership/has-permission/{permission}");
    }

    // ===== ADMIN - STATS =====

    public async Task<MembershipStatsDto?> GetAdminStatsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<MembershipStatsDto>("api/admin/stats");
    }

    // ===== ADMIN - PLANS =====

    public async Task<List<PlanDto>> GetAdminPlansAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<PlanDto>>("api/admin/plans") ?? new();
    }

    public async Task<PlanDto> CreatePlanAsync(CreatePlanDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/admin/plans", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlanDto>() ?? throw new Exception("Failed to create plan");
    }

    public async Task<PlanDto?> UpdatePlanAsync(Guid id, UpdatePlanDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/admin/plans/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PlanDto>();
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/admin/plans/{id}");
        return response.IsSuccessStatusCode;
    }

    // ===== ADMIN - ROLES =====

    public async Task<List<RoleDto>> GetAdminRolesAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<RoleDto>>("api/admin/roles") ?? new();
    }

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<PermissionDto>>("api/admin/permissions") ?? new();
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/admin/roles", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RoleDto>() ?? throw new Exception("Failed to create role");
    }

    public async Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/admin/roles/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RoleDto>();
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/admin/roles/{id}");
        return response.IsSuccessStatusCode;
    }

    // ===== ADMIN - USERS =====

    public async Task<List<UserMembershipDto>> GetAdminUsersAsync()
    {
        await SetAuthHeaderAsync();
        return await _http.GetFromJsonAsync<List<UserMembershipDto>>("api/admin/users") ?? new();
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/admin/users/{userId}/roles/{roleId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/admin/users/{userId}/roles/{roleId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignPlanToUserAsync(Guid userId, Guid planId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync($"api/admin/users/{userId}/plan", new { planId });
        return response.IsSuccessStatusCode;
    }

}
