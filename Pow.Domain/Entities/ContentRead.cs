namespace Pow.Domain.Entities;

/// <summary>
/// ContentRead - Tracks which content a user has marked as read.
/// </summary>
public class ContentRead : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ContentId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Content Content { get; set; } = null!;
}
