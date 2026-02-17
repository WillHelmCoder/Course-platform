using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;
using Pow.Domain.Enums;
using Stripe;
using Stripe.Checkout;

namespace Pow.Api.Services;

public class StripeService : IStripeService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;
    private readonly string? _webhookSecret;
    private readonly string? _publishableKey;
    private readonly bool _isConfigured;

    public StripeService(AppDbContext db, IConfiguration config, ILogger<StripeService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;

        var secretKey = _config["Stripe:SecretKey"];
        _webhookSecret = _config["Stripe:WebhookSecret"];
        _publishableKey = _config["Stripe:PublishableKey"];

        _isConfigured = !string.IsNullOrEmpty(secretKey) &&
                        !string.IsNullOrEmpty(_webhookSecret) &&
                        !string.IsNullOrEmpty(_publishableKey) &&
                        !secretKey.Contains("xxx"); // Ignore placeholder values

        if (_isConfigured)
        {
            StripeConfiguration.ApiKey = secretKey;
            _logger.LogInformation("Stripe configured successfully");
        }
        else
        {
            _logger.LogWarning("Stripe is not fully configured. Payment features will be disabled.");
        }
    }

    public string GetPublishableKey() => _publishableKey ?? string.Empty;

    private void EnsureConfigured()
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured. Please set Stripe:SecretKey, Stripe:WebhookSecret, and Stripe:PublishableKey in appsettings.json");
        }
    }

    public async Task<string> GetOrCreateCustomerAsync(User user)
    {
        EnsureConfigured();

        if (!string.IsNullOrEmpty(user.StripeCustomerId))
        {
            return user.StripeCustomerId;
        }

        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.DisplayName ?? user.Email,
            Metadata = new Dictionary<string, string>
            {
                { "userId", user.Id.ToString() }
            }
        });

        user.StripeCustomerId = customer.Id;
        await _db.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(Guid userId, Guid channelPlanId, string successUrl, string cancelUrl)
    {
        EnsureConfigured();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        var plan = await _db.ChannelPlans
            .Include(p => p.Channel)
            .FirstOrDefaultAsync(p => p.Id == channelPlanId);

        if (plan == null) return null;

        // Ensure plan is synced to Stripe
        if (string.IsNullOrEmpty(plan.StripePriceId))
        {
            var syncResult = await SyncPlanToStripeAsync(channelPlanId);
            if (!syncResult.Success) return null;

            // Reload plan to get updated StripePriceId
            await _db.Entry(plan).ReloadAsync();
        }

        var customerId = await GetOrCreateCustomerAsync(user);

        var sessionOptions = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            Mode = plan.Interval == "lifetime" ? "payment" : "subscription",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "channelPlanId", channelPlanId.ToString() },
                { "channelId", plan.ChannelId.ToString() }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = plan.StripePriceId,
                    Quantity = 1
                }
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions);

        return new CheckoutSessionResponse(session.Id, session.Url);
    }

    public async Task<CheckoutSessionResponse?> CreatePlatformCheckoutSessionAsync(Guid userId, Guid planId, string successUrl, string cancelUrl)
    {
        EnsureConfigured();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        var plan = await _db.Plans.FindAsync(planId);
        if (plan == null) return null;

        // Ensure plan is synced to Stripe
        if (string.IsNullOrEmpty(plan.StripePriceId))
        {
            var syncResult = await SyncPlatformPlanToStripeAsync(planId);
            if (!syncResult.Success) return null;

            // Reload plan to get updated StripePriceId
            await _db.Entry(plan).ReloadAsync();
        }

        var customerId = await GetOrCreateCustomerAsync(user);

        var sessionOptions = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            Mode = plan.Interval == "lifetime" ? "payment" : "subscription",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "platformPlanId", planId.ToString() },
                { "type", "platform" }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = plan.StripePriceId,
                    Quantity = 1
                }
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions);

        return new CheckoutSessionResponse(session.Id, session.Url);
    }

    public async Task<PortalSessionResponse?> CreatePortalSessionAsync(Guid userId, string returnUrl)
    {
        EnsureConfigured();

        var user = await _db.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
        {
            return null;
        }

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            ReturnUrl = returnUrl
        });

        return new PortalSessionResponse(session.Url);
    }

    public async Task<SyncPlanResponse> SyncPlanToStripeAsync(Guid channelPlanId)
    {
        EnsureConfigured();

        var plan = await _db.ChannelPlans
            .Include(p => p.Channel)
            .FirstOrDefaultAsync(p => p.Id == channelPlanId);

        if (plan == null)
        {
            return new SyncPlanResponse(false, null, null, "Plan not found");
        }

        if (plan.Price <= 0)
        {
            return new SyncPlanResponse(false, null, null, "Cannot sync free plans to Stripe");
        }

        try
        {
            var productService = new ProductService();
            var priceService = new PriceService();

            Product product;

            // Create or update product
            if (!string.IsNullOrEmpty(plan.StripeProductId))
            {
                product = await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
                {
                    Name = $"{plan.Channel.Name} - {plan.Name}",
                    Description = plan.Description
                });
            }
            else
            {
                product = await productService.CreateAsync(new ProductCreateOptions
                {
                    Name = $"{plan.Channel.Name} - {plan.Name}",
                    Description = plan.Description,
                    Metadata = new Dictionary<string, string>
                    {
                        { "channelPlanId", plan.Id.ToString() },
                        { "channelId", plan.ChannelId.ToString() }
                    }
                });
                plan.StripeProductId = product.Id;
            }

            // Create new price (prices are immutable in Stripe)
            var priceOptions = new PriceCreateOptions
            {
                Product = product.Id,
                UnitAmount = (long)(plan.Price * 100), // Convert to cents
                Currency = plan.Currency.ToLower(),
                Metadata = new Dictionary<string, string>
                {
                    { "channelPlanId", plan.Id.ToString() }
                }
            };

            if (plan.Interval != "lifetime")
            {
                priceOptions.Recurring = new PriceRecurringOptions
                {
                    Interval = plan.Interval == "yearly" ? "year" : "month"
                };
            }

            var price = await priceService.CreateAsync(priceOptions);
            plan.StripePriceId = price.Id;

            await _db.SaveChangesAsync();

            return new SyncPlanResponse(true, product.Id, price.Id, "Plan synced successfully");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to sync plan {PlanId} to Stripe", channelPlanId);
            return new SyncPlanResponse(false, null, null, ex.Message);
        }
    }

    public async Task<SyncPlanResponse> SyncPlatformPlanToStripeAsync(Guid planId)
    {
        EnsureConfigured();

        var plan = await _db.Plans.FindAsync(planId);

        if (plan == null)
        {
            return new SyncPlanResponse(false, null, null, "Plan not found");
        }

        if (plan.Price <= 0)
        {
            return new SyncPlanResponse(false, null, null, "Cannot sync free plans to Stripe");
        }

        try
        {
            var productService = new ProductService();
            var priceService = new PriceService();

            Product product;

            // Create or update product
            if (!string.IsNullOrEmpty(plan.StripeProductId))
            {
                product = await productService.UpdateAsync(plan.StripeProductId, new ProductUpdateOptions
                {
                    Name = $"Platform - {plan.Name}",
                    Description = plan.Description
                });
            }
            else
            {
                product = await productService.CreateAsync(new ProductCreateOptions
                {
                    Name = $"Platform - {plan.Name}",
                    Description = plan.Description,
                    Metadata = new Dictionary<string, string>
                    {
                        { "platformPlanId", plan.Id.ToString() },
                        { "type", "platform" }
                    }
                });
                plan.StripeProductId = product.Id;
            }

            // Create new price (prices are immutable in Stripe)
            var priceOptions = new PriceCreateOptions
            {
                Product = product.Id,
                UnitAmount = (long)(plan.Price * 100), // Convert to cents
                Currency = plan.Currency.ToLower(),
                Metadata = new Dictionary<string, string>
                {
                    { "platformPlanId", plan.Id.ToString() }
                }
            };

            if (plan.Interval != "lifetime")
            {
                priceOptions.Recurring = new PriceRecurringOptions
                {
                    Interval = plan.Interval == "yearly" ? "year" : "month"
                };
            }

            var price = await priceService.CreateAsync(priceOptions);
            plan.StripePriceId = price.Id;

            await _db.SaveChangesAsync();

            return new SyncPlanResponse(true, product.Id, price.Id, "Platform plan synced successfully");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to sync platform plan {PlanId} to Stripe", planId);
            return new SyncPlanResponse(false, null, null, ex.Message);
        }
    }

    public async Task<CheckoutSessionResponse?> CreateCourseCheckoutSessionAsync(Guid userId, Guid courseId, string successUrl, string cancelUrl)
    {
        EnsureConfigured();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return null;

        var course = await _db.Courses
            .Include(c => c.Channel)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return null;

        // Check if already purchased
        var alreadyPurchased = await _db.CoursePurchases
            .AnyAsync(p => p.UserId == userId && p.CourseId == courseId);

        if (alreadyPurchased)
        {
            _logger.LogWarning("User {UserId} already purchased course {CourseId}", userId, courseId);
            return null;
        }

        // Ensure course is synced to Stripe
        if (string.IsNullOrEmpty(course.StripePriceId))
        {
            var syncResult = await SyncCourseToStripeAsync(courseId);
            if (!syncResult.Success) return null;

            // Reload course to get updated StripePriceId
            await _db.Entry(course).ReloadAsync();
        }

        var customerId = await GetOrCreateCustomerAsync(user);

        var sessionOptions = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment", // One-time payment
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "courseId", courseId.ToString() },
                { "type", "course" }
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = course.StripePriceId,
                    Quantity = 1
                }
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions);

        return new CheckoutSessionResponse(session.Id, session.Url);
    }

    public async Task<SyncPlanResponse> SyncCourseToStripeAsync(Guid courseId)
    {
        EnsureConfigured();

        var course = await _db.Courses
            .Include(c => c.Channel)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
        {
            return new SyncPlanResponse(false, null, null, "Course not found");
        }

        if (course.Price <= 0)
        {
            return new SyncPlanResponse(false, null, null, "Cannot sync free courses to Stripe");
        }

        try
        {
            var productService = new ProductService();
            var priceService = new PriceService();

            Product product;

            // Create or update product
            if (!string.IsNullOrEmpty(course.StripeProductId))
            {
                product = await productService.UpdateAsync(course.StripeProductId, new ProductUpdateOptions
                {
                    Name = $"{course.Channel.Name} - {course.Title}",
                    Description = course.Description
                });
            }
            else
            {
                product = await productService.CreateAsync(new ProductCreateOptions
                {
                    Name = $"{course.Channel.Name} - {course.Title}",
                    Description = course.Description,
                    Metadata = new Dictionary<string, string>
                    {
                        { "courseId", course.Id.ToString() },
                        { "channelId", course.ChannelId.ToString() },
                        { "type", "course" }
                    }
                });
                course.StripeProductId = product.Id;
            }

            // Create new price (one-time, not recurring)
            var priceOptions = new PriceCreateOptions
            {
                Product = product.Id,
                UnitAmount = (long)(course.Price * 100), // Convert to cents
                Currency = course.Currency.ToLower(),
                Metadata = new Dictionary<string, string>
                {
                    { "courseId", course.Id.ToString() }
                }
            };

            var price = await priceService.CreateAsync(priceOptions);
            course.StripePriceId = price.Id;

            await _db.SaveChangesAsync();

            return new SyncPlanResponse(true, product.Id, price.Id, "Course synced successfully");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to sync course {CourseId} to Stripe", courseId);
            return new SyncPlanResponse(false, null, null, ex.Message);
        }
    }

    public async Task<bool> IsEventProcessedAsync(string eventId)
    {
        return await _db.StripeWebhookEvents.AnyAsync(e => e.EventId == eventId);
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature)
    {
        EnsureConfigured();

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret!);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Webhook signature verification failed");
            return false;
        }

        // Check idempotency
        if (await IsEventProcessedAsync(stripeEvent.Id))
        {
            _logger.LogInformation("Event {EventId} already processed", stripeEvent.Id);
            return true;
        }

        var webhookEvent = new StripeWebhookEvent
        {
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaid(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }

            webhookEvent.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", stripeEvent.Id);
            webhookEvent.Success = false;
            webhookEvent.ErrorMessage = ex.Message;
        }

        _db.StripeWebhookEvents.Add(webhookEvent);
        await _db.SaveChangesAsync();

        return webhookEvent.Success;
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var userId = Guid.Parse(session.Metadata["userId"]);

        // Check the type of checkout
        if (session.Metadata.TryGetValue("type", out var type))
        {
            switch (type)
            {
                case "platform":
                    await HandlePlatformCheckoutCompleted(session, userId);
                    break;
                case "course":
                    await HandleCourseCheckoutCompleted(session, userId);
                    break;
                default:
                    await HandleChannelCheckoutCompleted(session, userId);
                    break;
            }
        }
        else
        {
            await HandleChannelCheckoutCompleted(session, userId);
        }
    }

    private async Task HandleCourseCheckoutCompleted(Session session, Guid userId)
    {
        var courseId = Guid.Parse(session.Metadata["courseId"]);

        var course = await _db.Courses.FindAsync(courseId);
        if (course == null) return;

        // Check if already purchased (idempotency)
        var existingPurchase = await _db.CoursePurchases
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CourseId == courseId);

        if (existingPurchase != null)
        {
            _logger.LogInformation("Course {CourseId} already purchased by user {UserId}", courseId, userId);
            return;
        }

        // Create purchase record
        var purchase = new CoursePurchase
        {
            UserId = userId,
            CourseId = courseId,
            TenantId = course.TenantId,
            PurchasedAt = DateTime.UtcNow,
            PricePaid = course.Price,
            Currency = course.Currency,
            StripePaymentIntentId = session.PaymentIntentId,
            StripeSessionId = session.Id
        };

        _db.CoursePurchases.Add(purchase);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Course {CourseId} purchased by user {UserId}", courseId, userId);
    }

    private async Task HandlePlatformCheckoutCompleted(Session session, Guid userId)
    {
        var platformPlanId = Guid.Parse(session.Metadata["platformPlanId"]);

        var plan = await _db.Plans.FindAsync(platformPlanId);
        if (plan == null) return;

        // Check if user already has a plan
        var existingUserPlan = await _db.UserPlans
            .FirstOrDefaultAsync(up => up.UserId == userId);

        if (existingUserPlan != null)
        {
            // Update existing plan
            existingUserPlan.PlanId = platformPlanId;
            existingUserPlan.StartDate = DateTime.UtcNow;
            existingUserPlan.EndDate = CalculateExpiration(plan.Interval);
            existingUserPlan.Status = "active";
            existingUserPlan.ExternalSubscriptionId = session.SubscriptionId;
        }
        else
        {
            // Create new user plan
            var userPlan = new UserPlan
            {
                UserId = userId,
                PlanId = platformPlanId,
                StartDate = DateTime.UtcNow,
                EndDate = CalculateExpiration(plan.Interval),
                Status = "active",
                ExternalSubscriptionId = session.SubscriptionId
            };

            _db.UserPlans.Add(userPlan);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Created/updated platform plan for user {UserId} to plan {PlanId}", userId, platformPlanId);
    }

    private async Task HandleChannelCheckoutCompleted(Session session, Guid userId)
    {
        var channelPlanId = Guid.Parse(session.Metadata["channelPlanId"]);
        var channelId = Guid.Parse(session.Metadata["channelId"]);

        var plan = await _db.ChannelPlans.FindAsync(channelPlanId);
        if (plan == null) return;

        // Check if subscription already exists
        var existingSubscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ChannelId == channelId);

        if (existingSubscription != null)
        {
            // Update existing subscription
            existingSubscription.ChannelPlanId = channelPlanId;
            existingSubscription.Status = SubscriptionStatus.Active;
            existingSubscription.SubscribedAt = DateTime.UtcNow;
            existingSubscription.ExternalSubscriptionId = session.SubscriptionId;
            existingSubscription.StripeCustomerId = session.CustomerId;
            existingSubscription.StripePaymentIntentId = session.PaymentIntentId;
            existingSubscription.PricePaid = plan.Price;
            existingSubscription.Currency = plan.Currency;
            existingSubscription.ExpiresAt = CalculateExpiration(plan.Interval);
        }
        else
        {
            // Create new subscription
            var subscription = new ChannelSubscription
            {
                UserId = userId,
                ChannelId = channelId,
                ChannelPlanId = channelPlanId,
                TenantId = plan.TenantId,
                Status = SubscriptionStatus.Active,
                SubscribedAt = DateTime.UtcNow,
                ExternalSubscriptionId = session.SubscriptionId,
                StripeCustomerId = session.CustomerId,
                StripePaymentIntentId = session.PaymentIntentId,
                PricePaid = plan.Price,
                Currency = plan.Currency,
                ExpiresAt = CalculateExpiration(plan.Interval)
            };

            _db.ChannelSubscriptions.Add(subscription);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Created/updated channel subscription for user {UserId} to plan {PlanId}", userId, channelPlanId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var localSubscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscription.Id);

        if (localSubscription == null) return;

        localSubscription.Status = subscription.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.Pending,
            "canceled" => SubscriptionStatus.Cancelled,
            "unpaid" => SubscriptionStatus.Pending,
            _ => localSubscription.Status
        };

        if (subscription.CurrentPeriodEnd != DateTime.MinValue)
        {
            localSubscription.ExpiresAt = subscription.CurrentPeriodEnd;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated subscription {SubscriptionId}", subscription.Id);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var localSubscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscription.Id);

        if (localSubscription == null) return;

        localSubscription.Status = SubscriptionStatus.Cancelled;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscription.Id);
    }

    private async Task HandleInvoicePaid(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        var localSubscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == invoice.SubscriptionId);

        if (localSubscription == null) return;

        localSubscription.PricePaid = invoice.AmountPaid / 100m; // Convert from cents
        localSubscription.Status = SubscriptionStatus.Active;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Invoice paid for subscription {SubscriptionId}", invoice.SubscriptionId);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        var localSubscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == invoice.SubscriptionId);

        if (localSubscription == null) return;

        localSubscription.Status = SubscriptionStatus.Pending;
        await _db.SaveChangesAsync();
        _logger.LogWarning("Payment failed for subscription {SubscriptionId}", invoice.SubscriptionId);
    }

    private static DateTime? CalculateExpiration(string interval)
    {
        return interval switch
        {
            "monthly" => DateTime.UtcNow.AddMonths(1),
            "yearly" => DateTime.UtcNow.AddYears(1),
            "lifetime" => null, // Never expires
            _ => DateTime.UtcNow.AddMonths(1)
        };
    }
}
