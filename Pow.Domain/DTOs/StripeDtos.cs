namespace Pow.Domain.DTOs;

// ===== CHECKOUT =====

/// <summary>
/// Request to create a Stripe Checkout session for a channel plan subscription
/// </summary>
public record CreateCheckoutSessionRequest(
    Guid ChannelPlanId,
    string? SuccessUrl = null,
    string? CancelUrl = null
);

/// <summary>
/// Request to create a Stripe Checkout session for a platform plan subscription
/// </summary>
public record CreatePlatformCheckoutRequest(
    Guid PlanId,
    string? SuccessUrl = null,
    string? CancelUrl = null
);

/// <summary>
/// Request to create a Stripe Checkout session for a one-time course purchase
/// </summary>
public record CreateCourseCheckoutRequest(
    Guid CourseId,
    string? SuccessUrl = null,
    string? CancelUrl = null
);

/// <summary>
/// Response containing the Checkout session URL
/// </summary>
public record CheckoutSessionResponse(
    string SessionId,
    string Url
);

// ===== CUSTOMER PORTAL =====

/// <summary>
/// Request to create a Stripe Customer Portal session
/// </summary>
public record CreatePortalSessionRequest(
    string? ReturnUrl = null
);

/// <summary>
/// Response containing the Portal session URL
/// </summary>
public record PortalSessionResponse(
    string Url
);

// ===== SYNC =====

/// <summary>
/// Response from syncing a plan to Stripe
/// </summary>
public record SyncPlanResponse(
    bool Success,
    string? ProductId,
    string? PriceId,
    string? Message
);

// ===== PUBLISHABLE KEY =====

/// <summary>
/// Response containing the Stripe publishable key for frontend
/// </summary>
public record StripeConfigResponse(
    string PublishableKey
);
