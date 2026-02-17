namespace Pow.Domain.DTOs;

// ===== PLAN DTOs =====
public record PlanDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Interval,
    List<string> Features,
    int MaxUsers,
    bool IsRecommended,
    bool IsActive,
    int SortOrder
);

public record CreatePlanDto(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Interval,
    List<string>? Features,
    int MaxUsers,
    bool IsRecommended
);

public record UpdatePlanDto(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    string Interval,
    List<string>? Features,
    int MaxUsers,
    bool IsActive,
    bool IsRecommended,
    int SortOrder
);

// ===== ROLE DTOs =====
public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    List<string> Permissions
);

public record CreateRoleDto(
    string Name,
    string? Description,
    List<Guid>? PermissionIds
);

public record UpdateRoleDto(
    string Name,
    string? Description,
    List<Guid>? PermissionIds
);

// ===== PERMISSION DTOs =====
public record PermissionDto(
    Guid Id,
    string Name,
    string? Description,
    string? Category
);

// ===== USER MEMBERSHIP DTOs =====
public record UserMembershipDto(
    Guid UserId,
    string Email,
    PlanDto? CurrentPlan,
    List<RoleDto> Roles,
    DateTime? PlanExpiry
);

public record AssignPlanDto(Guid UserId, Guid PlanId);
public record AssignRoleDto(Guid UserId, Guid RoleId);

// ===== ADMIN STATS =====
public record MembershipStatsDto(
    int TotalUsers,
    int ActiveSubscriptions,
    Dictionary<string, int> UsersByPlan,
    Dictionary<string, int> UsersByRole
);
