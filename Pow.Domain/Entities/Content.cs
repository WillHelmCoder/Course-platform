using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// Content - The actual content piece (article, video, etc.)
/// </summary>
public class Content : TenantEntity
{
    public Guid ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? MainPicture { get; set; }
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Video URL (YouTube, Vimeo, or uploaded file path)
    /// </summary>
    public string? VideoUrl { get; set; }

    /// <summary>
    /// Access level: Public, LoggedIn, AllSubscribers
    /// Note: Plan-specific access is controlled at Course level
    /// </summary>
    public ContentAccessLevel AccessLevel { get; set; } = ContentAccessLevel.Public;

    /// <summary>
    /// Access code - if provided and user enters this code, bypasses all access rules.
    /// </summary>
    public string? AccessCode { get; set; }

    public bool IsPublished { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public int ViewCount { get; set; } = 0;
    public DateTime? PublishedAt { get; set; }

    // Navigation
    public Chapter Chapter { get; set; } = null!;
    public ICollection<ContentView> Views { get; set; } = new List<ContentView>();
    public ICollection<ContentAttachment> Attachments { get; set; } = new List<ContentAttachment>();
}
