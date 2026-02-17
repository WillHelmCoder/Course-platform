namespace Pow.Domain.Entities;

/// <summary>
/// ContentView - Tracks content views for reporting.
/// </summary>
public class ContentView : BaseEntity
{
    public Guid ContentId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Content Content { get; set; } = null!;
    public User? User { get; set; }
}
