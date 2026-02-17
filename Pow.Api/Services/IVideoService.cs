namespace Pow.Api.Services;

/// <summary>
/// Interface for video service providers (Local, MUX, etc.)
/// </summary>
public interface IVideoService
{
    /// <summary>
    /// Upload a video and return the video URL/ID
    /// </summary>
    Task<VideoUploadResult> UploadVideoAsync(Stream videoStream, string fileName, string contentType);

    /// <summary>
    /// Delete a video by its URL/ID
    /// </summary>
    Task<bool> DeleteVideoAsync(string videoUrl);

    /// <summary>
    /// Get the playback URL for a video
    /// </summary>
    string GetPlaybackUrl(string videoUrl);

    /// <summary>
    /// Get the provider name
    /// </summary>
    string ProviderName { get; }
}

public record VideoUploadResult(
    bool Success,
    string? VideoUrl,
    string? PlaybackId,
    string? Error
);
