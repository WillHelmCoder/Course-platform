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
/// Public content controller - read-only access to published content.
/// </summary>
[ApiController]
[Route("api/cms/content")]
[AllowAnonymous]
public class ContentController : ControllerBase
{
    private readonly ICMSService _cmsService;
    private readonly AppDbContext _db;

    public ContentController(ICMSService cmsService, AppDbContext db)
    {
        _cmsService = cmsService;
        _db = db;
    }

    /// <summary>
    /// Get published contents with pagination.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContentListDto>>> GetPublishedContents(
        [FromQuery] int take = 20,
        [FromQuery] int skip = 0)
    {
        var contents = await _cmsService.GetPublishedContentsAsync(take, skip);
        return Ok(contents);
    }

    /// <summary>
    /// Get content for reading by slug.
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ContentReaderDto>> GetContent(string slug)
    {
        var content = await _cmsService.GetContentForReaderAsync(slug);
        if (content == null)
            return NotFound();

        return Ok(content);
    }

    /// <summary>
    /// Get published contents by channel slug.
    /// </summary>
    [HttpGet("channel/{channelSlug}")]
    public async Task<ActionResult<List<ContentListDto>>> GetByChannel(string channelSlug)
    {
        var contents = await _cmsService.GetPublishedContentsByChannelAsync(channelSlug);
        return Ok(contents);
    }

    /// <summary>
    /// Get published contents by course slug.
    /// </summary>
    [HttpGet("course/{courseSlug}/contents")]
    public async Task<ActionResult<List<ContentListDto>>> GetByCourse(string courseSlug)
    {
        var contents = await _cmsService.GetPublishedContentsByCourseAsync(courseSlug);
        return Ok(contents);
    }

    /// <summary>
    /// Get course by slug.
    /// </summary>
    [HttpGet("courses/{slug}")]
    public async Task<ActionResult<CourseDto>> GetCourseBySlug(string slug)
    {
        var course = await _cmsService.GetCourseBySlugAsync(slug);
        if (course == null || !course.IsActive || !course.IsPublished)
            return NotFound();
        return Ok(course);
    }

    /// <summary>
    /// Validate access code for code-restricted content.
    /// </summary>
    [HttpPost("{contentId:guid}/validate-code")]
    public async Task<ActionResult<bool>> ValidateAccessCode(
        Guid contentId,
        [FromBody] ValidateAccessCodeDto dto)
    {
        var isValid = await _cmsService.ValidateAccessCodeAsync(contentId, dto.Code);
        return Ok(isValid);
    }

    /// <summary>
    /// Track a content view.
    /// </summary>
    [HttpPost("{contentId:guid}/view")]
    public async Task<ActionResult> TrackView(Guid contentId)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? Guid.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString())
            : (Guid?)null;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        await _cmsService.TrackViewAsync(contentId, userId, ipAddress, userAgent);
        return Ok();
    }

    /// <summary>
    /// Get all active channels.
    /// </summary>
    [HttpGet("channels")]
    public async Task<ActionResult<List<ChannelDto>>> GetChannels()
    {
        var channels = await _cmsService.GetChannelsAsync();
        return Ok(channels.Where(c => c.IsActive).ToList());
    }

    /// <summary>
    /// Get all published courses for a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/courses")]
    public async Task<ActionResult<List<CourseDto>>> GetCourses(Guid channelId)
    {
        var courses = await _cmsService.GetCoursesAsync(channelId);
        return Ok(courses.Where(c => c.IsActive && c.IsPublished).ToList());
    }

    /// <summary>
    /// Get channel by slug (for registration flow).
    /// </summary>
    [HttpGet("channels/{slug}")]
    public async Task<ActionResult<ChannelDto>> GetChannelBySlug(string slug)
    {
        var channel = await _cmsService.GetChannelBySlugAsync(slug);
        if (channel == null || !channel.IsActive)
            return NotFound();
        return Ok(channel);
    }

    /// <summary>
    /// Get channel subscription plans.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/plans")]
    public async Task<ActionResult<List<ChannelPlanDto>>> GetChannelPlans(Guid channelId)
    {
        var plans = await _cmsService.GetChannelPlansAsync(channelId);
        return Ok(plans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToList());
    }

    // ===== CHANNEL EVENTS (PUBLIC) =====

    /// <summary>
    /// Get upcoming events for a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/events")]
    public async Task<ActionResult<List<ChannelEventDto>>> GetChannelEvents(Guid channelId)
    {
        var events = await _db.ChannelEvents
            .Where(e => e.ChannelId == channelId && e.IsActive && e.EventDate >= DateTime.UtcNow.Date)
            .OrderBy(e => e.EventDate)
            .Select(e => new ChannelEventDto(
                e.Id, e.ChannelId, e.Title, e.Description, e.EventDate,
                e.Location, e.ZoomLink, e.StreamingLink, e.IsActive
            ))
            .ToListAsync();

        return Ok(events);
    }

    // ===== CHANNEL MESSAGES (PUBLIC) =====

    /// <summary>
    /// Send a message to a channel inbox.
    /// Requires inbox to be enabled (Everyone or SubscribersOnly).
    /// SuperAdmin can always send messages.
    /// </summary>
    [HttpPost("channels/{channelId:guid}/messages")]
    public async Task<ActionResult> SendChannelMessage(Guid channelId, [FromBody] SendChannelMessageDto dto)
    {
        var channel = await _db.Channels.FindAsync(channelId);
        if (channel == null)
            return NotFound("Channel not found");

        // Get current user ID if authenticated
        Guid? senderId = null;
        var isSuperAdmin = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst("sub")?.Value ??
                           User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
            {
                senderId = userId;
                isSuperAdmin = User.IsInRole("SuperAdmin");
            }
        }

        // SuperAdmin can always send messages
        if (!isSuperAdmin)
        {
            // Check inbox visibility
            if (channel.InboxVisibility == InboxVisibility.Disabled)
                return BadRequest("Channel inbox is disabled");

            if (channel.InboxVisibility == InboxVisibility.SubscribersOnly)
            {
                // Must be authenticated and subscribed
                if (!senderId.HasValue)
                    return Unauthorized("You must be logged in to send messages to this channel");

                var isSubscriber = await _db.ChannelSubscriptions
                    .AnyAsync(s => s.UserId == senderId.Value &&
                                  s.ChannelId == channelId &&
                                  s.Status == SubscriptionStatus.Active);

                if (!isSubscriber)
                    return Forbid("Only subscribers can send messages to this channel");
            }
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.SenderName) ||
            string.IsNullOrWhiteSpace(dto.SenderEmail) ||
            string.IsNullOrWhiteSpace(dto.Subject) ||
            string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest("All fields are required");
        }

        var message = new ChannelMessage
        {
            ChannelId = channelId,
            SenderId = senderId,
            SenderName = dto.SenderName,
            SenderEmail = dto.SenderEmail,
            Subject = dto.Subject,
            Message = dto.Message,
            TenantId = channel.TenantId
        };

        _db.ChannelMessages.Add(message);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Message sent successfully" });
    }
}
