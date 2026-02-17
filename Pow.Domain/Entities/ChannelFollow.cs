namespace Pow.Domain.Entities;

/// <summary>
/// Represents a user following a channel.
/// Following is free and allows seeing new courses and free content.
/// Different from subscription which provides paid access.
/// </summary>
public class ChannelFollow : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid ChannelId { get; set; }
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}
