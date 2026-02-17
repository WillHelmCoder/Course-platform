namespace Pow.Domain.Entities;

/// <summary>
/// Site-wide settings like Terms, Privacy Policy, etc.
/// </summary>
public class SiteSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
