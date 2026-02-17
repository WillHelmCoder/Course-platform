using System.ComponentModel.DataAnnotations;

namespace Pow.Domain.Entities;

public class Permission : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Category for grouping (Users, Plans, Settings, etc.)
    /// </summary>
    public string? Category { get; set; }

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
