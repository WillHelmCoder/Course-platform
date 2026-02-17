using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pow.Api.Services;
using Pow.Domain.DTOs;

namespace Pow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<StripeController> _logger;

    public StripeController(IStripeService stripeService, ILogger<StripeController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// Get Stripe publishable key for frontend
    /// </summary>
    [HttpGet("config")]
    public ActionResult<StripeConfigResponse> GetConfig()
    {
        return Ok(new StripeConfigResponse(_stripeService.GetPublishableKey()));
    }

    /// <summary>
    /// Create a Stripe Checkout session for subscribing to a channel plan
    /// </summary>
    [Authorize]
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);

        // Default URLs if not provided
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = request.SuccessUrl ?? $"{baseUrl}/subscription/success";
        var cancelUrl = request.CancelUrl ?? $"{baseUrl}/subscription/cancel";

        var result = await _stripeService.CreateCheckoutSessionAsync(userId, request.ChannelPlanId, successUrl, cancelUrl);

        if (result == null)
            return BadRequest(new { message = "Failed to create checkout session" });

        return Ok(result);
    }

    /// <summary>
    /// Create a Stripe Checkout session for subscribing to a platform plan
    /// </summary>
    [Authorize]
    [HttpPost("platform-checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreatePlatformCheckoutSession([FromBody] CreatePlatformCheckoutRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);

        // Default URLs if not provided
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = request.SuccessUrl ?? $"{baseUrl}/subscription/success";
        var cancelUrl = request.CancelUrl ?? $"{baseUrl}/plans";

        var result = await _stripeService.CreatePlatformCheckoutSessionAsync(userId, request.PlanId, successUrl, cancelUrl);

        if (result == null)
            return BadRequest(new { message = "Failed to create checkout session" });

        return Ok(result);
    }

    /// <summary>
    /// Create a Stripe Checkout session for purchasing a course (one-time payment)
    /// </summary>
    [Authorize]
    [HttpPost("course-checkout")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCourseCheckoutSession([FromBody] CreateCourseCheckoutRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);

        // Default URLs if not provided
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = request.SuccessUrl ?? $"{baseUrl}/subscription/success";
        var cancelUrl = request.CancelUrl ?? $"{baseUrl}/courses";

        var result = await _stripeService.CreateCourseCheckoutSessionAsync(userId, request.CourseId, successUrl, cancelUrl);

        if (result == null)
            return BadRequest(new { message = "Failed to create checkout session. You may have already purchased this course." });

        return Ok(result);
    }

    /// <summary>
    /// Create a Stripe Customer Portal session for managing subscriptions
    /// </summary>
    [Authorize]
    [HttpPost("portal")]
    public async Task<ActionResult<PortalSessionResponse>> CreatePortalSession([FromBody] CreatePortalSessionRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var returnUrl = request.ReturnUrl ?? $"{baseUrl}/subscriptions";

        var result = await _stripeService.CreatePortalSessionAsync(userId, returnUrl);

        if (result == null)
            return BadRequest(new { message = "Failed to create portal session. Make sure you have an active subscription." });

        return Ok(result);
    }

    /// <summary>
    /// Sync a channel plan to Stripe (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("sync-plan/{planId:guid}")]
    public async Task<ActionResult<SyncPlanResponse>> SyncPlan(Guid planId)
    {
        var result = await _stripeService.SyncPlanToStripeAsync(planId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Sync a platform plan to Stripe (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("sync-platform-plan/{planId:guid}")]
    public async Task<ActionResult<SyncPlanResponse>> SyncPlatformPlan(Guid planId)
    {
        var result = await _stripeService.SyncPlatformPlanToStripeAsync(planId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Stripe webhook endpoint - receives events from Stripe
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Webhook received without signature");
            return BadRequest();
        }

        var success = await _stripeService.HandleWebhookAsync(json, signature);

        if (!success)
        {
            _logger.LogWarning("Webhook processing failed");
            return BadRequest();
        }

        return Ok();
    }
}
