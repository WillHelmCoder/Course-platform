using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// FormQuestion - A question within a form
/// </summary>
public class FormQuestion : TenantEntity
{
    public Guid FormId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Type of question
    /// </summary>
    public QuestionType QuestionType { get; set; } = QuestionType.SingleChoice;

    /// <summary>
    /// Whether this question is required
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Sort order within the form
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // Navigation
    public Form Form { get; set; } = null!;
    public ICollection<FormAnswerOption> AnswerOptions { get; set; } = new List<FormAnswerOption>();
    public ICollection<FormResponseAnswer> ResponseAnswers { get; set; } = new List<FormResponseAnswer>();
}
