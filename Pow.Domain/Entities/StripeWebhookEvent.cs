namespace Pow.Domain.Entities;

/// <summary>
/// Tracks processed Stripe webhook events for idempotency.
/// Prevents processing the same event multiple times.
/// </summary>
public class StripeWebhookEvent : TenantEntity
{
    /// <summary>
    /// Stripe event ID (evt_xxx)
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Event type (e.g., checkout.session.completed)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// When the event was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the event was processed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
