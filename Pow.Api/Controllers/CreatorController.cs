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
/// Creator controller - content management for channel admins.
/// </summary>
[ApiController]
[Route("api/cms/creator")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class CreatorController : ControllerBase
{
    private readonly ICMSService _cmsService;
    private readonly AppDbContext _db;
    private readonly VideoServiceFactory _videoServiceFactory;

    public CreatorController(ICMSService cmsService, AppDbContext db, VideoServiceFactory videoServiceFactory)
    {
        _cmsService = cmsService;
        _db = db;
        _videoServiceFactory = videoServiceFactory;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst("sub")?.Value ??
                   User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                   Guid.Empty.ToString());

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

    // ===== MY CHANNELS =====

    /// <summary>
    /// Get channels where user is admin.
    /// </summary>
    [HttpGet("my-channels")]
    public async Task<ActionResult<List<ChannelDto>>> GetMyChannels()
    {
        var channels = await _cmsService.GetMyChannelsAsync(GetUserId());
        return Ok(channels);
    }

    /// <summary>
    /// Create a new channel and assign current user as admin.
    /// </summary>
    [HttpPost("channels")]
    public async Task<ActionResult<ChannelDto>> CreateChannel([FromBody] CreateChannelDto dto)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();

        // If tenantId is empty, use the user's tenantId
        if (tenantId == Guid.Empty)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
                tenantId = user.TenantId;
        }

        if (tenantId == Guid.Empty)
            return BadRequest("Invalid or missing tenant information");

        try
        {
            // Create the channel
            var channel = await _cmsService.CreateChannelAsync(dto, tenantId);

            // Assign the creating user as channel admin
            await _cmsService.AssignChannelAdminAsync(channel.Id, userId);

            return CreatedAtAction(nameof(GetMyChannels), channel);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating channel: {ex.Message}");
        }
    }

    /// <summary>
    /// Update a channel (name, description, image, active status).
    /// </summary>
    [HttpPut("channels/{channelId:guid}")]
    public async Task<ActionResult<ChannelDto>> UpdateChannel(Guid channelId, [FromBody] UpdateChannelDto dto)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var channel = await _db.Channels.FindAsync(channelId);
        if (channel == null)
            return NotFound();

        channel.Name = dto.Name;
        channel.Description = dto.Description;
        channel.IsActive = dto.IsActive;
        channel.SortOrder = dto.SortOrder;
        channel.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var courseCount = await _db.Courses.CountAsync(c => c.ChannelId == channelId);
        var contentCount = await _db.Contents.CountAsync(c => c.Chapter.Course.ChannelId == channelId);

        return Ok(new ChannelDto(
            channel.Id, channel.Name, channel.Description, channel.Slug,
            channel.MainPicture, channel.IsActive, channel.SortOrder,
            courseCount, contentCount, channel.InboxVisibility
        ));
    }

    /// <summary>
    /// Delete a channel and all its content.
    /// </summary>
    [HttpDelete("channels/{channelId:guid}")]
    public async Task<ActionResult> DeleteChannel(Guid channelId)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var channel = await _db.Channels
            .Include(c => c.Courses)
                .ThenInclude(co => co.Chapters)
                    .ThenInclude(ch => ch.Contents)
            .Include(c => c.Plans)
            .Include(c => c.Subscriptions)
            .Include(c => c.ChannelAdmins)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == channelId);

        if (channel == null)
            return NotFound();

        // Check if channel has active subscriptions
        var hasActiveSubscriptions = channel.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active);
        if (hasActiveSubscriptions)
            return BadRequest("Cannot delete channel with active subscriptions. Cancel all subscriptions first.");

        _db.Channels.Remove(channel);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// DEBUG: Get current user info and tenant details.
    /// </summary>
    [HttpGet("debug-info")]
    public async Task<ActionResult> GetDebugInfo()
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();

        var user = await _db.Users.FindAsync(userId);
        var channels = await _db.Channels.Where(c => c.TenantId == tenantId).ToListAsync();
        var channelAdmins = await _db.ChannelAdmins.Where(ca => ca.UserId == userId).ToListAsync();

        return Ok(new {
            UserId = userId,
            TenantId = tenantId,
            User = user?.Email,
            UserTenantId = user?.TenantId,
            ChannelsInTenant = channels.Select(c => new { c.Id, c.Name, c.TenantId }).ToList(),
            MyChannelAdmins = channelAdmins.Select(ca => new { ca.ChannelId, ca.UserId, ca.TenantId }).ToList()
        });
    }

    /// <summary>
    /// Check if user is admin of a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/is-admin")]
    public async Task<ActionResult<bool>> IsChannelAdmin(Guid channelId)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        return Ok(isAdmin);
    }

    // ===== CHANNEL PLANS =====

    /// <summary>
    /// Create a new channel plan.
    /// </summary>
    [HttpPost("channel-plans")]
    public async Task<ActionResult<ChannelPlanDto>> CreateChannelPlan([FromBody] CreateChannelPlanDto dto)
    {
        // Verify user is admin of the channel
        var isAdmin = await _cmsService.IsChannelAdminAsync(dto.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var plan = await _cmsService.CreateChannelPlanAsync(dto, GetTenantId());
        return Ok(plan);
    }

    /// <summary>
    /// Update a channel plan.
    /// </summary>
    [HttpPut("channel-plans/{id:guid}")]
    public async Task<ActionResult<ChannelPlanDto>> UpdateChannelPlan(Guid id, [FromBody] UpdateChannelPlanDto dto)
    {
        var plan = await _cmsService.UpdateChannelPlanAsync(id, dto);
        if (plan == null)
            return NotFound();

        return Ok(plan);
    }

    /// <summary>
    /// Delete a channel plan.
    /// </summary>
    [HttpDelete("channel-plans/{id:guid}")]
    public async Task<ActionResult> DeleteChannelPlan(Guid id)
    {
        var deleted = await _cmsService.DeleteChannelPlanAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ===== CHANNEL SUBSCRIBERS =====

    /// <summary>
    /// Get subscribers for a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/subscribers")]
    public async Task<ActionResult<List<ChannelSubscriberDto>>> GetChannelSubscribers(Guid channelId)
    {
        // Verify user is admin of the channel
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var subscribers = await _cmsService.GetChannelSubscribersAsync(channelId);
        return Ok(subscribers);
    }

    /// <summary>
    /// Update a subscription (status, expiry, plan).
    /// </summary>
    [HttpPut("subscriptions/{subscriptionId:guid}")]
    public async Task<ActionResult<ChannelSubscriberDto>> UpdateSubscription(Guid subscriptionId, [FromBody] UpdateSubscriptionDto dto)
    {
        var subscription = await _cmsService.GetSubscriptionAsync(subscriptionId);
        if (subscription == null)
            return NotFound();

        // Get channel from subscription to verify admin rights
        var channelSub = await _db.ChannelSubscriptions.FindAsync(subscriptionId);
        if (channelSub == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(channelSub.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var updated = await _cmsService.UpdateSubscriptionAsync(subscriptionId, dto);
        if (updated == null)
            return NotFound();

        return Ok(updated);
    }

    /// <summary>
    /// Extend a subscription by a number of days.
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/extend")]
    public async Task<ActionResult> ExtendSubscription(Guid subscriptionId, [FromQuery] int days = 30)
    {
        var channelSub = await _db.ChannelSubscriptions.FindAsync(subscriptionId);
        if (channelSub == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(channelSub.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var extended = await _cmsService.ExtendSubscriptionAsync(subscriptionId, days);
        if (!extended)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Cancel a subscription (admin).
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/cancel")]
    public async Task<ActionResult> CancelSubscriptionAdmin(Guid subscriptionId)
    {
        var channelSub = await _db.ChannelSubscriptions.FindAsync(subscriptionId);
        if (channelSub == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(channelSub.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var dto = new UpdateSubscriptionDto(Domain.Enums.SubscriptionStatus.Cancelled, null, null);
        var updated = await _cmsService.UpdateSubscriptionAsync(subscriptionId, dto);
        if (updated == null)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Reactivate a cancelled/expired subscription.
    /// </summary>
    [HttpPost("subscriptions/{subscriptionId:guid}/reactivate")]
    public async Task<ActionResult> ReactivateSubscription(Guid subscriptionId, [FromQuery] int days = 30)
    {
        var channelSub = await _db.ChannelSubscriptions.FindAsync(subscriptionId);
        if (channelSub == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(channelSub.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var extended = await _cmsService.ExtendSubscriptionAsync(subscriptionId, days);
        if (!extended)
            return NotFound();

        return Ok();
    }

    // ===== MY COURSES =====

    /// <summary>
    /// Get courses from user's channels.
    /// </summary>
    [HttpGet("my-courses")]
    public async Task<ActionResult<List<CourseDto>>> GetMyCourses()
    {
        var courses = await _cmsService.GetMyCoursesAsync(GetUserId());
        return Ok(courses);
    }

    /// <summary>
    /// Get course by ID.
    /// </summary>
    [HttpGet("courses/{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetCourse(Guid id)
    {
        var course = await _cmsService.GetCourseAsync(id);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    /// <summary>
    /// Create a new course.
    /// </summary>
    [HttpPost("courses")]
    public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();

        // Validate TenantId
        if (tenantId == Guid.Empty)
            return BadRequest("Invalid or missing tenant information");

        // Verify user is admin of the channel
        var isAdmin = await _cmsService.IsChannelAdminAsync(dto.ChannelId, userId);
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        try
        {
            var course = await _cmsService.CreateCourseAsync(dto, tenantId);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update a course.
    /// </summary>
    [HttpPut("courses/{id:guid}")]
    public async Task<ActionResult<CourseDto>> UpdateCourse(Guid id, [FromBody] UpdateCourseDto dto)
    {
        var course = await _cmsService.UpdateCourseAsync(id, dto);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    /// <summary>
    /// Delete a course.
    /// </summary>
    [HttpDelete("courses/{id:guid}")]
    public async Task<ActionResult> DeleteCourse(Guid id)
    {
        var deleted = await _cmsService.DeleteCourseAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ===== CHAPTERS =====

    /// <summary>
    /// Get chapters for a course.
    /// </summary>
    [HttpGet("courses/{courseId:guid}/chapters")]
    public async Task<ActionResult<List<ChapterDto>>> GetChapters(Guid courseId)
    {
        var chapters = await _cmsService.GetChaptersAsync(courseId);
        return Ok(chapters);
    }

    /// <summary>
    /// Get chapter by ID.
    /// </summary>
    [HttpGet("chapters/{id:guid}")]
    public async Task<ActionResult<ChapterDto>> GetChapter(Guid id)
    {
        var chapter = await _cmsService.GetChapterAsync(id);
        if (chapter == null)
            return NotFound();

        return Ok(chapter);
    }

    /// <summary>
    /// Create a new chapter.
    /// </summary>
    [HttpPost("chapters")]
    public async Task<ActionResult<ChapterDto>> CreateChapter([FromBody] CreateChapterDto dto)
    {
        var chapter = await _cmsService.CreateChapterAsync(dto, GetTenantId());
        return CreatedAtAction(nameof(GetChapter), new { id = chapter.Id }, chapter);
    }

    /// <summary>
    /// Update a chapter.
    /// </summary>
    [HttpPut("chapters/{id:guid}")]
    public async Task<ActionResult<ChapterDto>> UpdateChapter(Guid id, [FromBody] UpdateChapterDto dto)
    {
        var chapter = await _cmsService.UpdateChapterAsync(id, dto);
        if (chapter == null)
            return NotFound();

        return Ok(chapter);
    }

    /// <summary>
    /// Delete a chapter.
    /// </summary>
    [HttpDelete("chapters/{id:guid}")]
    public async Task<ActionResult> DeleteChapter(Guid id)
    {
        var deleted = await _cmsService.DeleteChapterAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ===== CONTENT =====

    /// <summary>
    /// Get contents for a chapter.
    /// </summary>
    [HttpGet("chapters/{chapterId:guid}/contents")]
    public async Task<ActionResult<List<ContentListDto>>> GetContents(Guid chapterId)
    {
        var contents = await _cmsService.GetContentsAsync(chapterId);
        return Ok(contents);
    }

    /// <summary>
    /// Get content by ID.
    /// </summary>
    [HttpGet("contents/{id:guid}")]
    public async Task<ActionResult<ContentDto>> GetContent(Guid id)
    {
        var content = await _cmsService.GetContentAsync(id);
        if (content == null)
            return NotFound();

        return Ok(content);
    }

    /// <summary>
    /// Create new content.
    /// </summary>
    [HttpPost("contents")]
    public async Task<ActionResult<ContentDto>> CreateContent([FromBody] CreateContentDto dto)
    {
        var content = await _cmsService.CreateContentAsync(dto, GetTenantId());
        return CreatedAtAction(nameof(GetContent), new { id = content.Id }, content);
    }

    /// <summary>
    /// Update content.
    /// </summary>
    [HttpPut("contents/{id:guid}")]
    public async Task<ActionResult<ContentDto>> UpdateContent(Guid id, [FromBody] UpdateContentDto dto)
    {
        var content = await _cmsService.UpdateContentAsync(id, dto);
        if (content == null)
            return NotFound();

        return Ok(content);
    }

    /// <summary>
    /// Delete content.
    /// </summary>
    [HttpDelete("contents/{id:guid}")]
    public async Task<ActionResult> DeleteContent(Guid id)
    {
        var deleted = await _cmsService.DeleteContentAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Publish content.
    /// </summary>
    [HttpPost("contents/{id:guid}/publish")]
    public async Task<ActionResult> PublishContent(Guid id)
    {
        var published = await _cmsService.PublishContentAsync(id);
        if (!published)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Unpublish content.
    /// </summary>
    [HttpPost("contents/{id:guid}/unpublish")]
    public async Task<ActionResult> UnpublishContent(Guid id)
    {
        var unpublished = await _cmsService.UnpublishContentAsync(id);
        if (!unpublished)
            return NotFound();

        return Ok();
    }

    // ===== FILE UPLOAD =====

    /// <summary>
    /// Upload an image file and return the URL.
    /// </summary>
    [HttpPost("upload-image")]
    public async Task<ActionResult<string>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Only image files are allowed (JPEG, PNG, GIF, WebP)");

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("File size must be less than 5MB");

        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
            Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the URL
            var imageUrl = $"/uploads/images/{fileName}";
            return Ok(imageUrl);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload a video file and return the URL.
    /// Uses the configured video provider (MUX or Vimeo).
    /// </summary>
    [HttpPost("upload-video")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
    public async Task<ActionResult<VideoUploadResponse>> UploadVideo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Validate file type
        var allowedTypes = new[] { "video/mp4", "video/webm", "video/ogg", "video/quicktime", "video/x-msvideo", "video/x-matroska" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest("Only video files are allowed (MP4, WebM, OGG, MOV, AVI, MKV)");

        // Validate file size (max 500MB)
        if (file.Length > 500 * 1024 * 1024)
            return BadRequest("File size must be less than 500MB");

        try
        {
            var videoService = await _videoServiceFactory.GetVideoServiceAsync();

            if (videoService == null)
                return BadRequest("Video provider not configured. Please configure MUX or Vimeo in Site Settings.");

            using var stream = file.OpenReadStream();
            var result = await videoService.UploadVideoAsync(stream, file.FileName, file.ContentType);

            if (!result.Success)
                return StatusCode(500, $"Error uploading video: {result.Error}");

            return Ok(new VideoUploadResponse(result.VideoUrl!, videoService.ProviderName, result.PlaybackId));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading video: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a video file.
    /// Uses the appropriate video service based on the URL format.
    /// </summary>
    [HttpDelete("videos")]
    public async Task<ActionResult> DeleteVideo([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
            return BadRequest("No URL provided");

        try
        {
            var videoService = await _videoServiceFactory.GetVideoServiceAsync();

            if (videoService == null)
                return BadRequest("Video provider not configured");

            // Validate URL format matches a known provider
            if (!url.StartsWith("mux:") && !url.StartsWith("vimeo:"))
                return BadRequest("Invalid video URL format");

            var deleted = await videoService.DeleteVideoAsync(url);
            return deleted ? Ok() : NotFound("Video not found");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting video: {ex.Message}");
        }
    }

    // ===== CONTENT ATTACHMENTS =====

    /// <summary>
    /// Upload an attachment to content.
    /// </summary>
    [HttpPost("contents/{contentId:guid}/attachments")]
    public async Task<ActionResult<ContentAttachmentDto>> UploadAttachment(Guid contentId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Validate file size (max 50MB)
        if (file.Length > 50 * 1024 * 1024)
            return BadRequest("File size must be less than 50MB");

        // Check content exists
        var content = await _db.Contents.FindAsync(contentId);
        if (content == null)
            return NotFound("Content not found");

        try
        {
            // Create uploads directory if it doesn't exist
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "attachments");
            Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create attachment record
            var attachment = new ContentAttachment
            {
                ContentId = contentId,
                FileName = file.FileName,
                FilePath = $"/uploads/attachments/{fileName}",
                ContentType = file.ContentType,
                FileSize = file.Length,
                SortOrder = await _db.ContentAttachments.Where(a => a.ContentId == contentId).CountAsync(),
                TenantId = content.TenantId
            };

            _db.ContentAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            return Ok(new ContentAttachmentDto(
                attachment.Id,
                attachment.FileName,
                attachment.FilePath,
                attachment.ContentType,
                attachment.FileSize,
                attachment.SortOrder
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete an attachment.
    /// </summary>
    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<ActionResult> DeleteAttachment(Guid attachmentId)
    {
        var attachment = await _db.ContentAttachments.FindAsync(attachmentId);
        if (attachment == null)
            return NotFound();

        try
        {
            // Delete physical file
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _db.ContentAttachments.Remove(attachment);
            await _db.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting file: {ex.Message}");
        }
    }

    // ===== CHANNEL SETTINGS =====

    /// <summary>
    /// Update channel settings (inbox visibility, etc.)
    /// </summary>
    [HttpPut("channels/{channelId:guid}/settings")]
    public async Task<ActionResult<ChannelDto>> UpdateChannelSettings(Guid channelId, [FromBody] UpdateChannelSettingsDto dto)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var channel = await _db.Channels.FindAsync(channelId);
        if (channel == null)
            return NotFound();

        if (dto.InboxVisibility.HasValue)
            channel.InboxVisibility = dto.InboxVisibility.Value;

        channel.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new ChannelDto(
            channel.Id, channel.Name, channel.Description, channel.Slug,
            channel.MainPicture, channel.IsActive, channel.SortOrder,
            await _db.Courses.CountAsync(c => c.ChannelId == channelId),
            0, channel.InboxVisibility
        ));
    }

    // ===== CHANNEL INBOX =====

    /// <summary>
    /// Get all inbox messages across all channels for the current user.
    /// </summary>
    [HttpGet("inbox/messages")]
    public async Task<ActionResult<List<InboxMessageDto>>> GetAllInboxMessages([FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        var userChannelIds = await _db.ChannelAdmins
            .Where(ca => ca.UserId == userId)
            .Select(ca => ca.ChannelId)
            .ToListAsync();

        var messages = await _db.ChannelMessages
            .Where(m => userChannelIds.Contains(m.ChannelId))
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new InboxMessageDto(
                m.Id,
                m.ChannelId,
                m.Channel.Name,
                m.SenderName,
                m.Subject,
                m.Message.Length > 60 ? m.Message.Substring(0, 60) + "..." : m.Message,
                m.IsRead,
                m.CreatedAt
            ))
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>
    /// Get total unread message count across all channels for the current user.
    /// </summary>
    [HttpGet("inbox/unread-count")]
    public async Task<ActionResult<int>> GetTotalUnreadCount()
    {
        var userId = GetUserId();
        var userChannelIds = await _db.ChannelAdmins
            .Where(ca => ca.UserId == userId)
            .Select(ca => ca.ChannelId)
            .ToListAsync();

        var count = await _db.ChannelMessages
            .CountAsync(m => userChannelIds.Contains(m.ChannelId) && !m.IsRead);

        return Ok(count);
    }

    /// <summary>
    /// Get messages for a channel (creator inbox).
    /// </summary>
    [HttpGet("channels/{channelId:guid}/messages")]
    public async Task<ActionResult<List<ChannelMessageDto>>> GetChannelMessages(Guid channelId, [FromQuery] bool unreadOnly = false)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var query = _db.ChannelMessages
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.CreatedAt);

        if (unreadOnly)
            query = (IOrderedQueryable<ChannelMessage>)query.Where(m => !m.IsRead);

        var messages = await query
            .Select(m => new ChannelMessageDto(
                m.Id, m.ChannelId, m.SenderId, m.SenderName, m.SenderEmail,
                m.Subject, m.Message, m.IsRead, m.CreatedAt, m.ReadAt
            ))
            .ToListAsync();

        return Ok(messages);
    }

    /// <summary>
    /// Get unread message count for a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/messages/unread-count")]
    public async Task<ActionResult<int>> GetUnreadMessageCount(Guid channelId)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var count = await _db.ChannelMessages
            .CountAsync(m => m.ChannelId == channelId && !m.IsRead);

        return Ok(count);
    }

    /// <summary>
    /// Mark a message as read.
    /// </summary>
    [HttpPost("messages/{messageId:guid}/read")]
    public async Task<ActionResult> MarkMessageAsRead(Guid messageId)
    {
        var message = await _db.ChannelMessages.FindAsync(messageId);
        if (message == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(message.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Mark all messages as read for a channel.
    /// </summary>
    [HttpPost("channels/{channelId:guid}/messages/mark-all-read")]
    public async Task<ActionResult> MarkAllMessagesAsRead(Guid channelId)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var unreadMessages = await _db.ChannelMessages
            .Where(m => m.ChannelId == channelId && !m.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = now;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Delete a message.
    /// </summary>
    [HttpDelete("messages/{messageId:guid}")]
    public async Task<ActionResult> DeleteMessage(Guid messageId)
    {
        var message = await _db.ChannelMessages.FindAsync(messageId);
        if (message == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(message.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        _db.ChannelMessages.Remove(message);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ===== CHANNEL EVENTS =====

    /// <summary>
    /// Get events for a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/events")]
    public async Task<ActionResult<List<ChannelEventDto>>> GetChannelEvents(Guid channelId)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var events = await _db.ChannelEvents
            .Where(e => e.ChannelId == channelId)
            .OrderByDescending(e => e.EventDate)
            .Select(e => new ChannelEventDto(
                e.Id, e.ChannelId, e.Title, e.Description, e.EventDate,
                e.Location, e.ZoomLink, e.StreamingLink, e.IsActive
            ))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Create a new event.
    /// </summary>
    [HttpPost("events")]
    public async Task<ActionResult<ChannelEventDto>> CreateEvent([FromBody] CreateChannelEventDto dto)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(dto.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var channel = await _db.Channels.FindAsync(dto.ChannelId);
        if (channel == null)
            return NotFound("Channel not found");

        var evt = new ChannelEvent
        {
            ChannelId = dto.ChannelId,
            Title = dto.Title,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            ZoomLink = dto.ZoomLink,
            StreamingLink = dto.StreamingLink,
            TenantId = channel.TenantId
        };

        _db.ChannelEvents.Add(evt);
        await _db.SaveChangesAsync();

        return Ok(new ChannelEventDto(
            evt.Id, evt.ChannelId, evt.Title, evt.Description, evt.EventDate,
            evt.Location, evt.ZoomLink, evt.StreamingLink, evt.IsActive
        ));
    }

    /// <summary>
    /// Update an event.
    /// </summary>
    [HttpPut("events/{eventId:guid}")]
    public async Task<ActionResult<ChannelEventDto>> UpdateEvent(Guid eventId, [FromBody] UpdateChannelEventDto dto)
    {
        var evt = await _db.ChannelEvents.FindAsync(eventId);
        if (evt == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(evt.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        evt.Title = dto.Title;
        evt.Description = dto.Description;
        evt.EventDate = dto.EventDate;
        evt.Location = dto.Location;
        evt.ZoomLink = dto.ZoomLink;
        evt.StreamingLink = dto.StreamingLink;
        evt.IsActive = dto.IsActive;
        evt.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new ChannelEventDto(
            evt.Id, evt.ChannelId, evt.Title, evt.Description, evt.EventDate,
            evt.Location, evt.ZoomLink, evt.StreamingLink, evt.IsActive
        ));
    }

    /// <summary>
    /// Delete an event.
    /// </summary>
    [HttpDelete("events/{eventId:guid}")]
    public async Task<ActionResult> DeleteEvent(Guid eventId)
    {
        var evt = await _db.ChannelEvents.FindAsync(eventId);
        if (evt == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(evt.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        _db.ChannelEvents.Remove(evt);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
