using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// ChannelSubscription - User's subscription to a channel plan.
/// Links User to a specific ChannelPlan.
/// </summary>
public class ChannelSubscription : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid ChannelPlanId { get; set; }

    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// External subscription ID (Stripe, PayPal, etc.)
    /// </summary>
    public string? ExternalSubscriptionId { get; set; }

    /// <summary>
    /// Stripe Customer ID for this subscription
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Stripe Payment Intent ID for this transaction
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Price paid (for record keeping)
    /// </summary>
    public decimal? PricePaid { get; set; }

    /// <summary>
    /// Currency of the payment
    /// </summary>
    public string? Currency { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
    public ChannelPlan ChannelPlan { get; set; } = null!;
}
