using Pow.Domain.DTOs;
using Pow.Domain.Entities;

namespace Pow.Api.Services;

public interface IStripeService
{
    /// <summary>
    /// Create a Stripe Checkout session for subscribing to a channel plan
    /// </summary>
    Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid userId, Guid channelPlanId, string successUrl, string cancelUrl);

    /// <summary>
    /// Create a Stripe Checkout session for subscribing to a platform plan
    /// </summary>
    Task<CheckoutSessionResponse?> CreatePlatformCheckoutSessionAsync(Guid userId, Guid planId, string successUrl, string cancelUrl);

    /// <summary>
    /// Create a Stripe Customer Portal session for managing subscriptions
    /// </summary>
    Task<PortalSessionResponse?> CreatePortalSessionAsync(Guid userId, string returnUrl);

    /// <summary>
    /// Sync a channel plan to Stripe (create/update product and price)
    /// </summary>
    Task<SyncPlanResponse> SyncPlanToStripeAsync(Guid channelPlanId);

    /// <summary>
    /// Sync a platform plan to Stripe (create/update product and price)
    /// </summary>
    Task<SyncPlanResponse> SyncPlatformPlanToStripeAsync(Guid planId);

    /// <summary>
    /// Create a Stripe Checkout session for purchasing a course (one-time payment)
    /// </summary>
    Task<CheckoutSessionResponse?> CreateCourseCheckoutSessionAsync(Guid userId, Guid courseId, string successUrl, string cancelUrl);

    /// <summary>
    /// Sync a course to Stripe (create/update product and price)
    /// </summary>
    Task<SyncPlanResponse> SyncCourseToStripeAsync(Guid courseId);

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    Task<bool> HandleWebhookAsync(string payload, string signature);

    /// <summary>
    /// Get or create a Stripe customer for a user
    /// </summary>
    Task<string> GetOrCreateCustomerAsync(User user);

    /// <summary>
    /// Check if a webhook event has already been processed (idempotency)
    /// </summary>
    Task<bool> IsEventProcessedAsync(string eventId);

    /// <summary>
    /// Get Stripe publishable key for frontend
    /// </summary>
    string GetPublishableKey();
}
