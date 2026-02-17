using Pow.Domain.DTOs;

namespace Pow.Domain.Interfaces;

/// <summary>
/// Service to check user access to channels, courses, and content.
/// Centralized access control for the application.
/// </summary>
public interface IUserAccessService
{
    /// <summary>
    /// Get a summary of all channels the user can access (as admin or subscriber)
    /// </summary>
    Task<UserAccessSummaryDto> GetUserAccessSummaryAsync(Guid userId);

    /// <summary>
    /// Check if user is admin of a specific channel
    /// </summary>
    Task<bool> IsChannelAdminAsync(Guid userId, Guid channelId);

    /// <summary>
    /// Check if user is subscribed to a specific channel (active subscription)
    /// </summary>
    Task<bool> IsChannelSubscriberAsync(Guid userId, Guid channelId);

    /// <summary>
    /// Check if user can access a specific channel's premium content
    /// (is admin OR has active subscription)
    /// </summary>
    Task<bool> CanAccessChannelContentAsync(Guid userId, Guid channelId);

    /// <summary>
    /// Check if user can access a specific course
    /// </summary>
    Task<CourseAccessResultDto> CanAccessCourseAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Check if user can access specific content.
    /// userId can be null for anonymous users (only IsPubliclyAccessible content allowed).
    /// </summary>
    Task<ContentAccessResultDto> CanAccessContentAsync(Guid? userId, Guid contentId);

    /// <summary>
    /// Get all channels where user is admin
    /// </summary>
    Task<List<ChannelAccessDto>> GetAdminChannelsAsync(Guid userId);

    /// <summary>
    /// Get all channels where user has active subscription
    /// </summary>
    Task<List<ChannelAccessDto>> GetSubscribedChannelsAsync(Guid userId);

    /// <summary>
    /// Subscribe user to a channel
    /// </summary>
    Task<bool> SubscribeToChannelAsync(Guid userId, Guid channelId, DateTime? expiresAt = null);

    /// <summary>
    /// Unsubscribe user from a channel
    /// </summary>
    Task<bool> UnsubscribeFromChannelAsync(Guid userId, Guid channelId);
}
