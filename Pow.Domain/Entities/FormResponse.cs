using Pow.Domain.Enums;

namespace Pow.Domain.Entities;

/// <summary>
/// FormResponse - A user's response to a form
/// </summary>
public class FormResponse : TenantEntity
{
    public Guid FormId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// Status of the response
    /// </summary>
    public FormResponseStatus Status { get; set; } = FormResponseStatus.InProgress;

    /// <summary>
    /// For Quiz type: total score achieved
    /// </summary>
    public int? Score { get; set; }

    /// <summary>
    /// For Quiz type: maximum possible score
    /// </summary>
    public int? MaxScore { get; set; }

    /// <summary>
    /// For Quiz type: whether the user passed
    /// </summary>
    public bool? Passed { get; set; }

    /// <summary>
    /// When the response was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Form Form { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<FormResponseAnswer> Answers { get; set; } = new List<FormResponseAnswer>();
}
