namespace Pow.Domain.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password, string? PhoneNumber = null);

/// <summary>
/// Creator registration - creates Admin user with channel and platform plan subscription.
/// </summary>
public record RegisterCreatorRequest(
    string Email,
    string Password,
    string ChannelName,
    Guid? PlatformPlanId = null
);

/// <summary>
/// Subscriber registration - creates User with optional channel subscription.
/// </summary>
public record RegisterSubscriberRequest(
    string Email,
    string Password,
    Guid ChannelId,
    Guid? ChannelPlanId,
    string? PhoneNumber = null
);

public record AuthResponse(
    bool Success,
    string? Token = null,
    string? RefreshToken = null,
    DateTime? Expiration = null,
    string? Message = null,
    UserInfo? User = null
);

public record UserInfo(
    Guid Id,
    string Email,
    string Role,
    Guid TenantId,
    string TenantName,
    string? DisplayName = null,
    string? Bio = null,
    string? ProfilePicture = null,
    string? Twitter = null,
    string? LinkedIn = null,
    string? GitHub = null,
    string? Instagram = null,
    string? YouTube = null,
    string? TikTok = null,
    string? Website = null
);

public record UpdateProfileRequest(
    string? DisplayName,
    string? Bio,
    string? ProfilePicture,
    string? Twitter,
    string? LinkedIn,
    string? GitHub,
    string? Instagram,
    string? YouTube,
    string? TikTok,
    string? Website
);

public record RefreshTokenRequest(string Token, string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

// ===== SITE SETTINGS =====
public record SiteSettingDto(string Key, string Value, string? Description);

// ===== VIDEO SETTINGS =====
public record VideoSettingsDto(string Provider, string? MuxTokenId, bool HasMuxTokenSecret, bool HasVimeoToken);
