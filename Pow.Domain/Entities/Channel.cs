using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// Channel - Container for courses. Assigned to Admins by SuperAdmin.
/// Each channel has its own subscription plans and membership.
/// </summary>
public class Channel : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? MainPicture { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Who can send messages to the channel inbox.
    /// </summary>
    public InboxVisibility InboxVisibility { get; set; } = InboxVisibility.Disabled;

    // Navigation
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<ChannelAdmin> ChannelAdmins { get; set; } = new List<ChannelAdmin>();
    public ICollection<ChannelPlan> Plans { get; set; } = new List<ChannelPlan>();
    public ICollection<ChannelSubscription> Subscriptions { get; set; } = new List<ChannelSubscription>();
    public ICollection<ChannelMessage> Messages { get; set; } = new List<ChannelMessage>();
    public ICollection<ChannelEvent> Events { get; set; } = new List<ChannelEvent>();
    public ICollection<ChannelFollow> Followers { get; set; } = new List<ChannelFollow>();
}
