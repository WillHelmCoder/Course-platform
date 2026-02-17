namespace Pow.Domain.Entities;

/// <summary>
/// Message sent to a channels inbox.
/// </summary>
public class ChannelMessage : TenantEntity
{
    public Guid ChannelId { get; set; }

    /// <summary>
    /// The user who sent the message (null if sent by anonymous/guest).
    /// </summary>
    public Guid? SenderId { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation
    public Channel Channel { get; set; } = null!;
    public User? Sender { get; set; }
}
