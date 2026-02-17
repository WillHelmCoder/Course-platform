using Pow.Domain.DTOs;

namespace Pow.Api.Services;

public interface ICMSService
{
    // ===== CHANNELS =====
    Task<List<ChannelDto>> GetChannelsAsync();
    Task<List<ChannelDto>> GetMyChannelsAsync(Guid userId);
    Task<ChannelDto?> GetChannelAsync(Guid id);
    Task<ChannelDto?> GetChannelBySlugAsync(string slug);
    Task<ChannelDto> CreateChannelAsync(CreateChannelDto dto, Guid tenantId);
    Task<ChannelDto?> UpdateChannelAsync(Guid id, UpdateChannelDto dto);
    Task<bool> DeleteChannelAsync(Guid id);

    // ===== CHANNEL PLANS =====
    Task<List<ChannelPlanDto>> GetChannelPlansAsync(Guid channelId);
    Task<ChannelPlanDto> CreateChannelPlanAsync(CreateChannelPlanDto dto, Guid tenantId);
    Task<ChannelPlanDto?> UpdateChannelPlanAsync(Guid id, UpdateChannelPlanDto dto);
    Task<bool> DeleteChannelPlanAsync(Guid id);

    // ===== CHANNEL SUBSCRIBERS =====
    Task<List<ChannelSubscriberDto>> GetChannelSubscribersAsync(Guid channelId);
    Task<ChannelSubscriberDto?> GetSubscriptionAsync(Guid subscriptionId);
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid userId);
    Task<ChannelSubscriberDto?> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionDto dto);
    Task<bool> ExtendSubscriptionAsync(Guid subscriptionId, int days);

    // ===== CHANNEL ADMINS =====
    Task<List<ChannelAdminDto>> GetChannelAdminsAsync(Guid channelId);
    Task<bool> AssignChannelAdminAsync(Guid channelId, Guid userId);
    Task<bool> RemoveChannelAdminAsync(Guid channelId, Guid userId);
    Task<bool> IsChannelAdminAsync(Guid channelId, Guid userId);

    // ===== COURSES =====
    Task<List<CourseDto>> GetCoursesAsync(Guid? channelId = null);
    Task<List<CourseDto>> GetMyCoursesAsync(Guid userId);
    Task<CourseDto?> GetCourseAsync(Guid id);
    Task<CourseDto?> GetCourseBySlugAsync(string slug);
    Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, Guid tenantId);
    Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto dto);
    Task<bool> DeleteCourseAsync(Guid id);

    // ===== CHAPTERS =====
    Task<List<ChapterDto>> GetChaptersAsync(Guid courseId);
    Task<ChapterDto?> GetChapterAsync(Guid id);
    Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto, Guid tenantId);
    Task<ChapterDto?> UpdateChapterAsync(Guid id, UpdateChapterDto dto);
    Task<bool> DeleteChapterAsync(Guid id);

    // ===== CONTENT =====
    Task<List<ContentListDto>> GetContentsAsync(Guid? chapterId = null);
    Task<ContentDto?> GetContentAsync(Guid id);
    Task<ContentDto?> GetContentBySlugAsync(string slug);
    Task<ContentDto> CreateContentAsync(CreateContentDto dto, Guid tenantId);
    Task<ContentDto?> UpdateContentAsync(Guid id, UpdateContentDto dto);
    Task<bool> DeleteContentAsync(Guid id);
    Task<bool> PublishContentAsync(Guid id);
    Task<bool> UnpublishContentAsync(Guid id);

    // ===== PUBLIC READER =====
    Task<ContentReaderDto?> GetContentForReaderAsync(string slug);
    Task<List<ContentListDto>> GetPublishedContentsAsync(int take = 20, int skip = 0);
    Task<List<ContentListDto>> GetPublishedContentsByChannelAsync(string channelSlug);
    Task<List<ContentListDto>> GetPublishedContentsByCourseAsync(string courseSlug);
    Task<bool> ValidateAccessCodeAsync(Guid contentId, string code);
    Task TrackViewAsync(Guid contentId, Guid? userId, string? ipAddress, string? userAgent);

    // ===== STATS =====
    Task<CMSStatsDto> GetStatsAsync();
}
