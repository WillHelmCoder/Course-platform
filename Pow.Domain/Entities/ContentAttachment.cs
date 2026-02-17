namespace Pow.Domain.Entities;

/// <summary>
/// Content attachment - PDFs and other files attached to content
/// </summary>
public class ContentAttachment : TenantEntity
{
    public Guid ContentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int SortOrder { get; set; } = 0;

    // Navigation
    public Content Content { get; set; } = null!;
}
