using Pow.Domain.Enums;

namespace Pow.Domain.DTOs;

/// <summary>
/// Summary of what a user can access
/// </summary>
public record UserAccessSummaryDto(
    List<ChannelAccessDto> AdminChannels,
    List<ChannelAccessDto> SubscribedChannels,
    bool IsSuperAdmin
);

/// <summary>
/// Channel access information
/// </summary>
public record ChannelAccessDto(
    Guid ChannelId,
    string ChannelName,
    string ChannelSlug,
    bool IsAdmin,
    bool IsSubscribed,
    DateTime? SubscriptionExpires
);

/// <summary>
/// Result of checking access to specific content
/// </summary>
public record ContentAccessResultDto(
    bool HasAccess,
    string? DenialReason,
    Guid? RequiredChannelId,
    string? RequiredChannelName
);

/// <summary>
/// Result of checking access to a course
/// </summary>
public record CourseAccessResultDto(
    bool HasAccess,
    string? DenialReason,
    CourseAccessType AccessType,
    Guid ChannelId,
    string ChannelName
);
