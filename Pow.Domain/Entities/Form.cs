using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// Form - Main form/survey/quiz entity
/// </summary>
public class Form : TenantEntity
{
    public Guid ChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Type of form - Survey (no scoring) or Quiz (with scoring)
    /// </summary>
    public FormType FormType { get; set; } = FormType.Survey;

    /// <summary>
    /// When this form is shown to users
    /// </summary>
    public FormTrigger Trigger { get; set; } = FormTrigger.Manual;

    /// <summary>
    /// If trigger is AfterCourseComplete, which course triggers it
    /// </summary>
    public Guid? TriggerCourseId { get; set; }

    /// <summary>
    /// For Quiz type: minimum score percentage to pass (0-100)
    /// </summary>
    public int? PassingScore { get; set; }

    /// <summary>
    /// Whether the form is active and accepting responses
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // Navigation
    public Channel Channel { get; set; } = null!;
    public Course? TriggerCourse { get; set; }
    public ICollection<FormQuestion> Questions { get; set; } = new List<FormQuestion>();
    public ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>();
}
