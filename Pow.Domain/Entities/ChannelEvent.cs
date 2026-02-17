namespace Pow.Domain.Entities;

/// <summary>
/// Event associated with a channel.
/// Can be in-person, online (Zoom/streaming), or hybrid.
/// </summary>
public class ChannelEvent : TenantEntity
{
    public Guid ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    
    /// <summary>
    /// Physical location (address, venue name, etc.)
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// Zoom meeting link
    /// </summary>
    public string? ZoomLink { get; set; }
    
    /// <summary>
    /// Streaming link (YouTube, Twitch, etc.)
    /// </summary>
    public string? StreamingLink { get; set; }
    
    public bool IsActive { get; set; } = true;

    // Navigation
    public Channel Channel { get; set; } = null!;
}
