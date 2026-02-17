using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;

namespace Pow.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get a public setting (Terms, Privacy, etc.)
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<SiteSettingDto>> GetSetting(string key)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
            return Ok(new SiteSettingDto(key, "", null));

        return Ok(new SiteSettingDto(setting.Key, setting.Value, setting.Description));
    }

    /// <summary>
    /// Get all settings (Super Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<List<SiteSettingDto>>> GetAllSettings()
    {
        var settings = await _db.SiteSettings
            .Select(s => new SiteSettingDto(s.Key, s.Value, s.Description))
            .ToListAsync();

        return Ok(settings);
    }

    /// <summary>
    /// Update a setting (Super Admin only)
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SiteSettingDto>> UpdateSetting(string key, [FromBody] UpdateSettingDto dto)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            setting = new SiteSetting
            {
                Key = key,
                Value = dto.Value,
                Description = dto.Description
            };
            _db.SiteSettings.Add(setting);
        }
        else
        {
            setting.Value = dto.Value;
            setting.Description = dto.Description;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new SiteSettingDto(setting.Key, setting.Value, setting.Description));
    }

    /// <summary>
    /// Get video provider settings (Super Admin only)
    /// </summary>
    [HttpGet("video")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VideoSettingsDto>> GetVideoSettings()
    {
        var provider = await _db.SiteSettings
            .Where(s => s.Key == "video_provider")
            .Select(s => s.Value)
            .FirstOrDefaultAsync() ?? "";

        var muxTokenId = await _db.SiteSettings
            .Where(s => s.Key == "mux_token_id")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        var hasMuxSecret = await _db.SiteSettings
            .AnyAsync(s => s.Key == "mux_token_secret" && !string.IsNullOrEmpty(s.Value));

        var hasVimeoToken = await _db.SiteSettings
            .AnyAsync(s => s.Key == "vimeo_access_token" && !string.IsNullOrEmpty(s.Value));

        return Ok(new VideoSettingsDto(provider, muxTokenId, hasMuxSecret, hasVimeoToken));
    }

    /// <summary>
    /// Update video provider settings (Super Admin only)
    /// </summary>
    [HttpPut("video")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VideoSettingsDto>> UpdateVideoSettings([FromBody] UpdateVideoSettingsDto dto)
    {
        // Update provider
        await UpdateOrCreateSetting("video_provider", dto.Provider, "Video Provider (mux or vimeo)");

        // Update MUX settings
        if (dto.MuxTokenId != null)
        {
            await UpdateOrCreateSetting("mux_token_id", dto.MuxTokenId, "MUX Token ID");
        }

        if (!string.IsNullOrEmpty(dto.MuxTokenSecret))
        {
            await UpdateOrCreateSetting("mux_token_secret", dto.MuxTokenSecret, "MUX Token Secret");
        }

        // Update Vimeo settings
        if (!string.IsNullOrEmpty(dto.VimeoAccessToken))
        {
            await UpdateOrCreateSetting("vimeo_access_token", dto.VimeoAccessToken, "Vimeo Access Token");
        }

        await _db.SaveChangesAsync();

        var hasMuxSecret = await _db.SiteSettings
            .AnyAsync(s => s.Key == "mux_token_secret" && !string.IsNullOrEmpty(s.Value));

        var hasVimeoToken = await _db.SiteSettings
            .AnyAsync(s => s.Key == "vimeo_access_token" && !string.IsNullOrEmpty(s.Value));

        return Ok(new VideoSettingsDto(dto.Provider, dto.MuxTokenId, hasMuxSecret, hasVimeoToken));
    }

    private async Task UpdateOrCreateSetting(string key, string value, string? description)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = value,
                Description = description
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }
}

public record UpdateSettingDto(string Value, string? Description);
public record UpdateVideoSettingsDto(string Provider, string? MuxTokenId, string? MuxTokenSecret, string? VimeoAccessToken);
