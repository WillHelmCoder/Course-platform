namespace Pow.Domain.Entities;

/// <summary>
/// ChannelPlan - Subscription tiers for a channel.
/// Each channel can have multiple plans (Free, Basic, Pro, VIP, etc.)
/// </summary>
public class ChannelPlan : TenantEntity
{
    public Guid ChannelId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Price for this plan (0 = free tier)
    /// </summary>
    public decimal Price { get; set; } = 0;

    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Billing interval: monthly, yearly, lifetime, free
    /// </summary>
    public string Interval { get; set; } = "monthly";

    /// <summary>
    /// Sort order - also determines hierarchy (higher = more access).
    /// Example: Free=0, Basic=10, Pro=20, VIP=30
    /// A user with Pro (20) can access content requiring Basic (10).
    /// </summary>
    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Stripe integration
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }

    // Navigation
    public Channel Channel { get; set; } = null!;
    public ICollection<ChannelSubscription> Subscriptions { get; set; } = new List<ChannelSubscription>();
}
