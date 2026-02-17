using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// Course - Container for chapters within a channel.
/// </summary>
public class Course : TenantEntity
{
    public Guid ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MainPicture { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsPublished { get; set; } = false;
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Access type: Free (everyone), SubscribersOnly (any subscription), or SpecificPlan.
    /// Defines who gets FREE access to this course.
    /// </summary>
    public CourseAccessType AccessType { get; set; } = CourseAccessType.Free;

    /// <summary>
    /// Required plan ID when AccessType is SpecificPlan. Users with this plan or higher can access.
    /// </summary>
    public Guid? RequiredPlanId { get; set; }

    /// <summary>
    /// Whether users who don't have free access can purchase this course for a one-time fee.
    /// </summary>
    public bool AllowPurchase { get; set; } = false;

    /// <summary>
    /// Price for one-time purchase (when AllowPurchase is true).
    /// </summary>
    public decimal Price { get; set; } = 0;

    /// <summary>
    /// Currency for one-time purchase.
    /// </summary>
    public string Currency { get; set; } = "USD";

    // Stripe integration for one-time purchase
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }

    // Navigation
    public Channel Channel { get; set; } = null!;
    public ChannelPlan? RequiredPlan { get; set; }
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
