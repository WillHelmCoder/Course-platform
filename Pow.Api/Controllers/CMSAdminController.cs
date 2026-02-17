using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pow.Api.Services;
using Pow.Domain.DTOs;

namespace Pow.Api.Controllers;

/// <summary>
/// CMS Admin controller - SuperAdmin only operations.
/// </summary>
[ApiController]
[Route("api/cms/admin")]
[Authorize(Roles = "SuperAdmin")]
public class CMSAdminController : ControllerBase
{
    private readonly ICMSService _cmsService;

    public CMSAdminController(ICMSService cmsService)
    {
        _cmsService = cmsService;
    }

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirst("tenant_id")?.Value ?? Guid.Empty.ToString());

    // ===== CHANNELS =====

    /// <summary>
    /// Get all channels.
    /// </summary>
    [HttpGet("channels")]
    public async Task<ActionResult<List<ChannelDto>>> GetChannels()
    {
        var channels = await _cmsService.GetChannelsAsync();
        return Ok(channels);
    }

    /// <summary>
    /// Get channel by ID.
    /// </summary>
    [HttpGet("channels/{id:guid}")]
    public async Task<ActionResult<ChannelDto>> GetChannel(Guid id)
    {
        var channel = await _cmsService.GetChannelAsync(id);
        if (channel == null)
            return NotFound();

        return Ok(channel);
    }

    /// <summary>
    /// Create a new channel.
    /// </summary>
    [HttpPost("channels")]
    public async Task<ActionResult<ChannelDto>> CreateChannel([FromBody] CreateChannelDto dto)
    {
        var channel = await _cmsService.CreateChannelAsync(dto, GetTenantId());
        return CreatedAtAction(nameof(GetChannel), new { id = channel.Id }, channel);
    }

    /// <summary>
    /// Update a channel.
    /// </summary>
    [HttpPut("channels/{id:guid}")]
    public async Task<ActionResult<ChannelDto>> UpdateChannel(Guid id, [FromBody] UpdateChannelDto dto)
    {
        var channel = await _cmsService.UpdateChannelAsync(id, dto);
        if (channel == null)
            return NotFound();

        return Ok(channel);
    }

    /// <summary>
    /// Delete a channel.
    /// </summary>
    [HttpDelete("channels/{id:guid}")]
    public async Task<ActionResult> DeleteChannel(Guid id)
    {
        var deleted = await _cmsService.DeleteChannelAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ===== CHANNEL ADMINS =====

    /// <summary>
    /// Get admins for a channel.
    /// </summary>
    [HttpGet("channels/{channelId:guid}/admins")]
    public async Task<ActionResult<List<ChannelAdminDto>>> GetChannelAdmins(Guid channelId)
    {
        var admins = await _cmsService.GetChannelAdminsAsync(channelId);
        return Ok(admins);
    }

    /// <summary>
    /// Assign a user as channel admin.
    /// </summary>
    [HttpPost("channels/{channelId:guid}/admins/{userId:guid}")]
    public async Task<ActionResult> AssignChannelAdmin(Guid channelId, Guid userId)
    {
        var assigned = await _cmsService.AssignChannelAdminAsync(channelId, userId);
        if (!assigned)
            return BadRequest("Failed to assign admin");

        return Ok();
    }

    /// <summary>
    /// Remove a channel admin.
    /// </summary>
    [HttpDelete("channels/{channelId:guid}/admins/{userId:guid}")]
    public async Task<ActionResult> RemoveChannelAdmin(Guid channelId, Guid userId)
    {
        var removed = await _cmsService.RemoveChannelAdminAsync(channelId, userId);
        if (!removed)
            return NotFound();

        return NoContent();
    }

    // ===== ALL COURSES (for admin oversight) =====

    /// <summary>
    /// Get all courses.
    /// </summary>
    [HttpGet("courses")]
    public async Task<ActionResult<List<CourseDto>>> GetAllCourses([FromQuery] Guid? channelId = null)
    {
        var courses = await _cmsService.GetCoursesAsync(channelId);
        return Ok(courses);
    }

    // ===== ALL CONTENT (for admin oversight) =====

    /// <summary>
    /// Get all content.
    /// </summary>
    [HttpGet("contents")]
    public async Task<ActionResult<List<ContentListDto>>> GetAllContents([FromQuery] Guid? chapterId = null)
    {
        var contents = await _cmsService.GetContentsAsync(chapterId);
        return Ok(contents);
    }

    // ===== STATS =====

    /// <summary>
    /// Get CMS statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<CMSStatsDto>> GetStats()
    {
        var stats = await _cmsService.GetStatsAsync();
        return Ok(stats);
    }
}
