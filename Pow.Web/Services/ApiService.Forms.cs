using System.Net.Http.Json;
using Pow.Domain.DTOs;

namespace Pow.Web.Services;

// Forms API Methods - Extension for ApiService
public partial class ApiService
{
    // ===== CREATOR - FORMS =====

    public async Task<List<FormDto>> GetFormsByChannelAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/forms/channels/{channelId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<FormDto>>() ?? new();
        return new();
    }

    public async Task<FormDetailDto?> GetFormAsync(Guid formId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/forms/{formId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormDetailDto>();
        return null;
    }

    public async Task<FormDto?> CreateFormAsync(CreateFormDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync("api/cms/creator/forms", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormDto>();
        return null;
    }

    public async Task<FormDto?> UpdateFormAsync(Guid formId, UpdateFormDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/forms/{formId}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormDto>();
        return null;
    }

    public async Task<bool> DeleteFormAsync(Guid formId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/forms/{formId}");
        return response.IsSuccessStatusCode;
    }

    // ===== CREATOR - QUESTIONS =====

    public async Task<FormQuestionDto?> CreateFormQuestionAsync(Guid formId, CreateFormQuestionDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync($"api/cms/creator/forms/{formId}/questions", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormQuestionDto>();
        return null;
    }

    public async Task<FormQuestionDto?> UpdateFormQuestionAsync(Guid questionId, UpdateFormQuestionDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/forms/questions/{questionId}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormQuestionDto>();
        return null;
    }

    public async Task<bool> DeleteFormQuestionAsync(Guid questionId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/forms/questions/{questionId}");
        return response.IsSuccessStatusCode;
    }

    // ===== CREATOR - ANSWER OPTIONS =====

    public async Task<FormAnswerOptionDto?> CreateAnswerOptionAsync(Guid questionId, CreateFormAnswerOptionDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync($"api/cms/creator/forms/questions/{questionId}/options", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormAnswerOptionDto>();
        return null;
    }

    public async Task<FormAnswerOptionDto?> UpdateAnswerOptionAsync(Guid optionId, UpdateFormAnswerOptionDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PutAsJsonAsync($"api/cms/creator/forms/options/{optionId}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormAnswerOptionDto>();
        return null;
    }

    public async Task<bool> DeleteAnswerOptionAsync(Guid optionId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync($"api/cms/creator/forms/options/{optionId}");
        return response.IsSuccessStatusCode;
    }

    // ===== CREATOR - RESPONSES =====

    public async Task<List<FormResponseDto>> GetFormResponsesAsync(Guid formId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/forms/{formId}/responses");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<FormResponseDto>>() ?? new();
        return new();
    }

    public async Task<FormResponseDetailDto?> GetFormResponseDetailAsync(Guid responseId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/forms/responses/{responseId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormResponseDetailDto>();
        return null;
    }

    public async Task<FormStatsDto?> GetFormStatsAsync(Guid formId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/creator/forms/{formId}/stats");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormStatsDto>();
        return null;
    }

    // ===== USER - FORMS =====

    public async Task<List<PendingFormDto>> GetPendingFormsAsync(Guid channelId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/forms/pending/{channelId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<PendingFormDto>>() ?? new();
        return new();
    }

    public async Task<FormForUserDto?> GetFormForUserAsync(Guid formId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/forms/{formId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormForUserDto>();
        return null;
    }

    public async Task<bool> HasCompletedFormAsync(Guid formId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/forms/{formId}/completed");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<bool>();
        return false;
    }

    public async Task<FormResponseDetailDto?> SubmitFormAsync(Guid formId, SubmitFormDto dto)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync($"api/cms/user/forms/{formId}/submit", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormResponseDetailDto>();
        return null;
    }

    public async Task<FormResponseDetailDto?> GetFormResultAsync(Guid responseId)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/cms/user/forms/result/{responseId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<FormResponseDetailDto>();
        return null;
    }
}
