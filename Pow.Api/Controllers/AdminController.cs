using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pow.Api.Services;
using Pow.Domain.DTOs;

namespace Pow.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminController : ControllerBase
{
    private readonly IMembershipService _membership;

    public AdminController(IMembershipService membership)
    {
        _membership = membership;
    }

    // ===== DASHBOARD =====

    [HttpGet("stats")]
    public async Task<ActionResult<MembershipStatsDto>> GetStats()
    {
        return await _membership.GetStatsAsync();
    }

    // ===== PLAN MANAGEMENT =====

    [HttpGet("plans")]
    public async Task<ActionResult<List<PlanDto>>> GetPlans()
    {
        return await _membership.GetAllPlansAsync();
    }

    [HttpGet("plans/{id}")]
    public async Task<ActionResult<PlanDto>> GetPlan(Guid id)
    {
        var plan = await _membership.GetPlanAsync(id);
        if (plan == null) return NotFound();
        return plan;
    }

    [HttpPost("plans")]
    public async Task<ActionResult<PlanDto>> CreatePlan([FromBody] CreatePlanDto dto)
    {
        var plan = await _membership.CreatePlanAsync(dto);
        return CreatedAtAction(nameof(GetPlan), new { id = plan.Id }, plan);
    }

    [HttpPut("plans/{id}")]
    public async Task<ActionResult<PlanDto>> UpdatePlan(Guid id, [FromBody] UpdatePlanDto dto)
    {
        var plan = await _membership.UpdatePlanAsync(id, dto);
        if (plan == null) return NotFound();
        return plan;
    }

    [HttpDelete("plans/{id}")]
    public async Task<ActionResult> DeletePlan(Guid id)
    {
        var success = await _membership.DeletePlanAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    // ===== ROLE MANAGEMENT =====

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        return await _membership.GetRolesAsync();
    }

    [HttpGet("roles/{id}")]
    public async Task<ActionResult<RoleDto>> GetRole(Guid id)
    {
        var role = await _membership.GetRoleAsync(id);
        if (role == null) return NotFound();
        return role;
    }

    [HttpPost("roles")]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        var role = await _membership.CreateRoleAsync(dto);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
    }

    [HttpPut("roles/{id}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
    {
        var role = await _membership.UpdateRoleAsync(id, dto);
        if (role == null) return NotFound();
        return role;
    }

    [HttpDelete("roles/{id}")]
    public async Task<ActionResult> DeleteRole(Guid id)
    {
        var success = await _membership.DeleteRoleAsync(id);
        if (!success) return BadRequest("Cannot delete system role");
        return NoContent();
    }

    // ===== PERMISSION MANAGEMENT =====

    [HttpGet("permissions")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        return await _membership.GetPermissionsAsync();
    }

    // ===== USER MANAGEMENT =====

    [HttpGet("users")]
    public async Task<ActionResult<List<UserMembershipDto>>> GetUsers()
    {
        return await _membership.GetUsersWithMembershipAsync();
    }

    [HttpPost("users/{userId}/roles/{roleId}")]
    public async Task<ActionResult> AssignRole(Guid userId, Guid roleId)
    {
        var success = await _membership.AssignRoleAsync(userId, roleId);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpDelete("users/{userId}/roles/{roleId}")]
    public async Task<ActionResult> RemoveRole(Guid userId, Guid roleId)
    {
        var success = await _membership.RemoveRoleAsync(userId, roleId);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("users/{userId}/plan")]
    public async Task<ActionResult> AssignPlan(Guid userId, [FromBody] AssignPlanDto dto)
    {
        var success = await _membership.SelectPlanAsync(userId, dto.PlanId);
        if (!success) return BadRequest("Plan not found");
        return Ok();
    }
}
