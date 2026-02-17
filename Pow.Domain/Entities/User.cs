namespace Pow.Domain.Entities;

/// <summary>
/// User entity with authentication properties.
/// Marked as partial to allow OAuth integrations (GitHub, Google, etc.) to add fields.
/// </summary>
public partial class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;

    // Profile information
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    public string? PhoneNumber { get; set; }

    // Social networks
    public string? Twitter { get; set; }
    public string? LinkedIn { get; set; }
    public string? GitHub { get; set; }
    public string? Instagram { get; set; }
    public string? YouTube { get; set; }
    public string? TikTok { get; set; }
    public string? Website { get; set; }

    // Refresh token for JWT
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Stripe integration
    public string? StripeCustomerId { get; set; }

    // Navigation - CMS
    public ICollection<ChannelAdmin> ChannelAdmins { get; set; } = new List<ChannelAdmin>();
    public ICollection<ChannelSubscription> ChannelSubscriptions { get; set; } = new List<ChannelSubscription>();
    public ICollection<ChannelFollow> ChannelFollows { get; set; } = new List<ChannelFollow>();
}
