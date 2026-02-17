using System.ComponentModel.DataAnnotations;

namespace Pow.Domain.Entities;

public class UserPlan : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Status: active, cancelled, expired, pending
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// External subscription ID (Stripe, PayPal, etc.)
    /// </summary>
    public string? ExternalSubscriptionId { get; set; }
}
