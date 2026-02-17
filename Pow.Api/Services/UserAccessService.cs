using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;
using Pow.Domain.Enums;
using Pow.Domain.Interfaces;

namespace Pow.Api.Services;

/// <summary>
/// Centralized service for checking user access to channels, courses, and content.
/// </summary>
public class UserAccessService : IUserAccessService
{
    private readonly AppDbContext _db;

    public UserAccessService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserAccessSummaryDto> GetUserAccessSummaryAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        var isSuperAdmin = user?.Role == "SuperAdmin";

        var adminChannels = await GetAdminChannelsAsync(userId);
        var subscribedChannels = await GetSubscribedChannelsAsync(userId);

        return new UserAccessSummaryDto(adminChannels, subscribedChannels, isSuperAdmin);
    }

    public async Task<bool> IsChannelAdminAsync(Guid userId, Guid channelId)
    {
        // SuperAdmin is admin of all channels
        var user = await _db.Users.FindAsync(userId);
        if (user?.Role == "SuperAdmin") return true;

        return await _db.ChannelAdmins
            .AnyAsync(ca => ca.UserId == userId && ca.ChannelId == channelId);
    }

    public async Task<bool> IsChannelSubscriberAsync(Guid userId, Guid channelId)
    {
        return await _db.ChannelSubscriptions
            .AnyAsync(cs => cs.UserId == userId
                && cs.ChannelId == channelId
                && cs.Status == SubscriptionStatus.Active
                && (cs.ExpiresAt == null || cs.ExpiresAt > DateTime.UtcNow));
    }

    /// <summary>
    /// Get the user's subscription to a channel (if any).
    /// </summary>
    public async Task<ChannelSubscription?> GetUserSubscriptionAsync(Guid userId, Guid channelId)
    {
        return await _db.ChannelSubscriptions
            .Include(cs => cs.ChannelPlan)
            .FirstOrDefaultAsync(cs => cs.UserId == userId
                && cs.ChannelId == channelId
                && cs.Status == SubscriptionStatus.Active
                && (cs.ExpiresAt == null || cs.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> CanAccessChannelContentAsync(Guid userId, Guid channelId)
    {
        // Admin can access everything
        if (await IsChannelAdminAsync(userId, channelId))
            return true;

        // Subscriber can access
        if (await IsChannelSubscriberAsync(userId, channelId))
            return true;

        return false;
    }

    public async Task<CourseAccessResultDto> CanAccessCourseAsync(Guid userId, Guid courseId)
    {
        var course = await _db.Courses
            .Include(c => c.Channel)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return new CourseAccessResultDto(false, "Course not found", CourseAccessType.Free, Guid.Empty, "");

        // Free courses are accessible to everyone
        if (course.AccessType == CourseAccessType.Free)
            return new CourseAccessResultDto(true, null, course.AccessType, course.ChannelId, course.Channel.Name);

        // Check if user can access channel content
        if (await CanAccessChannelContentAsync(userId, course.ChannelId))
            return new CourseAccessResultDto(true, null, course.AccessType, course.ChannelId, course.Channel.Name);

        return new CourseAccessResultDto(
            false,
            $"This course requires a subscription to {course.Channel.Name}",
            course.AccessType,
            course.ChannelId,
            course.Channel.Name
        );
    }

    public async Task<ContentAccessResultDto> CanAccessContentAsync(Guid? userId, Guid contentId)
    {
        var content = await _db.Contents
            .Include(c => c.Chapter)
                .ThenInclude(ch => ch.Course)
                    .ThenInclude(co => co.Channel)
            .FirstOrDefaultAsync(c => c.Id == contentId);

        if (content == null)
            return new ContentAccessResultDto(false, "Content not found", null, null);

        var course = content.Chapter.Course;
        var channel = course.Channel;

        // Public content - no login required
        if (content.AccessLevel == ContentAccessLevel.Public)
            return new ContentAccessResultDto(true, null, null, null);

        // From here, user must be logged in
        if (!userId.HasValue)
            return new ContentAccessResultDto(false, "Login required to view this content", channel.Id, channel.Name);

        // LoggedIn - any authenticated user
        if (content.AccessLevel == ContentAccessLevel.LoggedIn)
            return new ContentAccessResultDto(true, null, null, null);

        // Check if user is admin (admins can access everything)
        if (await IsChannelAdminAsync(userId.Value, channel.Id))
            return new ContentAccessResultDto(true, null, null, null);

        // AllSubscribers - any subscriber of the channel
        if (content.AccessLevel == ContentAccessLevel.AllSubscribers)
        {
            if (await IsChannelSubscriberAsync(userId.Value, channel.Id))
                return new ContentAccessResultDto(true, null, null, null);

            return new ContentAccessResultDto(
                false,
                $"This content requires a subscription to {channel.Name}",
                channel.Id,
                channel.Name
            );
        }

        return new ContentAccessResultDto(false, "Access denied", channel.Id, channel.Name);
    }

    /// <summary>
    /// Check access with optional access code override.
    /// </summary>
    public async Task<ContentAccessResultDto> CanAccessContentAsync(Guid? userId, Guid contentId, string? accessCode)
    {
        // First check if access code is valid
        if (!string.IsNullOrEmpty(accessCode))
        {
            var content = await _db.Contents.FindAsync(contentId);
            if (content != null && !string.IsNullOrEmpty(content.AccessCode) && content.AccessCode == accessCode)
            {
                return new ContentAccessResultDto(true, null, null, null);
            }
        }

        // Fall back to normal access check
        return await CanAccessContentAsync(userId, contentId);
    }

    public async Task<List<ChannelAccessDto>> GetAdminChannelsAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);

        // SuperAdmin gets all channels
        if (user?.Role == "SuperAdmin")
        {
            return await _db.Channels
                .Where(c => c.IsActive)
                .Select(c => new ChannelAccessDto(
                    c.Id, c.Name, c.Slug, true, false, null
                ))
                .ToListAsync();
        }

        return await _db.ChannelAdmins
            .Where(ca => ca.UserId == userId)
            .Select(ca => new ChannelAccessDto(
                ca.Channel.Id,
                ca.Channel.Name,
                ca.Channel.Slug,
                true,
                false,
                null
            ))
            .ToListAsync();
    }

    public async Task<List<ChannelAccessDto>> GetSubscribedChannelsAsync(Guid userId)
    {
        return await _db.ChannelSubscriptions
            .Where(cs => cs.UserId == userId
                && cs.Status == SubscriptionStatus.Active
                && (cs.ExpiresAt == null || cs.ExpiresAt > DateTime.UtcNow))
            .Select(cs => new ChannelAccessDto(
                cs.Channel.Id,
                cs.Channel.Name,
                cs.Channel.Slug,
                false,
                true,
                cs.ExpiresAt
            ))
            .ToListAsync();
    }

    public async Task<bool> SubscribeToChannelAsync(Guid userId, Guid channelId, DateTime? expiresAt = null)
    {
        // Get default plan (lowest SortOrder)
        var defaultPlan = await _db.ChannelPlans
            .Where(p => p.ChannelId == channelId && p.IsActive)
            .OrderBy(p => p.SortOrder)
            .FirstOrDefaultAsync();

        if (defaultPlan == null)
            return false;

        return await SubscribeToChannelPlanAsync(userId, channelId, defaultPlan.Id, expiresAt);
    }

    public async Task<bool> SubscribeToChannelPlanAsync(Guid userId, Guid channelId, Guid planId, DateTime? expiresAt = null)
    {
        // Check if already subscribed
        var existing = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.ChannelId == channelId);

        if (existing != null)
        {
            // Update to new plan
            existing.ChannelPlanId = planId;
            existing.Status = SubscriptionStatus.Active;
            existing.ExpiresAt = expiresAt;
            existing.SubscribedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ChannelSubscriptions.Add(new ChannelSubscription
            {
                UserId = userId,
                ChannelId = channelId,
                ChannelPlanId = planId,
                SubscribedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Status = SubscriptionStatus.Active
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnsubscribeFromChannelAsync(Guid userId, Guid channelId)
    {
        var subscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.ChannelId == channelId);

        if (subscription == null)
            return false;

        subscription.Status = SubscriptionStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }
}
