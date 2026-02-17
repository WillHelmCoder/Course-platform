using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pow.Api.Data;
using Pow.Domain.Entities;

namespace Pow.Api.Data;

/// <summary>
/// Seeds initial data for the application.
/// Creates a SuperAdmin user from configuration or defaults.
/// </summary>
public static class AppSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await SeedSuperAdminAsync(db, config);
    }

    private static async Task SeedSuperAdminAsync(AppDbContext db, IConfiguration config)
    {
        // Check if SuperAdmin already exists
        var superAdminEmail = config["SuperAdmin:Email"] ?? "admin@admin.com";

        if (await db.Users.AnyAsync(u => u.Email == superAdminEmail))
            return;

        // Get or create default tenant
        var tenant = await db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = "Default",
                Slug = "default",
                IsActive = true
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
        }

        var superAdminPassword = config["SuperAdmin:Password"] ?? "Admin123!";

        var superAdmin = new User
        {
            Email = superAdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(superAdminPassword),
            Role = "SuperAdmin",
            TenantId = tenant.Id,
            IsActive = true
        };

        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();
    }
}
