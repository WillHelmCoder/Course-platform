using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Pow.Api.Services;
using Pow.Domain.DTOs;

namespace Pow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembershipController : ControllerBase
{
    private readonly IMembershipService _membership;

    public MembershipController(IMembershipService membership)
    {
        _membership = membership;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ===== PUBLIC ENDPOINTS =====

    /// <summary>
    /// Get available plans for selection
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PlanDto>>> GetPlans()
    {
        return await _membership.GetPlansAsync();
    }

    /// <summary>
    /// Select a plan for the current user (post-registration)
    /// </summary>
    [HttpPost("select-plan")]
    public async Task<ActionResult> SelectPlan([FromBody] SelectPlanRequest request)
    {
        var success = await _membership.SelectPlanAsync(GetUserId(), request.PlanId);
        if (!success) return BadRequest("Plan not found or inactive");
        return Ok(new { message = "Plan selected successfully" });
    }

    /// <summary>
    /// Get current user's plan
    /// </summary>
    [HttpGet("my-plan")]
    public async Task<ActionResult<PlanDto?>> GetMyPlan()
    {
        return await _membership.GetUserPlanAsync(GetUserId());
    }

    /// <summary>
    /// Check if user has selected a plan
    /// </summary>
    [HttpGet("has-plan")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> HasPlan()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim)) return false;
        return await _membership.HasSelectedPlanAsync(Guid.Parse(userIdClaim));
    }

    /// <summary>
    /// Get current user's permissions
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<ActionResult<List<string>>> GetMyPermissions()
    {
        return await _membership.GetUserPermissionsAsync(GetUserId());
    }

    /// <summary>
    /// Check if current user has a specific permission
    /// </summary>
    [HttpGet("has-permission/{permission}")]
    public async Task<ActionResult<bool>> HasPermission(string permission)
    {
        return await _membership.UserHasPermissionAsync(GetUserId(), permission);
    }
}

public record SelectPlanRequest(Guid PlanId);
