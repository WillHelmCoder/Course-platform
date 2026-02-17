namespace Pow.Domain.Entities;

/// <summary>
/// ChannelAdmin - Many-to-many relationship between Channels and Users (Admins).
/// SuperAdmin assigns channels to Admins.
/// </summary>
public class ChannelAdmin : TenantEntity
{
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }

    // Navigation
    public Channel Channel { get; set; } = null!;
    public User User { get; set; } = null!;
}
