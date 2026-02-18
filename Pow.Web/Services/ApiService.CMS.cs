using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Pow.Domain.DTOs;

namespace Pow.Web.Services;

// CMS API Methods - Extension for ApiService
public partial class ApiService
{
    // ===== PUBLIC CONTENT =====

    public async Task<List<ContentListDto>> GetPublishedContentsAsync(int take = 20, int skip = 0)
    {
        try
        {
            var response = await _http.GetAsync($"api/cms/content?take={take}&skip={skip}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ContentListDto>>() ?? new();

            Console.WriteLine($"GetPublishedContentsAsync failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetPublishedContentsAsync exception: {ex.Message}");
            return new();
        }
    }

    public async Task<ContentReaderDto?> GetContentForReaderAsync(string slug)
    {
        var response = await _http.GetAsync($"api/cms/content/{slug}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentReaderDto>();
        return null;
    }

    public async Task<List<ContentListDto>> GetContentsByChannelAsync(string channelSlug)
    {
        var response = await _http.GetAsync($"api/cms/content/channel/{channelSlug}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ContentListDto>>() ?? new();
        return new();
    }

    public async Task<List<ContentListDto>> GetContentsByCourseAsync(string courseSlug)
    {
        var response = await _http.GetAsync($"api/cms/content/course/{courseSlug}/contents");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ContentListDto>>() ?? new();
        return new();
    }

    public async Task<bool> ValidateAccessCodeAsync(Guid contentId, string code)
    {
        var response = await _http.PostAsJsonAsync($"api/cms/content/{contentId}/validate-code", new ValidateAccessCodeDto(contentId, code));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<bool>();
        return false;
    }

    public async Task TrackContentViewAsync(Guid contentId)
    {
        await _http.PostAsync($"api/cms/content/{contentId}/view", null);
    }

    public async Task<List<ChannelDto>> GetPublicChannelsAsync()
    {
        var response = await _http.GetAsync("api/cms/content/channels");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelDto>>() ?? new();
        return new();
    }

    public async Task<List<CourseDto>> GetPublicCoursesAsync(Guid channelId)
    {
        var response = await _http.GetAsync($"api/cms/content/channels/{channelId}/courses");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<CourseDto>>() ?? new();
        return new();
    }

    // ===== CREATOR ENDPOINTS =====

    public async Task<List<ChannelDto>> GetMyChannelsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/creator/my-channels");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelDto>>() ?? new();
        return new();
    }

    public async Task<bool> IsChannelAdminAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/channels/{channelId}/is-admin");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<bool>();
        return false;
    }

    // ===== CREATOR - CHANNEL PLANS =====

    public async Task<ChannelPlanDto?> CreateChannelPlanAsync(CreateChannelPlanDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/channel-plans", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelPlanDto>();
        return null;
    }

    public async Task<ChannelPlanDto?> UpdateChannelPlanAsync(Guid id, UpdateChannelPlanDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/channel-plans/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelPlanDto>();
        return null;
    }

    public async Task<bool> DeleteChannelPlanAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/channel-plans/{id}");
        return response.IsSuccessStatusCode;
    }

    // ===== USER - MY SUBSCRIPTIONS =====

