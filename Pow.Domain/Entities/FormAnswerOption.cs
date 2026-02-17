namespace Pow.Domain.Entities;

/// <summary>
/// FormAnswerOption - An answer option for a choice-based question
/// </summary>
public class FormAnswerOption : TenantEntity
{
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// For Quiz type: points awarded if this option is selected
    /// </summary>
    public int Points { get; set; } = 0;

    /// <summary>
    /// Whether this is the correct answer (for Quiz type)
    /// </summary>
    public bool IsCorrect { get; set; } = false;

    /// <summary>
    /// Sort order within the question
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // Navigation
    public FormQuestion Question { get; set; } = null!;
}
