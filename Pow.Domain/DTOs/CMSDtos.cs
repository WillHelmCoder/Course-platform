using Pow.Domain.Enums;

namespace Pow.Domain.DTOs;

// ===== CHANNEL =====
public record ChannelDto(
    Guid Id,
    string Name,
    string? Description,
    string Slug,
    string? MainPicture,
    bool IsActive,
    int SortOrder,
    int CourseCount,
    int ContentCount = 0,
    InboxVisibility InboxVisibility = InboxVisibility.Disabled
);

public record CreateChannelDto(string Name, string? Description);
public record UpdateChannelDto(string Name, string? Description, bool IsActive, int SortOrder);

// ===== CHANNEL PLAN =====
public record ChannelPlanDto(
    Guid Id,
    Guid ChannelId,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Interval,
    int SortOrder,
    bool IsActive,
    int SubscriberCount
);

public record CreateChannelPlanDto(Guid ChannelId, string Name, string? Description, decimal Price, string Currency, string Interval, int SortOrder);
public record UpdateChannelPlanDto(string Name, string? Description, decimal Price, string Currency, string Interval, int SortOrder, bool IsActive);

// ===== CHANNEL SUBSCRIBER =====
public record ChannelSubscriberDto(
    Guid Id,
    Guid UserId,
    string Email,
    Guid PlanId,
    string PlanName,
    DateTime SubscribedAt,
    DateTime? ExpiresAt,
    SubscriptionStatus Status,
    decimal? PricePaid,
    string? Currency
);

// ===== USER SUBSCRIPTION (for user's own view) =====
public record UserSubscriptionDto(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    string ChannelSlug,
    Guid PlanId,
    string PlanName,
    DateTime SubscribedAt,
    DateTime? ExpiresAt,
    SubscriptionStatus Status,
    int CoursesCount
);

// ===== COURSE =====
public record CourseDto(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    string Title,
    string? Description,
    string? MainPicture,
    string Slug,
    bool IsActive,
    bool IsPublished,
    CourseAccessType AccessType,
    Guid? RequiredPlanId,
    string? RequiredPlanName,
    int SortOrder,
    int ChapterCount,
    int ContentCount,
    bool AllowPurchase = false,
    decimal Price = 0,
    string Currency = "USD"
);

public record CreateCourseDto(Guid ChannelId, string Title, string? Description, string? MainPicture, CourseAccessType AccessType = CourseAccessType.Free, Guid? RequiredPlanId = null, bool AllowPurchase = false, decimal Price = 0, string Currency = "USD");
public record UpdateCourseDto(string Title, string? Description, string? MainPicture, bool IsActive, bool IsPublished, CourseAccessType AccessType, Guid? RequiredPlanId, int SortOrder, bool AllowPurchase = false, decimal Price = 0, string Currency = "USD");

// ===== COURSE PURCHASE =====
public record CoursePurchaseDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string? CourseImage,
    string CourseSlug,
    string ChannelName,
    string ChannelSlug,
    DateTime PurchasedAt,
    decimal PricePaid,
    string Currency
);

public record UserPurchasedCourseDto(
    Guid CourseId,
    string Title,
    string? Description,
    string? MainPicture,
    string Slug,
    string ChannelName,
    string ChannelSlug,
    int ChapterCount,
    DateTime PurchasedAt
);

// ===== CHAPTER =====
public record ChapterDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Description,
    int SortOrder,
    int ContentCount
);

public record CreateChapterDto(Guid CourseId, string Title, string? Description);
public record UpdateChapterDto(string Title, string? Description, int SortOrder);

// ===== CONTENT =====
public record ContentDto(
    Guid Id,
    Guid ChapterId,
    string ChapterTitle,
    string CourseTitle,
    string ChannelName,
    string Title,
    string? Description,
    string Body,
    string? MainPicture,
    string? VideoUrl,
    string Slug,
    ContentAccessLevel AccessLevel,
    bool HasAccessCode,
    bool IsPublished,
    int SortOrder,
    int ViewCount,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    List<ContentAttachmentDto> Attachments
);

public record ContentListDto(
    Guid Id,
    string Title,
    string? Description,
    string? MainPicture,
    string Slug,
    ContentAccessLevel AccessLevel,
    int ViewCount,
    DateTime? PublishedAt,
    Guid? AuthorId = null,
    string? AuthorName = null,
    string? AuthorProfilePicture = null,
    Guid? ChannelId = null,
    string? ChannelName = null,
    string? ChannelSlug = null
);

public record ContentAttachmentDto(
    Guid Id,
    string FileName,
    string FilePath,
    string ContentType,
    long FileSize,
    int SortOrder
);

