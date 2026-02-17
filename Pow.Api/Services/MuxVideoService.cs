using Mux.Csharp.Sdk.Api;
using Mux.Csharp.Sdk.Client;
using Mux.Csharp.Sdk.Model;

namespace Pow.Api.Services;

/// <summary>
/// MUX video service - uses MUX API for video hosting and streaming
/// </summary>
public class MuxVideoService : IVideoService
{
    private readonly string _tokenId;
    private readonly string _tokenSecret;
    private readonly AssetsApi _assetsApi;
    private readonly DirectUploadsApi _uploadsApi;

    public string ProviderName => "MUX";

    public MuxVideoService(string tokenId, string tokenSecret)
    {
        _tokenId = tokenId;
        _tokenSecret = tokenSecret;

        var config = new Configuration
        {
            Username = tokenId,
            Password = tokenSecret
        };

        _assetsApi = new AssetsApi(config);
        _uploadsApi = new DirectUploadsApi(config);
    }

    public async Task<VideoUploadResult> UploadVideoAsync(Stream videoStream, string fileName, string contentType)
    {
        try
        {
            // Create a direct upload URL
            var createUploadRequest = new CreateUploadRequest(
                newAssetSettings: new CreateAssetRequest(
                    playbackPolicy: new List<PlaybackPolicy> { PlaybackPolicy.Public },
                    mp4Support: CreateAssetRequest.Mp4SupportEnum.Standard
                ),
                corsOrigin: "*"
            );

            var uploadResponse = await _uploadsApi.CreateDirectUploadAsync(createUploadRequest);
            var uploadUrl = uploadResponse.Data.Url;
            var uploadId = uploadResponse.Data.Id;

            // Upload the video to MUX
            using var httpClient = new HttpClient();
            using var content = new StreamContent(videoStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            var response = await httpClient.PutAsync(uploadUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                return new VideoUploadResult(false, null, null, $"Upload failed: {response.StatusCode}");
            }

            // Wait a moment for MUX to process the upload
            await Task.Delay(2000);

            // Get the upload to retrieve the asset ID
            var upload = await _uploadsApi.GetDirectUploadAsync(uploadId);
            var assetId = upload.Data.AssetId;

            if (string.IsNullOrEmpty(assetId))
            {
                // If asset not ready yet, return upload ID as reference
                return new VideoUploadResult(true, $"mux:upload:{uploadId}", null, null);
            }

            // Get the asset to retrieve playback ID
            var asset = await _assetsApi.GetAssetAsync(assetId);
            var playbackId = asset.Data.PlaybackIds?.FirstOrDefault()?.Id;

            return new VideoUploadResult(true, $"mux:{assetId}", playbackId, null);
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
            if (string.IsNullOrEmpty(videoUrl) || !videoUrl.StartsWith("mux:"))
                return false;

            var parts = videoUrl.Split(':');
            if (parts.Length < 2)
                return false;

            var assetId = parts[1];

            // If it's an upload reference, we can't delete directly
            if (assetId == "upload")
                return false;

            await _assetsApi.DeleteAssetAsync(assetId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetPlaybackUrl(string videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl) || !videoUrl.StartsWith("mux:"))
            return videoUrl;

        var parts = videoUrl.Split(':');
        if (parts.Length < 2)
            return videoUrl;

        // For mux:assetId format, we need to get the playback ID
        // The playback URL format is: https://stream.mux.com/{PLAYBACK_ID}.m3u8
        var assetId = parts[1];

        // If we have a playback ID stored (mux:assetId:playbackId)
        if (parts.Length >= 3)
        {
            return $"https://stream.mux.com/{parts[2]}.m3u8";
        }

        // Return the asset ID - frontend will need to handle this
        return $"mux:{assetId}";
    }

    /// <summary>
    /// Get playback ID for an asset (useful for getting playback after upload processing)
    /// </summary>
    public async Task<string?> GetPlaybackIdAsync(string assetId)
    {
        try
        {
            var asset = await _assetsApi.GetAssetAsync(assetId);
            return asset.Data.PlaybackIds?.FirstOrDefault()?.Id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if an asset is ready for playback
    /// </summary>
    public async Task<bool> IsAssetReadyAsync(string assetId)
    {
        try
        {
            var asset = await _assetsApi.GetAssetAsync(assetId);
            return asset.Data.Status == Asset.StatusEnum.Ready;
        }
        catch
        {
            return false;
        }
    }
}
