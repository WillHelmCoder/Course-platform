using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pow.Domain.DTOs;
using Pow.Domain.Interfaces;

namespace Pow.Api.Controllers;

/// <summary>
/// Controller for user access information.
/// Provides endpoints to check what the current user can access.
/// </summary>
[ApiController]
[Route("api/user-access")]
[Authorize]
public class UserAccessController : ControllerBase
{
    private readonly IUserAccessService _accessService;

    public UserAccessController(IUserAccessService accessService)
    {
        _accessService = accessService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Get summary of current user's access (admin channels, subscribed channels)
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<UserAccessSummaryDto>> GetAccessSummary()
    {
        var summary = await _accessService.GetUserAccessSummaryAsync(GetUserId());
        return Ok(summary);
    }

    /// <summary>
    /// Get channels where current user is admin
    /// </summary>
    [HttpGet("admin-channels")]
    public async Task<ActionResult<List<ChannelAccessDto>>> GetAdminChannels()
    {
        var channels = await _accessService.GetAdminChannelsAsync(GetUserId());
        return Ok(channels);
    }

    /// <summary>
    /// Get channels where current user has subscription
    /// </summary>
    [HttpGet("subscribed-channels")]
    public async Task<ActionResult<List<ChannelAccessDto>>> GetSubscribedChannels()
    {
        var channels = await _accessService.GetSubscribedChannelsAsync(GetUserId());
        return Ok(channels);
    }

    /// <summary>
    /// Check if current user can access a specific channel's premium content
    /// </summary>
    [HttpGet("channel/{channelId}/can-access")]
    public async Task<ActionResult<bool>> CanAccessChannel(Guid channelId)
    {
        var canAccess = await _accessService.CanAccessChannelContentAsync(GetUserId(), channelId);
        return Ok(canAccess);
    }

    /// <summary>
    /// Check if current user can access a specific course
    /// </summary>
    [HttpGet("course/{courseId}/can-access")]
    public async Task<ActionResult<CourseAccessResultDto>> CanAccessCourse(Guid courseId)
    {
        var result = await _accessService.CanAccessCourseAsync(GetUserId(), courseId);
        return Ok(result);
    }

    /// <summary>
    /// Check if current user can access specific content.
    /// Works for both authenticated and anonymous users.
    /// </summary>
    [HttpGet("content/{contentId}/can-access")]
    [AllowAnonymous]
    public async Task<ActionResult<ContentAccessResultDto>> CanAccessContent(Guid contentId)
    {
        Guid? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdClaim))
            userId = Guid.Parse(userIdClaim);

        var result = await _accessService.CanAccessContentAsync(userId, contentId);
        return Ok(result);
    }

    /// <summary>
    /// Subscribe to a channel (for testing/free channels)
    /// </summary>
    [HttpPost("channel/{channelId}/subscribe")]
    public async Task<ActionResult> SubscribeToChannel(Guid channelId)
    {
        var success = await _accessService.SubscribeToChannelAsync(GetUserId(), channelId);
        if (success)
            return Ok(new { message = "Subscribed successfully" });
        return BadRequest(new { message = "Failed to subscribe" });
    }

    /// <summary>
    /// Unsubscribe from a channel
    /// </summary>
    [HttpPost("channel/{channelId}/unsubscribe")]
    public async Task<ActionResult> UnsubscribeFromChannel(Guid channelId)
    {
        var success = await _accessService.UnsubscribeFromChannelAsync(GetUserId(), channelId);
        if (success)
            return Ok(new { message = "Unsubscribed successfully" });
        return BadRequest(new { message = "Failed to unsubscribe or not subscribed" });
    }
}
