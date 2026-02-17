using Pow.Domain.DTOs;
using Pow.Domain.Entities;

namespace Pow.Api.Services;

public interface IMembershipService
{
    // Plans
    Task<List<PlanDto>> GetPlansAsync();
    Task<List<PlanDto>> GetAllPlansAsync();
    Task<PlanDto?> GetPlanAsync(Guid id);
    Task<PlanDto> CreatePlanAsync(CreatePlanDto dto);
    Task<PlanDto?> UpdatePlanAsync(Guid id, UpdatePlanDto dto);
    Task<bool> DeletePlanAsync(Guid id);

    // User Plans
    Task<bool> SelectPlanAsync(Guid userId, Guid planId);
    Task<PlanDto?> GetUserPlanAsync(Guid userId);
    Task<bool> HasSelectedPlanAsync(Guid userId);

    // Roles
    Task<List<RoleDto>> GetRolesAsync();
    Task<RoleDto?> GetRoleAsync(Guid id);
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
    Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleDto dto);
    Task<bool> DeleteRoleAsync(Guid id);

    // Permissions
    Task<List<PermissionDto>> GetPermissionsAsync();
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task<bool> UserHasPermissionAsync(Guid userId, string permission);

    // User Management
    Task<List<UserMembershipDto>> GetUsersWithMembershipAsync();
    Task<bool> AssignRoleAsync(Guid userId, Guid roleId);
    Task<bool> RemoveRoleAsync(Guid userId, Guid roleId);

    // Stats
    Task<MembershipStatsDto> GetStatsAsync();
}