    public async Task<List<UserSubscriptionDto>> GetMySubscriptionsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/user/my-subscriptions");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<UserSubscriptionDto>>() ?? new();
        return new();
    }

    public async Task<bool> CancelMySubscriptionAsync(Guid subscriptionId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/user/subscriptions/{subscriptionId}/cancel", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserPurchasedCourseDto>> GetMyPurchasedCoursesAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/user/my-courses");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<UserPurchasedCourseDto>>() ?? new();
        return new();
    }

    public async Task<bool> HasPurchasedCourseAsync(Guid courseId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/courses/{courseId}/purchased");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<bool>();
        return false;
    }

    // ===== CREATOR - SUBSCRIPTION MANAGEMENT =====

    public async Task<bool> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/subscriptions/{subscriptionId}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ExtendSubscriptionAsync(Guid subscriptionId, int days = 30)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/subscriptions/{subscriptionId}/extend?days={days}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CancelSubscriptionAdminAsync(Guid subscriptionId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/subscriptions/{subscriptionId}/cancel", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId, int days = 30)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/subscriptions/{subscriptionId}/reactivate?days={days}", null);
        return response.IsSuccessStatusCode;
    }

    // ===== CREATOR - SUBSCRIBERS =====

    public async Task<List<ChannelSubscriberDto>> GetChannelSubscribersAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/channels/{channelId}/subscribers");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelSubscriberDto>>() ?? new();
        return new();
    }

    public async Task<List<CourseDto>> GetMyCoursesAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/creator/my-courses");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<CourseDto>>() ?? new();
        return new();
    }

    public async Task<CourseDto?> GetCourseAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/courses/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CourseDto>();
        return null;
    }

    public async Task<CourseDto?> CreateCourseAsync(CreateCourseDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/courses", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CourseDto>();
        return null;
    }

    public async Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/courses/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CourseDto>();
        return null;
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/courses/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ChapterDto>> GetChaptersAsync(Guid courseId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/courses/{courseId}/chapters");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChapterDto>>() ?? new();
        return new();
    }

    public async Task<ChapterDto?> GetChapterAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/chapters/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChapterDto>();
        return null;
    }

    public async Task<ChapterDto?> CreateChapterAsync(CreateChapterDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/chapters", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChapterDto>();
        return null;
    }

    public async Task<ChapterDto?> UpdateChapterAsync(Guid id, UpdateChapterDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/chapters/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChapterDto>();
        return null;
    }

    public async Task<bool> DeleteChapterAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/chapters/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ContentListDto>> GetContentsAsync(Guid chapterId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/chapters/{chapterId}/contents");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ContentListDto>>() ?? new();
        return new();
    }

    public async Task<ContentDto?> GetContentAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/contents/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentDto>();
        return null;
    }

    public async Task<ContentDto?> CreateContentAsync(CreateContentDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/contents", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentDto>();
        return null;
    }

    public async Task<ContentDto?> UpdateContentAsync(Guid id, UpdateContentDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/contents/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentDto>();
        return null;
    }

    public async Task<bool> DeleteContentAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/contents/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PublishContentAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/contents/{id}/publish", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnpublishContentAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/contents/{id}/unpublish", null);
        return response.IsSuccessStatusCode;
    }

    // ===== CONTENT ATTACHMENTS =====

    public async Task<ContentAttachmentDto?> UploadContentAttachmentAsync(Guid contentId, IBrowserFile file)
    {
        await SetAuthHeaderAsync();

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024)); // 50MB max
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await _http.PostAsync($"api/cms/creator/contents/{contentId}/attachments", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentAttachmentDto>();
        return null;
    }

    public async Task<bool> DeleteContentAttachmentAsync(Guid attachmentId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/attachments/{attachmentId}");
        return response.IsSuccessStatusCode;
    }

    // ===== ADMIN ENDPOINTS =====

    public async Task<List<ChannelDto>> GetAllChannelsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/admin/channels");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelDto>>() ?? new();
        return new();
    }

    public async Task<ChannelDto?> GetChannelAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/admin/channels/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelDto>();
        return null;
    }

    public async Task<ChannelDto?> CreateChannelAsync(CreateChannelDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/channels", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelDto>();
        return null;
    }

    public async Task<ChannelDto?> UpdateMyChannelAsync(Guid channelId, UpdateChannelDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/channels/{channelId}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelDto>();
        return null;
    }

    public async Task<(bool Success, string? Error)> DeleteMyChannelAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/channels/{channelId}");
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await response.Content.ReadAsStringAsync();
        return (false, error);
    }

    public async Task<ChannelDto?> UpdateChannelAsync(Guid id, UpdateChannelDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/admin/channels/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelDto>();
        return null;
    }

    public async Task<bool> DeleteChannelAsync(Guid id)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/admin/channels/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ChannelAdminDto>> GetChannelAdminsAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/admin/channels/{channelId}/admins");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelAdminDto>>() ?? new();
        return new();
    }

    public async Task<bool> AssignChannelAdminAsync(Guid channelId, Guid userId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/admin/channels/{channelId}/admins/{userId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveChannelAdminAsync(Guid channelId, Guid userId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/admin/channels/{channelId}/admins/{userId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CourseDto>> GetAllCoursesAsync(Guid? channelId = null)
    {
        await SetAuthHeaderAsync();
        var url = channelId.HasValue
            ? $"api/cms/admin/courses?channelId={channelId}"
            : "api/cms/admin/courses";
        var response = await _http.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<CourseDto>>() ?? new();
        return new();
    }

    public async Task<List<ContentListDto>> GetAllContentsAsync(Guid? chapterId = null)
    {
        await SetAuthHeaderAsync();
        var url = chapterId.HasValue
            ? $"api/cms/admin/contents?chapterId={chapterId}"
            : "api/cms/admin/contents";
        var response = await _http.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ContentListDto>>() ?? new();
        return new();
    }

    public async Task<CMSStatsDto?> GetCMSStatsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/admin/stats");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CMSStatsDto>();
        return null;
    }

    // ===== CHANNEL PLANS =====

    public async Task<ChannelDto?> GetChannelBySlugAsync(string slug)
    {
        var response = await _http.GetAsync($"api/cms/content/channels/{slug}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelDto>();
        return null;
    }

    public async Task<CourseDto?> GetCourseBySlugAsync(string slug)
    {
        var response = await _http.GetAsync($"api/cms/content/courses/{slug}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CourseDto>();
        return null;
    }

    public async Task<List<ChannelPlanDto>> GetChannelPlansAsync(Guid channelId)
    {
        var response = await _http.GetAsync($"api/cms/content/channels/{channelId}/plans");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelPlanDto>>() ?? new();
        return new();
    }

    // ===== REGISTRATION WITH SUBSCRIPTIONS =====

    public async Task<AuthResponse> RegisterCreatorAsync(string email, string password, string channelName, Guid? platformPlanId)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register-creator", new
        {
            Email = email,
            Password = password,
            ChannelName = channelName,
            PlatformPlanId = platformPlanId
        });
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result ?? new AuthResponse(false, Message: "Unknown error");
    }

    public async Task<AuthResponse> RegisterWithChannelAsync(string email, string password, Guid channelId, Guid? channelPlanId, string? phoneNumber = null)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register-subscriber", new
        {
            Email = email,
            Password = password,
            ChannelId = channelId,
            ChannelPlanId = channelPlanId,
            PhoneNumber = phoneNumber
        });
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result ?? new AuthResponse(false, Message: "Unknown error");
    }

    // ===== FILE UPLOAD =====
    public async Task<string?> UploadImageAsync(IBrowserFile file)
    {
        await SetAuthHeaderAsync();

        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024)); // 5MB max
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await _http.PostAsync("api/cms/creator/upload-image", content);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return result.Trim('"'); // Remove quotes from JSON string
        }

        return null;
    }

    public async Task<string?> UploadVideoAsync(IBrowserFile file, Action<long, long>? onProgress = null)
    {
        await SetAuthHeaderAsync();

        var content = new MultipartFormDataContent();
        var maxSize = 500L * 1024 * 1024; // 500MB max

        using var stream = file.OpenReadStream(maxAllowedSize: maxSize);
        using var memoryStream = new MemoryStream();

        // Copy with progress tracking
        var buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
            onProgress?.Invoke(totalBytesRead, file.Size);
        }

        memoryStream.Position = 0;
        var fileContent = new StreamContent(memoryStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await _http.PostAsync("api/cms/creator/upload-video", content);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return result.Trim('"');
        }

        return null;
    }

    public async Task<bool> DeleteVideoAsync(string videoUrl)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/videos?url={Uri.EscapeDataString(videoUrl)}");
        return response.IsSuccessStatusCode;
    }

    // ===== CHANNEL SETTINGS =====

    public async Task<ChannelDto?> UpdateChannelSettingsAsync(Guid channelId, UpdateChannelSettingsDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/channels/{channelId}/settings", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelDto>();
        return null;
    }

    // ===== CHANNEL INBOX (CREATOR) =====

    public async Task<List<InboxMessageDto>> GetAllInboxMessagesAsync(int limit = 20)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/inbox/messages?limit={limit}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<InboxMessageDto>>() ?? new();
        return new();
    }

    public async Task<int> GetTotalUnreadCountAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/creator/inbox/unread-count");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<int>();
        return 0;
    }

    public async Task<List<ChannelMessageDto>> GetChannelMessagesAsync(Guid channelId, bool unreadOnly = false)
    {
        await SetAuthHeaderAsync();
        var url = $"api/cms/creator/channels/{channelId}/messages";
        if (unreadOnly)
            url += "?unreadOnly=true";
        var response = await _http.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelMessageDto>>() ?? new();
        return new();
    }

    public async Task<int> GetUnreadMessageCountAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/channels/{channelId}/messages/unread-count");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<int>();
        return 0;
    }

    public async Task<bool> MarkMessageAsReadAsync(Guid messageId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/messages/{messageId}/read", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkAllMessagesAsReadAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/creator/channels/{channelId}/messages/mark-all-read", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/messages/{messageId}");
        return response.IsSuccessStatusCode;
    }

    // ===== CHANNEL INBOX (PUBLIC) =====

    public async Task<bool> SendChannelMessageAsync(SendChannelMessageDto dto)
    {
        await SetAuthHeaderAsync(); // Optional - sends auth if available
        var response = await _http.PostAsJsonAsync($"api/cms/content/channels/{dto.ChannelId}/messages", dto);
        return response.IsSuccessStatusCode;
    }

    // ===== CHANNEL EVENTS (CREATOR) =====

    public async Task<List<ChannelEventDto>> GetChannelEventsCreatorAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/channels/{channelId}/events");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelEventDto>>() ?? new();
        return new();
    }

    public async Task<ChannelEventDto?> CreateEventAsync(CreateChannelEventDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/events", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelEventDto>();
        return null;
    }

    public async Task<ChannelEventDto?> UpdateEventAsync(Guid eventId, UpdateChannelEventDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/events/{eventId}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChannelEventDto>();
        return null;
    }

    public async Task<bool> DeleteEventAsync(Guid eventId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/events/{eventId}");
        return response.IsSuccessStatusCode;
    }

    // ===== CHANNEL EVENTS (PUBLIC) =====

    public async Task<List<ChannelEventDto>> GetChannelEventsAsync(Guid channelId)
    {
        var response = await _http.GetAsync($"api/cms/content/channels/{channelId}/events");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelEventDto>>() ?? new();
        return new();
    }

    // ===== CHANNEL FOLLOWS =====

    public async Task<List<ChannelFollowDto>> GetMyFollowsAsync()
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/cms/user/my-follows");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChannelFollowDto>>() ?? new();
        return new();
    }

    public async Task<bool> IsFollowingChannelAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/channels/{channelId}/following");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<bool>();
        return false;
    }

    public async Task<bool> FollowChannelAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/user/channels/{channelId}/follow", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnfollowChannelAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/user/channels/{channelId}/follow");
        return response.IsSuccessStatusCode;
    }

    // ===== CONTENT READ =====

    public async Task<ContentReadStatusDto?> ToggleContentReadAsync(Guid contentId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/cms/user/contents/{contentId}/toggle-read", null);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentReadStatusDto>();
        return null;
    }

    public async Task<ContentReadStatusDto?> GetContentReadStatusAsync(Guid contentId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/contents/{contentId}/read-status");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContentReadStatusDto>();
        return null;
    }

    public async Task<CourseProgressDto?> GetCourseProgressAsync(string courseSlug)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/courses/{courseSlug}/progress");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CourseProgressDto>();
        return null;
    }

    // ===== FEED =====

    public async Task<List<FeedItemDto>> GetFeedAsync(int page = 1, int pageSize = 20)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/feed?page={page}&pageSize={pageSize}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<FeedItemDto>>() ?? new();
        return new();
    }
}
