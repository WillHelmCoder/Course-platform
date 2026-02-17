using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pow.Api.Data;
using Pow.Api.Services;
using Pow.Domain.DTOs;

namespace Pow.Api.Controllers;

/// <summary>
/// Form management controller for creators
/// </summary>
[ApiController]
[Route("api/cms/creator/forms")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class FormCreatorController : ControllerBase
{
    private readonly IFormService _formService;
    private readonly ICMSService _cmsService;
    private readonly AppDbContext _db;

    public FormCreatorController(IFormService formService, ICMSService cmsService, AppDbContext db)
    {
        _formService = formService;
        _cmsService = cmsService;
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst("sub")?.Value ??
                   User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                   Guid.Empty.ToString());

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

    // ===== FORMS =====

    /// <summary>
    /// Get all forms for a channel
    /// </summary>
    [HttpGet("channels/{channelId:guid}")]
    public async Task<ActionResult<List<FormDto>>> GetForms(Guid channelId)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(channelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var forms = await _formService.GetFormsByChannelAsync(channelId);
        return Ok(forms);
    }

    /// <summary>
    /// Get a form with its questions
    /// </summary>
    [HttpGet("{formId:guid}")]
    public async Task<ActionResult<FormDetailDto>> GetForm(Guid formId)
    {
        var form = await _formService.GetFormAsync(formId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        return Ok(form);
    }

    /// <summary>
    /// Create a new form
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FormDto>> CreateForm([FromBody] CreateFormDto dto)
    {
        var isAdmin = await _cmsService.IsChannelAdminAsync(dto.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user != null)
                tenantId = user.TenantId;
        }

        try
        {
            var form = await _formService.CreateFormAsync(dto, tenantId);
            return CreatedAtAction(nameof(GetForm), new { formId = form.Id }, form);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update a form
    /// </summary>
    [HttpPut("{formId:guid}")]
    public async Task<ActionResult<FormDto>> UpdateForm(Guid formId, [FromBody] UpdateFormDto dto)
    {
        var existingForm = await _formService.GetFormAsync(formId);
        if (existingForm == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(existingForm.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var form = await _formService.UpdateFormAsync(formId, dto);
        return Ok(form);
    }

    /// <summary>
    /// Delete a form
    /// </summary>
    [HttpDelete("{formId:guid}")]
    public async Task<ActionResult> DeleteForm(Guid formId)
    {
        var existingForm = await _formService.GetFormAsync(formId);
        if (existingForm == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(existingForm.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        await _formService.DeleteFormAsync(formId);
        return NoContent();
    }

    // ===== QUESTIONS =====

    /// <summary>
    /// Add a question to a form
    /// </summary>
    [HttpPost("{formId:guid}/questions")]
    public async Task<ActionResult<FormQuestionDto>> CreateQuestion(Guid formId, [FromBody] CreateFormQuestionDto dto)
    {
        if (dto.FormId != formId)
            return BadRequest("Form ID mismatch");

        var existingForm = await _formService.GetFormAsync(formId);
        if (existingForm == null)
            return NotFound("Form not found");

        var isAdmin = await _cmsService.IsChannelAdminAsync(existingForm.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user != null)
                tenantId = user.TenantId;
        }

        try
        {
            var question = await _formService.CreateQuestionAsync(dto, tenantId);
            return Ok(question);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update a question
    /// </summary>
    [HttpPut("questions/{questionId:guid}")]
    public async Task<ActionResult<FormQuestionDto>> UpdateQuestion(Guid questionId, [FromBody] UpdateFormQuestionDto dto)
    {
        var question = await _db.FormQuestions.FindAsync(questionId);
        if (question == null)
            return NotFound();

        var form = await _formService.GetFormAsync(question.FormId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var updated = await _formService.UpdateQuestionAsync(questionId, dto);
        return Ok(updated);
    }

    /// <summary>
    /// Delete a question
    /// </summary>
    [HttpDelete("questions/{questionId:guid}")]
    public async Task<ActionResult> DeleteQuestion(Guid questionId)
    {
        var question = await _db.FormQuestions.FindAsync(questionId);
        if (question == null)
            return NotFound();

        var form = await _formService.GetFormAsync(question.FormId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        await _formService.DeleteQuestionAsync(questionId);
        return NoContent();
    }

    // ===== ANSWER OPTIONS =====

    /// <summary>
    /// Add an answer option to a question
    /// </summary>
    [HttpPost("questions/{questionId:guid}/options")]
    public async Task<ActionResult<FormAnswerOptionDto>> CreateOption(Guid questionId, [FromBody] CreateFormAnswerOptionDto dto)
    {
        if (dto.QuestionId != questionId)
            return BadRequest("Question ID mismatch");

        var question = await _db.FormQuestions.FindAsync(questionId);
        if (question == null)
            return NotFound("Question not found");

        var form = await _formService.GetFormAsync(question.FormId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var tenantId = GetTenantId();
        if (tenantId == Guid.Empty)
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user != null)
                tenantId = user.TenantId;
        }

        try
        {
            var option = await _formService.CreateAnswerOptionAsync(dto, tenantId);
            return Ok(option);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update an answer option
    /// </summary>
    [HttpPut("options/{optionId:guid}")]
    public async Task<ActionResult<FormAnswerOptionDto>> UpdateOption(Guid optionId, [FromBody] UpdateFormAnswerOptionDto dto)
    {
        var option = await _db.FormAnswerOptions.FindAsync(optionId);
        if (option == null)
            return NotFound();

        var question = await _db.FormQuestions.FindAsync(option.QuestionId);
        if (question == null)
            return NotFound();

        var form = await _formService.GetFormAsync(question.FormId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var updated = await _formService.UpdateAnswerOptionAsync(optionId, dto);
        return Ok(updated);
    }

    /// <summary>
    /// Delete an answer option
    /// </summary>
    [HttpDelete("options/{optionId:guid}")]
    public async Task<ActionResult> DeleteOption(Guid optionId)
    {
        var option = await _db.FormAnswerOptions.FindAsync(optionId);
        if (option == null)
            return NotFound();

        var question = await _db.FormQuestions.FindAsync(option.QuestionId);
        if (question == null)
            return NotFound();

        var form = await _formService.GetFormAsync(question.FormId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        await _formService.DeleteAnswerOptionAsync(optionId);
        return NoContent();
    }

    // ===== RESPONSES =====

    /// <summary>
    /// Get all responses for a form
    /// </summary>
    [HttpGet("{formId:guid}/responses")]
    public async Task<ActionResult<List<FormResponseDto>>> GetResponses(Guid formId)
    {
        var form = await _formService.GetFormAsync(formId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var responses = await _formService.GetFormResponsesAsync(formId);
        return Ok(responses);
    }

    /// <summary>
    /// Get a specific response with answers
    /// </summary>
    [HttpGet("responses/{responseId:guid}")]
    public async Task<ActionResult<FormResponseDetailDto>> GetResponse(Guid responseId)
    {
        var response = await _formService.GetFormResponseAsync(responseId);
        if (response == null)
            return NotFound();

        var form = await _formService.GetFormAsync(response.FormId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        return Ok(response);
    }

    /// <summary>
    /// Get form statistics
    /// </summary>
    [HttpGet("{formId:guid}/stats")]
    public async Task<ActionResult<FormStatsDto>> GetStats(Guid formId)
    {
        var form = await _formService.GetFormAsync(formId);
        if (form == null)
            return NotFound();

        var isAdmin = await _cmsService.IsChannelAdminAsync(form.ChannelId, GetUserId());
        if (!isAdmin && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var stats = await _formService.GetFormStatsAsync(formId);
        return Ok(stats);
    }
}
