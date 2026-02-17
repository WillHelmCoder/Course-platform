using System.Net.Http.Headers;
using System.Text.Json;

namespace Pow.Api.Services;

/// <summary>
/// Vimeo video service - uses Vimeo API for video hosting and streaming
/// </summary>
public class VimeoVideoService : IVideoService
{
    private readonly string _accessToken;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Vimeo";

    public VimeoVideoService(string accessToken)
    {
        _accessToken = accessToken;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.vimeo.*+json"));
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.vimeo.*+json;version=3.4");
    }

    public async Task<VideoUploadResult> UploadVideoAsync(Stream videoStream, string fileName, string contentType)
    {
        try
        {
            // Step 1: Create upload ticket
            var createRequest = new
            {
                upload = new
                {
                    approach = "tus",
                    size = videoStream.Length
                },
                name = Path.GetFileNameWithoutExtension(fileName)
            };

            var createResponse = await _httpClient.PostAsync(
                "https://api.vimeo.com/me/videos",
                new StringContent(JsonSerializer.Serialize(createRequest), System.Text.Encoding.UTF8, "application/json")
            );

            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                return new VideoUploadResult(false, null, null, $"Failed to create upload: {error}");
            }

            var createResult = await createResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(createResult);
            var root = doc.RootElement;

            var uploadLink = root.GetProperty("upload").GetProperty("upload_link").GetString();
            var videoUri = root.GetProperty("uri").GetString(); // e.g., "/videos/123456789"
            var videoId = videoUri?.Split('/').LastOrDefault();

            if (string.IsNullOrEmpty(uploadLink))
            {
                return new VideoUploadResult(false, null, null, "No upload link received");
            }

            // Step 2: Upload using TUS protocol
            using var tusClient = new HttpClient();
            tusClient.DefaultRequestHeaders.Add("Tus-Resumable", "1.0.0");
            tusClient.DefaultRequestHeaders.Add("Upload-Offset", "0");
            tusClient.DefaultRequestHeaders.Add("Content-Type", "application/offset+octet-stream");

            using var content = new StreamContent(videoStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

            var uploadResponse = await tusClient.PatchAsync(uploadLink, content);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                return new VideoUploadResult(false, null, null, $"Upload failed: {uploadResponse.StatusCode}");
            }

            // Return the Vimeo video ID
            return new VideoUploadResult(true, $"vimeo:{videoId}", videoId, null);
        }
        catch (Exception ex)
        {
            return new VideoUploadResult(false, null, null, ex.Message);
        }
    }

    public async Task<bool> DeleteVideoAsync(string videoUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(videoUrl) || !videoUrl.StartsWith("vimeo:"))
                return false;

            var videoId = videoUrl.Replace("vimeo:", "");
            var response = await _httpClient.DeleteAsync($"https://api.vimeo.com/videos/{videoId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string GetPlaybackUrl(string videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl) || !videoUrl.StartsWith("vimeo:"))
            return videoUrl;

        var videoId = videoUrl.Replace("vimeo:", "");
        // Return embed URL for Vimeo player
        return $"https://player.vimeo.com/video/{videoId}";
    }

    /// <summary>
    /// Get video status and details
    /// </summary>
    public async Task<VimeoVideoStatus?> GetVideoStatusAsync(string videoId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://api.vimeo.com/videos/{videoId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();
            var duration = root.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0;

            return new VimeoVideoStatus(
                VideoId: videoId,
                Status: status ?? "unknown",
                Duration: duration,
                IsPlayable: status == "available"
            );
        }
        catch
        {
            return null;
        }
    }
}

public record VimeoVideoStatus(
    string VideoId,
    string Status,
    int Duration,
    bool IsPlayable
);
