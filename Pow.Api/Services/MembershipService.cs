using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;

namespace Pow.Api.Services;

public class MembershipService : IMembershipService
{
    private readonly AppDbContext _db;

    public MembershipService(AppDbContext db)
    {
        _db = db;
    }

    // ===== PLANS =====

    public async Task<List<PlanDto>> GetPlansAsync()
    {
        return await _db.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => ToDto(p))
            .ToListAsync();
    }

    public async Task<List<PlanDto>> GetAllPlansAsync()
    {
        return await _db.Plans
            .OrderBy(p => p.SortOrder)
            .Select(p => ToDto(p))
            .ToListAsync();
    }

    public async Task<PlanDto?> GetPlanAsync(Guid id)
    {
        var plan = await _db.Plans.FindAsync(id);
        return plan == null ? null : ToDto(plan);
    }

    public async Task<PlanDto> CreatePlanAsync(CreatePlanDto dto)
    {
        var plan = new Plan
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Currency = dto.Currency,
            Interval = dto.Interval,
            Features = dto.Features != null ? string.Join(",", dto.Features) : null,
            MaxUsers = dto.MaxUsers,
            IsRecommended = dto.IsRecommended,
            SortOrder = await _db.Plans.CountAsync()
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync();
        return ToDto(plan);
    }

    public async Task<PlanDto?> UpdatePlanAsync(Guid id, UpdatePlanDto dto)
    {
        var plan = await _db.Plans.FindAsync(id);
        if (plan == null) return null;

        plan.Name = dto.Name;
        plan.Description = dto.Description;
        plan.Price = dto.Price;
        plan.Currency = dto.Currency;
        plan.Interval = dto.Interval;
        plan.Features = dto.Features != null ? string.Join(",", dto.Features) : null;
        plan.MaxUsers = dto.MaxUsers;
        plan.IsActive = dto.IsActive;
        plan.IsRecommended = dto.IsRecommended;
        plan.SortOrder = dto.SortOrder;

        await _db.SaveChangesAsync();
        return ToDto(plan);
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        var plan = await _db.Plans.FindAsync(id);
        if (plan == null) return false;

        // Soft delete - just deactivate
        plan.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== USER PLANS =====

    public async Task<bool> SelectPlanAsync(Guid userId, Guid planId)
    {
        var plan = await _db.Plans.FindAsync(planId);
        if (plan == null || !plan.IsActive) return false;

        // Deactivate existing plan
        var existing = await _db.UserPlans
            .Where(up => up.UserId == userId && up.Status == "active")
            .ToListAsync();

        foreach (var up in existing)
        {
            up.Status = "cancelled";
            up.EndDate = DateTime.UtcNow;
        }

        // Create new subscription
        _db.UserPlans.Add(new UserPlan
        {
            UserId = userId,
            PlanId = planId,
            StartDate = DateTime.UtcNow,
            Status = "active"
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PlanDto?> GetUserPlanAsync(Guid userId)
    {
        var userPlan = await _db.UserPlans
            .Include(up => up.Plan)
            .Where(up => up.UserId == userId && up.Status == "active")
            .FirstOrDefaultAsync();

        return userPlan?.Plan == null ? null : ToDto(userPlan.Plan);
    }

    public async Task<bool> HasSelectedPlanAsync(Guid userId)
    {
        return await _db.UserPlans
            .AnyAsync(up => up.UserId == userId && up.Status == "active");
    }

    // ===== ROLES =====

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        return await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystem,
                r.RolePermissions.Select(rp => rp.Permission!.Name).ToList()
            ))
            .ToListAsync();
    }

    public async Task<RoleDto?> GetRoleAsync(Guid id)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return null;

        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            role.RolePermissions.Select(rp => rp.Permission!.Name).ToList()
        );
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
    {
        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description,
            IsSystem = false
        };

        if (dto.PermissionIds != null)
        {
            foreach (var permId in dto.PermissionIds)
            {
                role.RolePermissions.Add(new RolePermission { PermissionId = permId });
            }
        }

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return await GetRoleAsync(role.Id) ?? throw new Exception("Failed to create role");
    }

    public async Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleDto dto)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null || role.IsSystem) return null;

        role.Name = dto.Name;
        role.Description = dto.Description;

        // Update permissions
        _db.RolePermissions.RemoveRange(role.RolePermissions);

        if (dto.PermissionIds != null)
        {
            foreach (var permId in dto.PermissionIds)
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = permId });
            }
        }

        await _db.SaveChangesAsync();
        return await GetRoleAsync(id);
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null || role.IsSystem) return false;

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== PERMISSIONS =====

    public async Task<List<PermissionDto>> GetPermissionsAsync()
    {
        return await _db.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Description, p.Category))
            .ToListAsync();
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Select(rp => rp.Permission!.Name)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string permission)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .AnyAsync(rp => rp.Permission!.Name == permission);
    }

    // ===== USER MANAGEMENT =====

    public async Task<List<UserMembershipDto>> GetUsersWithMembershipAsync()
    {
        var users = await _db.Users
            .Include(u => u.Tenant)
            .ToListAsync();

        var result = new List<UserMembershipDto>();

        foreach (var user in users)
        {
            var plan = await GetUserPlanAsync(user.Id);
            var roles = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Select(ur => new RoleDto(
                    ur.Role!.Id,
                    ur.Role.Name,
                    ur.Role.Description,
                    ur.Role.IsSystem,
                    ur.Role.RolePermissions.Select(rp => rp.Permission!.Name).ToList()
                ))
                .ToListAsync();

            var userPlan = await _db.UserPlans
                .Where(up => up.UserId == user.Id && up.Status == "active")
                .FirstOrDefaultAsync();

            result.Add(new UserMembershipDto(
                user.Id,
                user.Email,
                plan,
                roles,
                userPlan?.EndDate
            ));
        }

        return result;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId)
    {
        var exists = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (exists) return true;

        _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var userRole = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole == null) return false;

        _db.UserRoles.Remove(userRole);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== STATS =====

    public async Task<MembershipStatsDto> GetStatsAsync()
    {
        var totalUsers = await _db.Users.CountAsync();
        var activeSubscriptions = await _db.UserPlans.CountAsync(up => up.Status == "active");

        var usersByPlan = await _db.UserPlans
            .Where(up => up.Status == "active")
            .GroupBy(up => up.Plan!.Name)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Plan, x => x.Count);

        var usersByRole = await _db.UserRoles
            .GroupBy(ur => ur.Role!.Name)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count);

        return new MembershipStatsDto(totalUsers, activeSubscriptions, usersByPlan, usersByRole);
    }

    // ===== HELPERS =====

    private static PlanDto ToDto(Plan p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.Currency,
        p.Interval,
        string.IsNullOrEmpty(p.Features) ? new List<string>() : p.Features.Split(',').ToList(),
        p.MaxUsers,
        p.IsRecommended,
        p.IsActive,
        p.SortOrder
    );
}