public record CreateContentDto(
    Guid ChapterId,
    string Title,
    string? Description,
    string Body,
    string? MainPicture,
    string? VideoUrl,
    ContentAccessLevel AccessLevel,
    string? AccessCode
);

public record UpdateContentDto(
    string Title,
    string? Description,
    string Body,
    string? MainPicture,
    string? VideoUrl,
    ContentAccessLevel AccessLevel,
    string? AccessCode,
    bool IsPublished,
    int SortOrder
);

// ===== CHANNEL ADMIN =====
public record ChannelAdminDto(Guid ChannelId, string ChannelName, Guid UserId, string UserEmail);
public record AssignChannelAdminDto(Guid ChannelId, Guid UserId);

// ===== STATS =====
public record CMSStatsDto(
    int TotalChannels,
    int TotalCourses,
    int TotalChapters,
    int TotalContent,
    int PublishedContent,
    int TotalViews,
    Dictionary<string, int> ViewsByChannel
);

// ===== PUBLIC READER =====
public record ContentReaderDto(
    Guid Id,
    string Title,
    string? Description,
    string Body,
    string? MainPicture,
    string? VideoUrl,
    string Slug,
    string ChapterTitle,
    string CourseTitle,
    string ChannelName,
    ContentAccessLevel AccessLevel,
    bool RequiresCode,
    DateTime? PublishedAt,
    ContentReaderNavDto? Previous,
    ContentReaderNavDto? Next,
    List<ContentAttachmentDto> Attachments,
    Guid? AuthorId = null,
    string? AuthorName = null,
    string? AuthorProfilePicture = null
);

public record ContentReaderNavDto(Guid Id, string Title, string Slug);
public record ValidateAccessCodeDto(Guid ContentId, string Code);

// ===== SUBSCRIPTION MANAGEMENT =====
public record UpdateSubscriptionDto(
    SubscriptionStatus? Status,
    DateTime? ExpiresAt,
    Guid? NewPlanId
);

// ===== CHANNEL MESSAGES =====
public record ChannelMessageDto(
    Guid Id,
    Guid ChannelId,
    Guid? SenderId,
    string SenderName,
    string SenderEmail,
    string Subject,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public record InboxMessageDto(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    string SenderName,
    string Subject,
    string MessagePreview,
    bool IsRead,
    DateTime CreatedAt
);

public record SendChannelMessageDto(
    Guid ChannelId,
    string SenderName,
    string SenderEmail,
    string Subject,
    string Message
);

public record UpdateChannelSettingsDto(
    InboxVisibility? InboxVisibility
);

// ===== CHANNEL EVENTS =====
public record ChannelEventDto(
    Guid Id,
    Guid ChannelId,
    string Title,
    string? Description,
    DateTime EventDate,
    string? Location,
    string? ZoomLink,
    string? StreamingLink,
    bool IsActive
);

public record CreateChannelEventDto(
    Guid ChannelId,
    string Title,
    string? Description,
    DateTime EventDate,
    string? Location,
    string? ZoomLink,
    string? StreamingLink
);

public record UpdateChannelEventDto(
    string Title,
    string? Description,
    DateTime EventDate,
    string? Location,
    string? ZoomLink,
    string? StreamingLink,
    bool IsActive
);

// ===== AUTH - CHANGE PASSWORD =====
public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);

// ===== CHANNEL FOLLOWS =====
public record ChannelFollowDto(
    Guid ChannelId,
    string ChannelName,
    string ChannelSlug,
    string? ChannelPicture,
    DateTime FollowedAt
);

// ===== FEED =====
public record FeedItemDto(
    Guid ContentId,
    string ContentTitle,
    string? ContentDescription,
    string? ContentPicture,
    string ContentSlug,
    ContentAccessLevel AccessLevel,
    DateTime? PublishedAt,
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    Guid ChannelId,
    string ChannelName,
    string ChannelSlug,
    string? ChannelPicture,
    Guid? AuthorId,
    string? AuthorName,
    string? AuthorProfilePicture
);

// ===== CONTENT READ =====
public record ContentReadStatusDto(
    Guid ContentId,
    bool IsRead,
    DateTime? ReadAt
);

public record CourseProgressDto(
    Guid CourseId,
    List<ChapterProgressDto> Chapters
);

public record ChapterProgressDto(
    Guid ChapterId,
    string Title,
    string? Description,
    int SortOrder,
    int TotalContents,
    int ReadContents,
    double ProgressPercent,
    List<ContentProgressDto> Contents
);

public record ContentProgressDto(
    Guid Id,
    string Title,
    string? Description,
    string? MainPicture,
    string Slug,
    bool IsRead
);

// ===== VIDEO =====
public record VideoUploadResponse(
    string VideoUrl,
    string Provider,
    string? PlaybackId
);
