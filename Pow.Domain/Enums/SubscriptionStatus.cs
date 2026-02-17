namespace Pow.Domain.Enums;

/// <summary>
/// Channel subscription status.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Active subscription</summary>
    Active = 0,

    /// <summary>Subscription cancelled but still valid until end date</summary>
    Cancelled = 1,

    /// <summary>Subscription expired</summary>
    Expired = 2,

    /// <summary>Pending payment confirmation</summary>
    Pending = 3
}
