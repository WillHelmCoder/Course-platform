using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Api.Services;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;
using Pow.Domain.Enums;

namespace Pow.Api.Controllers;

/// <summary>
/// User CMS controller - authenticated user's subscriptions and content access.
/// </summary>
[ApiController]
[Route("api/cms/user")]
[Authorize]
public class UserCMSController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICMSService _cmsService;

    public UserCMSController(AppDbContext db, ICMSService cmsService)
    {
        _db = db;
        _cmsService = cmsService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst("sub")?.Value ??
                   User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                   Guid.Empty.ToString());

    /// <summary>
    /// Get current user's channel subscriptions.
    /// </summary>
    [HttpGet("my-subscriptions")]
    public async Task<ActionResult<List<UserSubscriptionDto>>> GetMySubscriptions()
    {
        var userId = GetUserId();

        var subscriptions = await _db.ChannelSubscriptions
            .Where(cs => cs.UserId == userId)
            .Select(cs => new UserSubscriptionDto(
                cs.Id,
                cs.ChannelId,
                cs.Channel.Name,
                cs.Channel.Slug,
                cs.ChannelPlanId,
                cs.ChannelPlan.Name,
                cs.SubscribedAt,
                cs.ExpiresAt,
                cs.Status,
                cs.Channel.Courses.Count
            ))
            .ToListAsync();

        return Ok(subscriptions);
    }

    /// <summary>
    /// Cancel a subscription.
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/cancel")]
    public async Task<ActionResult> CancelSubscription(Guid subscriptionId)
    {
        var userId = GetUserId();
        var cancelled = await _cmsService.CancelSubscriptionAsync(subscriptionId, userId);

        if (!cancelled)
            return NotFound("Subscription not found or doesn't belong to you");

        return Ok();
    }

    /// <summary>
    /// Get current user's purchased courses.
    /// </summary>
    [HttpGet("my-courses")]
    public async Task<ActionResult<List<UserPurchasedCourseDto>>> GetMyPurchasedCourses()
    {
        var userId = GetUserId();

        var courses = await _db.CoursePurchases
            .Where(cp => cp.UserId == userId)
            .Select(cp => new UserPurchasedCourseDto(
                cp.CourseId,
                cp.Course.Title,
                cp.Course.Description,
                cp.Course.MainPicture,
                cp.Course.Slug,
                cp.Course.Channel.Name,
                cp.Course.Channel.Slug,
                cp.Course.Chapters.Count,
                cp.PurchasedAt
            ))
            .ToListAsync();

        return Ok(courses);
    }

    /// <summary>
    /// Check if user has purchased a specific course.
    /// </summary>
    [HttpGet("courses/{courseId:guid}/purchased")]
    public async Task<ActionResult<bool>> HasPurchasedCourse(Guid courseId)
    {
        var userId = GetUserId();
        var purchased = await _db.CoursePurchases
            .AnyAsync(cp => cp.UserId == userId && cp.CourseId == courseId);

        return Ok(purchased);
    }

    // ===== FOLLOWS =====

    /// <summary>
    /// Get channels the user follows.
    /// </summary>
    [HttpGet("my-follows")]
    public async Task<ActionResult<List<ChannelFollowDto>>> GetMyFollows()
    {
        var userId = GetUserId();

        var follows = await _db.ChannelFollows
            .Where(cf => cf.UserId == userId)
            .Select(cf => new ChannelFollowDto(
                cf.ChannelId,
                cf.Channel.Name,
                cf.Channel.Slug,
                cf.Channel.MainPicture,
                cf.FollowedAt
            ))
            .OrderByDescending(f => f.FollowedAt)
            .ToListAsync();

        return Ok(follows);
    }

    /// <summary>
    /// Check if user follows a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/following")]
    public async Task<ActionResult<bool>> IsFollowingChannel(Guid channelId)
    {
        var userId = GetUserId();
        var isFollowing = await _db.ChannelFollows
            .AnyAsync(cf => cf.UserId == userId && cf.ChannelId == channelId);

        return Ok(isFollowing);
    }

    /// <summary>
    /// Follow a channel.
    /// </summary>
    [HttpPost("channels/{channelId:guid}/follow")]
    public async Task<ActionResult> FollowChannel(Guid channelId)
    {
        var userId = GetUserId();

        // Check if channel exists
        var channel = await _db.Channels.FindAsync(channelId);
        if (channel == null)
            return NotFound("Channel not found");

        // Check if already following
        var existingFollow = await _db.ChannelFollows
            .FirstOrDefaultAsync(cf => cf.UserId == userId && cf.ChannelId == channelId);

        if (existingFollow != null)
            return Ok(); // Already following

        var follow = new ChannelFollow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChannelId = channelId,
            FollowedAt = DateTime.UtcNow,
            TenantId = channel.TenantId
        };

        _db.ChannelFollows.Add(follow);
        await _db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Unfollow a channel.
    /// </summary>
    [HttpDelete("channels/{channelId:guid}/follow")]
    public async Task<ActionResult> UnfollowChannel(Guid channelId)
    {
        var userId = GetUserId();

        var follow = await _db.ChannelFollows
            .FirstOrDefaultAsync(cf => cf.UserId == userId && cf.ChannelId == channelId);

        if (follow == null)
            return Ok(); // Not following

        _db.ChannelFollows.Remove(follow);
        await _db.SaveChangesAsync();

        return Ok();
    }

    // ===== FEED =====

    /// <summary>
    /// Get personalized feed with content from followed/subscribed channels.
    /// </summary>
    [HttpGet("feed")]
    public async Task<ActionResult<List<FeedItemDto>>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();

        // Get channels the user follows or is subscribed to
        var followedChannelIds = await _db.ChannelFollows
            .Where(cf => cf.UserId == userId)
            .Select(cf => cf.ChannelId)
            .ToListAsync();

        var subscribedChannelIds = await _db.ChannelSubscriptions
            .Where(cs => cs.UserId == userId && cs.Status == SubscriptionStatus.Active)
            .Select(cs => cs.ChannelId)
            .ToListAsync();

        var channelIds = followedChannelIds.Union(subscribedChannelIds).Distinct().ToList();

        if (!channelIds.Any())
            return Ok(new List<FeedItemDto>());

        // Get recent published content from these channels
        var feed = await _db.Contents
            .Where(c => c.IsPublished && c.Chapter.Course.IsPublished && c.Chapter.Course.IsActive)
            .Where(c => channelIds.Contains(c.Chapter.Course.ChannelId))
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new FeedItemDto(
                c.Id,
                c.Title,
                c.Description,
                c.MainPicture,
                c.Slug,
                c.AccessLevel,
                c.PublishedAt,
                c.Chapter.CourseId,
                c.Chapter.Course.Title,
                c.Chapter.Course.Slug,
                c.Chapter.Course.ChannelId,
                c.Chapter.Course.Channel.Name,
                c.Chapter.Course.Channel.Slug,
                c.Chapter.Course.Channel.MainPicture,
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => (Guid?)a.UserId).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.DisplayName ?? a.User.Email).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.ProfilePicture).FirstOrDefault()
            ))
            .ToListAsync();

        return Ok(feed);
    }
}
