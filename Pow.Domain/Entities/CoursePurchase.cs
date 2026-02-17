namespace Pow.Domain.Entities;

/// <summary>
/// Tracks one-time course purchases by users.
/// </summary>
public class CoursePurchase : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public decimal PricePaid { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Stripe Payment Intent ID
    /// </summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Stripe Checkout Session ID
    /// </summary>
    public string? StripeSessionId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
