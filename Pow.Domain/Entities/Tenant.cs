namespace Pow.Domain.Entities;

/// <summary>
/// Tenant entity - root entity for multi-tenancy
/// All other entities belong to a Tenant
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
