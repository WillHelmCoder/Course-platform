using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;
using Pow.Domain.Enums;

namespace Pow.Api.Services;

public class FormService : IFormService
{
    private readonly AppDbContext _db;

    public FormService(AppDbContext db)
    {
        _db = db;
    }

    // ===== FORMS (Creator) =====

    public async Task<List<FormDto>> GetFormsByChannelAsync(Guid channelId)
    {
        return await _db.Forms
            .Where(f => f.ChannelId == channelId)
            .OrderBy(f => f.SortOrder)
            .ThenByDescending(f => f.CreatedAt)
            .Select(f => new FormDto(
                f.Id,
                f.ChannelId,
                f.Channel.Name,
                f.Title,
                f.Description,
                f.FormType,
                f.Trigger,
                f.TriggerCourseId,
                f.TriggerCourse != null ? f.TriggerCourse.Title : null,
                f.PassingScore,
                f.IsActive,
                f.SortOrder,
                f.Questions.Count,
                f.Responses.Count,
                f.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<FormDetailDto?> GetFormAsync(Guid formId)
    {
        var form = await _db.Forms
            .Include(f => f.Channel)
            .Include(f => f.TriggerCourse)
            .Include(f => f.Questions.OrderBy(q => q.SortOrder))
                .ThenInclude(q => q.AnswerOptions.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null) return null;

        return new FormDetailDto(
            form.Id,
            form.ChannelId,
            form.Channel.Name,
            form.Title,
            form.Description,
            form.FormType,
            form.Trigger,
            form.TriggerCourseId,
            form.TriggerCourse?.Title,
            form.PassingScore,
            form.IsActive,
            form.SortOrder,
            form.Questions.Select(q => new FormQuestionDto(
                q.Id,
                q.FormId,
                q.Text,
                q.Description,
                q.QuestionType,
                q.IsRequired,
                q.SortOrder,
                q.AnswerOptions.Select(o => new FormAnswerOptionDto(
                    o.Id,
                    o.QuestionId,
                    o.Text,
                    o.Points,
                    o.IsCorrect,
                    o.SortOrder
                )).ToList()
            )).ToList(),
            form.CreatedAt
        );
    }

    public async Task<FormDto> CreateFormAsync(CreateFormDto dto, Guid tenantId)
    {
        var channel = await _db.Channels.FindAsync(dto.ChannelId);
        if (channel == null)
            throw new InvalidOperationException("Channel not found");

        var form = new Form
        {
            ChannelId = dto.ChannelId,
            Title = dto.Title,
            Description = dto.Description,
            FormType = dto.FormType,
            Trigger = dto.Trigger,
            TriggerCourseId = dto.TriggerCourseId,
            PassingScore = dto.PassingScore,
            TenantId = tenantId
        };

        _db.Forms.Add(form);
        await _db.SaveChangesAsync();

        string? triggerCourseName = null;
        if (dto.TriggerCourseId.HasValue)
        {
            var course = await _db.Courses.FindAsync(dto.TriggerCourseId.Value);
            triggerCourseName = course?.Title;
        }

        return new FormDto(
            form.Id,
            form.ChannelId,
            channel.Name,
            form.Title,
            form.Description,
            form.FormType,
            form.Trigger,
            form.TriggerCourseId,
            triggerCourseName,
            form.PassingScore,
            form.IsActive,
            form.SortOrder,
            0,
            0,
            form.CreatedAt
        );
    }

    public async Task<FormDto?> UpdateFormAsync(Guid formId, UpdateFormDto dto)
    {
        var form = await _db.Forms
            .Include(f => f.Channel)
            .Include(f => f.Questions)
            .Include(f => f.Responses)
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null) return null;

        form.Title = dto.Title;
        form.Description = dto.Description;
        form.FormType = dto.FormType;
        form.Trigger = dto.Trigger;
        form.TriggerCourseId = dto.TriggerCourseId;
        form.PassingScore = dto.PassingScore;
        form.IsActive = dto.IsActive;
        form.SortOrder = dto.SortOrder;
        form.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        string? triggerCourseName = null;
        if (dto.TriggerCourseId.HasValue)
        {
            var course = await _db.Courses.FindAsync(dto.TriggerCourseId.Value);
            triggerCourseName = course?.Title;
        }

        return new FormDto(
            form.Id,
            form.ChannelId,
            form.Channel.Name,
            form.Title,
            form.Description,
            form.FormType,
            form.Trigger,
            form.TriggerCourseId,
            triggerCourseName,
            form.PassingScore,
            form.IsActive,
            form.SortOrder,
            form.Questions.Count,
            form.Responses.Count,
            form.CreatedAt
        );
    }

    public async Task<bool> DeleteFormAsync(Guid formId)
    {
        var form = await _db.Forms.FindAsync(formId);
        if (form == null) return false;

        _db.Forms.Remove(form);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== QUESTIONS (Creator) =====

    public async Task<FormQuestionDto> CreateQuestionAsync(CreateFormQuestionDto dto, Guid tenantId)
    {
        var form = await _db.Forms.FindAsync(dto.FormId);
        if (form == null)
            throw new InvalidOperationException("Form not found");

        var maxSortOrder = await _db.FormQuestions
            .Where(q => q.FormId == dto.FormId)
            .MaxAsync(q => (int?)q.SortOrder) ?? -1;

        var question = new FormQuestion
        {
            FormId = dto.FormId,
            Text = dto.Text,
            Description = dto.Description,
            QuestionType = dto.QuestionType,
            IsRequired = dto.IsRequired,
            SortOrder = maxSortOrder + 1,
            TenantId = tenantId
        };

        _db.FormQuestions.Add(question);
        await _db.SaveChangesAsync();

        return new FormQuestionDto(
            question.Id,
            question.FormId,
            question.Text,
            question.Description,
            question.QuestionType,
            question.IsRequired,
            question.SortOrder,
            new List<FormAnswerOptionDto>()
        );
    }

    public async Task<FormQuestionDto?> UpdateQuestionAsync(Guid questionId, UpdateFormQuestionDto dto)
    {
        var question = await _db.FormQuestions
            .Include(q => q.AnswerOptions.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question == null) return null;

        question.Text = dto.Text;
        question.Description = dto.Description;
        question.QuestionType = dto.QuestionType;
        question.IsRequired = dto.IsRequired;
        question.SortOrder = dto.SortOrder;
        question.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new FormQuestionDto(
            question.Id,
            question.FormId,
            question.Text,
            question.Description,
            question.QuestionType,
            question.IsRequired,
            question.SortOrder,
            question.AnswerOptions.Select(o => new FormAnswerOptionDto(
                o.Id,
                o.QuestionId,
                o.Text,
                o.Points,
                o.IsCorrect,
                o.SortOrder
            )).ToList()
        );
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        var question = await _db.FormQuestions.FindAsync(questionId);
        if (question == null) return false;

        _db.FormQuestions.Remove(question);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== ANSWER OPTIONS (Creator) =====

    public async Task<FormAnswerOptionDto> CreateAnswerOptionAsync(CreateFormAnswerOptionDto dto, Guid tenantId)
    {
        var question = await _db.FormQuestions.FindAsync(dto.QuestionId);
        if (question == null)
            throw new InvalidOperationException("Question not found");

        var maxSortOrder = await _db.FormAnswerOptions
            .Where(o => o.QuestionId == dto.QuestionId)
            .MaxAsync(o => (int?)o.SortOrder) ?? -1;

        var option = new FormAnswerOption
        {
            QuestionId = dto.QuestionId,
            Text = dto.Text,
            Points = dto.Points,
            IsCorrect = dto.IsCorrect,
            SortOrder = maxSortOrder + 1,
            TenantId = tenantId
        };

        _db.FormAnswerOptions.Add(option);
        await _db.SaveChangesAsync();

        return new FormAnswerOptionDto(
            option.Id,
            option.QuestionId,
            option.Text,
            option.Points,
            option.IsCorrect,
            option.SortOrder
        );
    }

    public async Task<FormAnswerOptionDto?> UpdateAnswerOptionAsync(Guid optionId, UpdateFormAnswerOptionDto dto)
    {
        var option = await _db.FormAnswerOptions.FindAsync(optionId);
        if (option == null) return null;

        option.Text = dto.Text;
        option.Points = dto.Points;
        option.IsCorrect = dto.IsCorrect;
        option.SortOrder = dto.SortOrder;
        option.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new FormAnswerOptionDto(
            option.Id,
            option.QuestionId,
            option.Text,
            option.Points,
            option.IsCorrect,
            option.SortOrder
        );
    }

    public async Task<bool> DeleteAnswerOptionAsync(Guid optionId)
    {
        var option = await _db.FormAnswerOptions.FindAsync(optionId);
        if (option == null) return false;

        _db.FormAnswerOptions.Remove(option);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== RESPONSES (Creator) =====

    public async Task<List<FormResponseDto>> GetFormResponsesAsync(Guid formId)
    {
        return await _db.FormResponses
            .Where(r => r.FormId == formId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new FormResponseDto(
                r.Id,
                r.FormId,
                r.Form.Title,
                r.UserId,
                r.User.Email,
                r.Status,
                r.Score,
                r.MaxScore,
                r.Passed,
                r.CreatedAt,
                r.CompletedAt
            ))
            .ToListAsync();
    }

    public async Task<FormResponseDetailDto?> GetFormResponseAsync(Guid responseId)
    {
        var response = await _db.FormResponses
            .Include(r => r.Form)
            .Include(r => r.User)
            .Include(r => r.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.AnswerOptions)
            .Include(r => r.Answers)
                .ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null) return null;

        return MapToResponseDetail(response);
    }

    public async Task<FormStatsDto?> GetFormStatsAsync(Guid formId)
    {
        var form = await _db.Forms
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .Include(f => f.Responses)
                .ThenInclude(r => r.Answers)
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null) return null;

        var completedResponses = form.Responses.Where(r => r.Status == FormResponseStatus.Completed).ToList();
        var inProgressResponses = form.Responses.Where(r => r.Status == FormResponseStatus.InProgress).ToList();

        double? averageScore = null;
        int? passedCount = null;
        int? failedCount = null;
        double? passRate = null;

        if (form.FormType == FormType.Quiz && completedResponses.Any())
        {
            var scores = completedResponses.Where(r => r.Score.HasValue && r.MaxScore.HasValue && r.MaxScore > 0)
                .Select(r => (double)r.Score!.Value / r.MaxScore!.Value * 100)
                .ToList();

            if (scores.Any())
                averageScore = scores.Average();

            passedCount = completedResponses.Count(r => r.Passed == true);
            failedCount = completedResponses.Count(r => r.Passed == false);

            if (completedResponses.Any())
                passRate = (double)passedCount / completedResponses.Count * 100;
        }

        var questionStats = form.Questions.OrderBy(q => q.SortOrder).Select(q =>
        {
            var answers = form.Responses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionId == q.Id)
                .ToList();

            List<OptionStatsDto>? optionStats = null;
            List<string>? sampleTextAnswers = null;

            if (q.QuestionType == QuestionType.OpenText)
            {
                sampleTextAnswers = answers
                    .Where(a => !string.IsNullOrEmpty(a.TextAnswer))
                    .Select(a => a.TextAnswer!)
                    .Take(10)
                    .ToList();
            }
            else
            {
                optionStats = q.AnswerOptions.OrderBy(o => o.SortOrder).Select(o =>
                {
                    var count = answers.Count(a =>
                        a.SelectedOptionId == o.Id ||
                        (a.SelectedOptionIds != null && a.SelectedOptionIds.Contains(o.Id.ToString())));

                    var percentage = answers.Any() ? (double)count / answers.Count * 100 : 0;

                    return new OptionStatsDto(o.Id, o.Text, count, percentage);
                }).ToList();
            }

            return new QuestionStatsDto(
                q.Id,
                q.Text,
                q.QuestionType,
                answers.Count,
                optionStats,
                sampleTextAnswers
            );
        }).ToList();

        return new FormStatsDto(
            form.Id,
            form.Title,
            form.FormType,
            form.Responses.Count,
            completedResponses.Count,
            inProgressResponses.Count,
            averageScore,
            passedCount,
            failedCount,
            passRate,
            questionStats
        );
    }

    // ===== USER =====

    public async Task<List<PendingFormDto>> GetPendingFormsAsync(Guid userId, Guid channelId)
    {
        // Get forms that the user hasn't completed yet
        var completedFormIds = await _db.FormResponses
            .Where(r => r.UserId == userId && r.Status == FormResponseStatus.Completed)
            .Select(r => r.FormId)
            .ToListAsync();

        return await _db.Forms
            .Where(f => f.ChannelId == channelId && f.IsActive && !completedFormIds.Contains(f.Id))
            .OrderBy(f => f.SortOrder)
            .Select(f => new PendingFormDto(
                f.Id,
                f.Title,
                f.Description,
                f.FormType,
                f.Trigger,
                f.Channel.Name
            ))
            .ToListAsync();
    }

    public async Task<FormForUserDto?> GetFormForUserAsync(Guid formId)
    {
        var form = await _db.Forms
            .Include(f => f.Questions.OrderBy(q => q.SortOrder))
                .ThenInclude(q => q.AnswerOptions.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(f => f.Id == formId && f.IsActive);

        if (form == null) return null;

        return new FormForUserDto(
            form.Id,
            form.Title,
            form.Description,
            form.FormType,
            form.Questions.Select(q => new FormQuestionForUserDto(
                q.Id,
                q.Text,
                q.Description,
                q.QuestionType,
                q.IsRequired,
                q.AnswerOptions.Select(o => new FormAnswerOptionForUserDto(o.Id, o.Text)).ToList()
            )).ToList()
        );
    }

    public async Task<FormResponseDetailDto?> SubmitFormAsync(Guid formId, Guid userId, SubmitFormDto dto, Guid tenantId)
    {
        var form = await _db.Forms
            .Include(f => f.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(f => f.Id == formId);

        if (form == null) return null;

        // Check if user already has an in-progress response
        var existingResponse = await _db.FormResponses
            .FirstOrDefaultAsync(r => r.FormId == formId && r.UserId == userId && r.Status == FormResponseStatus.InProgress);

        var response = existingResponse ?? new FormResponse
        {
            FormId = formId,
            UserId = userId,
            TenantId = tenantId
        };

        if (existingResponse == null)
            _db.FormResponses.Add(response);

        // Clear existing answers if updating
        if (existingResponse != null)
        {
            var existingAnswers = await _db.FormResponseAnswers
                .Where(a => a.ResponseId == existingResponse.Id)
                .ToListAsync();
            _db.FormResponseAnswers.RemoveRange(existingAnswers);
        }

        int totalScore = 0;
        int maxScore = 0;

        foreach (var answerDto in dto.Answers)
        {
            var question = form.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            int pointsEarned = 0;

            if (form.FormType == FormType.Quiz && question.QuestionType != QuestionType.OpenText)
            {
                var questionMaxPoints = question.AnswerOptions.Max(o => o.Points);
                maxScore += questionMaxPoints;

                if (question.QuestionType == QuestionType.SingleChoice && answerDto.SelectedOptionId.HasValue)
                {
                    var selectedOption = question.AnswerOptions.FirstOrDefault(o => o.Id == answerDto.SelectedOptionId);
                    if (selectedOption != null)
                        pointsEarned = selectedOption.Points;
                }
                else if (question.QuestionType == QuestionType.MultipleChoice && answerDto.SelectedOptionIds?.Any() == true)
                {
                    foreach (var optionId in answerDto.SelectedOptionIds)
                    {
                        var selectedOption = question.AnswerOptions.FirstOrDefault(o => o.Id == optionId);
                        if (selectedOption != null)
                            pointsEarned += selectedOption.Points;
                    }
                }

                totalScore += pointsEarned;
            }

            var answer = new FormResponseAnswer
            {
                ResponseId = response.Id,
                QuestionId = answerDto.QuestionId,
                TextAnswer = answerDto.TextAnswer,
                SelectedOptionId = answerDto.SelectedOptionId,
                SelectedOptionIds = answerDto.SelectedOptionIds != null ? string.Join(",", answerDto.SelectedOptionIds) : null,
                PointsEarned = pointsEarned,
                TenantId = tenantId
            };

            _db.FormResponseAnswers.Add(answer);
        }

        response.Status = FormResponseStatus.Completed;
        response.CompletedAt = DateTime.UtcNow;

        if (form.FormType == FormType.Quiz)
        {
            response.Score = totalScore;
            response.MaxScore = maxScore;

            if (form.PassingScore.HasValue && maxScore > 0)
            {
                var percentage = (double)totalScore / maxScore * 100;
                response.Passed = percentage >= form.PassingScore.Value;
            }
        }

        await _db.SaveChangesAsync();

        // Reload with all navigation properties
        return await GetFormResponseAsync(response.Id);
    }

    public async Task<FormResponseDetailDto?> GetUserFormResultAsync(Guid responseId, Guid userId)
    {
        var response = await _db.FormResponses
            .Include(r => r.Form)
            .Include(r => r.User)
            .Include(r => r.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.AnswerOptions)
            .Include(r => r.Answers)
                .ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(r => r.Id == responseId && r.UserId == userId);

        if (response == null) return null;

        return MapToResponseDetail(response);
    }

    public async Task<bool> HasUserCompletedFormAsync(Guid formId, Guid userId)
    {
        return await _db.FormResponses
            .AnyAsync(r => r.FormId == formId && r.UserId == userId && r.Status == FormResponseStatus.Completed);
    }

    private FormResponseDetailDto MapToResponseDetail(FormResponse response)
    {
        return new FormResponseDetailDto(
            response.Id,
            response.FormId,
            response.Form.Title,
            response.Form.FormType,
            response.UserId,
            response.User.Email,
            response.Status,
            response.Score,
            response.MaxScore,
            response.Passed,
            response.CreatedAt,
            response.CompletedAt,
            response.Answers.Select(a =>
            {
                var maxPoints = a.Question.AnswerOptions.Any() ? a.Question.AnswerOptions.Max(o => o.Points) : 0;
                bool? isCorrect = null;

                if (response.Form.FormType == FormType.Quiz && a.Question.QuestionType != QuestionType.OpenText)
                {
                    if (a.Question.QuestionType == QuestionType.SingleChoice)
                    {
                        isCorrect = a.SelectedOption?.IsCorrect;
                    }
                    else if (a.Question.QuestionType == QuestionType.MultipleChoice)
                    {
                        var selectedIds = a.SelectedOptionIds?.Split(',').Select(Guid.Parse).ToList() ?? new List<Guid>();
                        var correctIds = a.Question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
                        isCorrect = selectedIds.OrderBy(x => x).SequenceEqual(correctIds.OrderBy(x => x));
                    }
                }

                List<Guid>? selectedOptionIds = null;
                List<string>? selectedOptionTexts = null;

                if (!string.IsNullOrEmpty(a.SelectedOptionIds))
                {
                    selectedOptionIds = a.SelectedOptionIds.Split(',').Select(Guid.Parse).ToList();
                    selectedOptionTexts = a.Question.AnswerOptions
                        .Where(o => selectedOptionIds.Contains(o.Id))
                        .Select(o => o.Text)
                        .ToList();
                }

                return new FormResponseAnswerDto(
                    a.Id,
                    a.QuestionId,
                    a.Question.Text,
                    a.Question.QuestionType,
                    a.TextAnswer,
                    a.SelectedOptionId,
                    a.SelectedOption?.Text,
                    selectedOptionIds,
                    selectedOptionTexts,
                    a.PointsEarned,
                    maxPoints,
                    isCorrect
                );
            }).ToList()
        );
    }
}
