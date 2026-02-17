using Pow.Domain.Enums;

namespace Pow.Domain.DTOs;

// ===== FORM =====
public record FormDto(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    string Title,
    string? Description,
    FormType FormType,
    FormTrigger Trigger,
    Guid? TriggerCourseId,
    string? TriggerCourseName,
    int? PassingScore,
    bool IsActive,
    int SortOrder,
    int QuestionCount,
    int ResponseCount,
    DateTime CreatedAt
);

public record FormDetailDto(
    Guid Id,
    Guid ChannelId,
    string ChannelName,
    string Title,
    string? Description,
    FormType FormType,
    FormTrigger Trigger,
    Guid? TriggerCourseId,
    string? TriggerCourseName,
    int? PassingScore,
    bool IsActive,
    int SortOrder,
    List<FormQuestionDto> Questions,
    DateTime CreatedAt
);

public record CreateFormDto(
    Guid ChannelId,
    string Title,
    string? Description,
    FormType FormType,
    FormTrigger Trigger,
    Guid? TriggerCourseId,
    int? PassingScore
);

public record UpdateFormDto(
    string Title,
    string? Description,
    FormType FormType,
    FormTrigger Trigger,
    Guid? TriggerCourseId,
    int? PassingScore,
    bool IsActive,
    int SortOrder
);

// ===== FORM QUESTION =====
public record FormQuestionDto(
    Guid Id,
    Guid FormId,
    string Text,
    string? Description,
    QuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    List<FormAnswerOptionDto> AnswerOptions
);

public record CreateFormQuestionDto(
    Guid FormId,
    string Text,
    string? Description,
    QuestionType QuestionType,
    bool IsRequired
);

public record UpdateFormQuestionDto(
    string Text,
    string? Description,
    QuestionType QuestionType,
    bool IsRequired,
    int SortOrder
);

// ===== FORM ANSWER OPTION =====
public record FormAnswerOptionDto(
    Guid Id,
    Guid QuestionId,
    string Text,
    int Points,
    bool IsCorrect,
    int SortOrder
);

public record CreateFormAnswerOptionDto(
    Guid QuestionId,
    string Text,
    int Points,
    bool IsCorrect
);

public record UpdateFormAnswerOptionDto(
    string Text,
    int Points,
    bool IsCorrect,
    int SortOrder
);

// ===== FORM RESPONSE =====
public record FormResponseDto(
    Guid Id,
    Guid FormId,
    string FormTitle,
    Guid UserId,
    string UserEmail,
    FormResponseStatus Status,
    int? Score,
    int? MaxScore,
    bool? Passed,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record FormResponseDetailDto(
    Guid Id,
    Guid FormId,
    string FormTitle,
    FormType FormType,
    Guid UserId,
    string UserEmail,
    FormResponseStatus Status,
    int? Score,
    int? MaxScore,
    bool? Passed,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<FormResponseAnswerDto> Answers
);

public record FormResponseAnswerDto(
    Guid Id,
    Guid QuestionId,
    string QuestionText,
    QuestionType QuestionType,
    string? TextAnswer,
    Guid? SelectedOptionId,
    string? SelectedOptionText,
    List<Guid>? SelectedOptionIds,
    List<string>? SelectedOptionTexts,
    int PointsEarned,
    int MaxPoints,
    bool? IsCorrect
);

// ===== SUBMIT FORM =====
public record SubmitFormDto(
    List<SubmitAnswerDto> Answers
);

public record SubmitAnswerDto(
    Guid QuestionId,
    string? TextAnswer,
    Guid? SelectedOptionId,
    List<Guid>? SelectedOptionIds
);

// ===== USER FORM VIEW (for filling) =====
public record FormForUserDto(
    Guid Id,
    string Title,
    string? Description,
    FormType FormType,
    List<FormQuestionForUserDto> Questions
);

public record FormQuestionForUserDto(
    Guid Id,
    string Text,
    string? Description,
    QuestionType QuestionType,
    bool IsRequired,
    List<FormAnswerOptionForUserDto> AnswerOptions
);

public record FormAnswerOptionForUserDto(
    Guid Id,
    string Text
);

// ===== PENDING FORMS =====
public record PendingFormDto(
    Guid Id,
    string Title,
    string? Description,
    FormType FormType,
    FormTrigger Trigger,
    string ChannelName
);

// ===== STATS =====
public record FormStatsDto(
    Guid FormId,
    string Title,
    FormType FormType,
    int TotalResponses,
    int CompletedResponses,
    int InProgressResponses,
    double? AverageScore,
    int? PassedCount,
    int? FailedCount,
    double? PassRate,
    List<QuestionStatsDto> QuestionStats
);

public record QuestionStatsDto(
    Guid QuestionId,
    string Text,
    QuestionType QuestionType,
    int TotalAnswers,
    List<OptionStatsDto>? OptionStats,
    List<string>? SampleTextAnswers
);

public record OptionStatsDto(
    Guid OptionId,
    string Text,
    int SelectionCount,
    double SelectionPercentage
);
