using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pow.Api.Data;
using Pow.Api.Services;
using Pow.Domain.DTOs;

namespace Pow.Api.Controllers;

/// <summary>
/// Form controller for users to fill and view forms
/// </summary>
[ApiController]
[Route("api/cms/user/forms")]
[Authorize]
public class FormUserController : ControllerBase
{
    private readonly IFormService _formService;
    private readonly AppDbContext _db;

    public FormUserController(IFormService formService, AppDbContext db)
    {
        _formService = formService;
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst("sub")?.Value ??
                   User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                   Guid.Empty.ToString());

    private Guid GetTenantId() =>
        Guid.Parse(User.FindFirst("TenantId")?.Value ?? Guid.Empty.ToString());

    /// <summary>
    /// Get pending forms for a channel (forms user hasn't completed)
    /// </summary>
    [HttpGet("pending/{channelId:guid}")]
    public async Task<ActionResult<List<PendingFormDto>>> GetPendingForms(Guid channelId)
    {
        var userId = GetUserId();
        var forms = await _formService.GetPendingFormsAsync(userId, channelId);
        return Ok(forms);
    }

    /// <summary>
    /// Get a form for filling
    /// </summary>
    [HttpGet("{formId:guid}")]
    public async Task<ActionResult<FormForUserDto>> GetForm(Guid formId)
    {
        var form = await _formService.GetFormForUserAsync(formId);
        if (form == null)
            return NotFound();

        return Ok(form);
    }

    /// <summary>
    /// Check if user has completed a form
    /// </summary>
    [HttpGet("{formId:guid}/completed")]
    public async Task<ActionResult<bool>> HasCompletedForm(Guid formId)
    {
        var userId = GetUserId();
        var completed = await _formService.HasUserCompletedFormAsync(formId, userId);
        return Ok(completed);
    }

    /// <summary>
    /// Submit form answers
    /// </summary>
    [HttpPost("{formId:guid}/submit")]
    public async Task<ActionResult<FormResponseDetailDto>> SubmitForm(Guid formId, [FromBody] SubmitFormDto dto)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();

        if (tenantId == Guid.Empty)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
                tenantId = user.TenantId;
        }

        var result = await _formService.SubmitFormAsync(formId, userId, dto, tenantId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Get user's form result
    /// </summary>
    [HttpGet("result/{responseId:guid}")]
    public async Task<ActionResult<FormResponseDetailDto>> GetResult(Guid responseId)
    {
        var userId = GetUserId();
        var result = await _formService.GetUserFormResultAsync(responseId, userId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
