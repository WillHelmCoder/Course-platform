using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;

namespace Pow.Api.Services;

/// <summary>
/// Factory for creating the appropriate video service based on site settings
/// </summary>
public class VideoServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public VideoServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<IVideoService?> GetVideoServiceAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var provider = await db.SiteSettings
            .Where(s => s.Key == "video_provider")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        if (provider?.ToLower() == "mux")
        {
            var tokenId = await db.SiteSettings
                .Where(s => s.Key == "mux_token_id")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            var tokenSecret = await db.SiteSettings
                .Where(s => s.Key == "mux_token_secret")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(tokenId) && !string.IsNullOrEmpty(tokenSecret))
            {
                return new MuxVideoService(tokenId, tokenSecret);
            }
        }
        else if (provider?.ToLower() == "vimeo")
        {
            var accessToken = await db.SiteSettings
                .Where(s => s.Key == "vimeo_access_token")
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(accessToken))
            {
                return new VimeoVideoService(accessToken);
            }
        }

        // No valid provider configured
        return null;
    }

    /// <summary>
    /// Get the current provider name from settings
    /// </summary>
    public async Task<string?> GetCurrentProviderAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.SiteSettings
            .Where(s => s.Key == "video_provider")
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Check if video service is properly configured
    /// </summary>
    public async Task<bool> IsConfiguredAsync()
    {
        var service = await GetVideoServiceAsync();
        return service != null;
    }
}
