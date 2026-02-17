using Pow.Domain.DTOs;

namespace Pow.Api.Services;

public interface IFormService
{
    // ===== FORMS (Creator) =====
    Task<List<FormDto>> GetFormsByChannelAsync(Guid channelId);
    Task<FormDetailDto?> GetFormAsync(Guid formId);
    Task<FormDto> CreateFormAsync(CreateFormDto dto, Guid tenantId);
    Task<FormDto?> UpdateFormAsync(Guid formId, UpdateFormDto dto);
    Task<bool> DeleteFormAsync(Guid formId);

    // ===== QUESTIONS (Creator) =====
    Task<FormQuestionDto> CreateQuestionAsync(CreateFormQuestionDto dto, Guid tenantId);
    Task<FormQuestionDto?> UpdateQuestionAsync(Guid questionId, UpdateFormQuestionDto dto);
    Task<bool> DeleteQuestionAsync(Guid questionId);

    // ===== ANSWER OPTIONS (Creator) =====
    Task<FormAnswerOptionDto> CreateAnswerOptionAsync(CreateFormAnswerOptionDto dto, Guid tenantId);
    Task<FormAnswerOptionDto?> UpdateAnswerOptionAsync(Guid optionId, UpdateFormAnswerOptionDto dto);
    Task<bool> DeleteAnswerOptionAsync(Guid optionId);

    // ===== RESPONSES (Creator) =====
    Task<List<FormResponseDto>> GetFormResponsesAsync(Guid formId);
    Task<FormResponseDetailDto?> GetFormResponseAsync(Guid responseId);
    Task<FormStatsDto?> GetFormStatsAsync(Guid formId);

    // ===== USER =====
    Task<List<PendingFormDto>> GetPendingFormsAsync(Guid userId, Guid channelId);
    Task<FormForUserDto?> GetFormForUserAsync(Guid formId);
    Task<FormResponseDetailDto?> SubmitFormAsync(Guid formId, Guid userId, SubmitFormDto dto, Guid tenantId);
    Task<FormResponseDetailDto?> GetUserFormResultAsync(Guid responseId, Guid userId);
    Task<bool> HasUserCompletedFormAsync(Guid formId, Guid userId);
}
