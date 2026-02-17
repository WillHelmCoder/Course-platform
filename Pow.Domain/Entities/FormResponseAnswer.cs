namespace Pow.Domain.Entities;

/// <summary>
/// FormResponseAnswer - A single answer within a form response
/// </summary>
public class FormResponseAnswer : TenantEntity
{
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }

    /// <summary>
    /// For OpenText questions: the text answer
    /// </summary>
    public string? TextAnswer { get; set; }

    /// <summary>
    /// For SingleChoice questions: the selected option ID
    /// </summary>
    public Guid? SelectedOptionId { get; set; }

    /// <summary>
    /// For MultipleChoice questions: comma-separated option IDs
    /// </summary>
    public string? SelectedOptionIds { get; set; }

    /// <summary>
    /// Points earned for this answer (for Quiz type)
    /// </summary>
    public int PointsEarned { get; set; } = 0;

    // Navigation
    public FormResponse Response { get; set; } = null!;
    public FormQuestion Question { get; set; } = null!;
    public FormAnswerOption? SelectedOption { get; set; }
}
