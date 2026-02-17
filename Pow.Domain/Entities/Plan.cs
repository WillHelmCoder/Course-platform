using System.ComponentModel.DataAnnotations;

namespace Pow.Domain.Entities;

public class Plan : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Billing interval: monthly, yearly, lifetime, free
    /// </summary>
    public string Interval { get; set; } = "monthly";

    /// <summary>
    /// Features included in this plan (JSON or comma-separated)
    /// </summary>
    public string? Features { get; set; }

    /// <summary>
    /// Maximum users allowed (0 = unlimited)
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// Display order in plan selection
    /// </summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Highlight this plan as recommended
    /// </summary>
    public bool IsRecommended { get; set; }

    // Stripe integration
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }

    // Navigation
    public ICollection<UserPlan> UserPlans { get; set; } = new List<UserPlan>();
}
