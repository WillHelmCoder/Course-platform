namespace Pow.Domain.Entities;

/// <summary>
/// Chapter - Container for content within a course.
/// </summary>
public class Chapter : TenantEntity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
