using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pow.Api.Data;
using Pow.Domain.Entities;

namespace Pow.Api.Data;

public static class MembershipSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await SeedPermissionsAsync(db);
        await SeedRolesAsync(db);
        await SeedPlansAsync(db);
        await SeedSuperAdminAsync(db, config);
    }

    private static async Task SeedSuperAdminAsync(AppDbContext db, IConfiguration config)
    {
        // Check if SuperAdmin user already exists
        var superAdminEmail = config["SuperAdmin:Email"] ?? "admin@admin.com";
        if (await db.Users.AnyAsync(u => u.Email == superAdminEmail)) return;

        var superAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        if (superAdminRole == null) return;

        var superAdminPassword = config["SuperAdmin:Password"] ?? "Admin123!";

        var superAdmin = new User
        {
            Email = superAdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(superAdminPassword),
            IsActive = true
        };

        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();

        // Assign SuperAdmin role
        db.UserRoles.Add(new UserRole
        {
            UserId = superAdmin.Id,
            RoleId = superAdminRole.Id
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(AppDbContext db)
    {
        if (await db.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>
        {
            // Users
            new() { Name = "users.view", Description = "View users", Category = "Users" },
            new() { Name = "users.create", Description = "Create users", Category = "Users" },
            new() { Name = "users.edit", Description = "Edit users", Category = "Users" },
            new() { Name = "users.delete", Description = "Delete users", Category = "Users" },

            // Plans
            new() { Name = "plans.view", Description = "View plans", Category = "Plans" },
            new() { Name = "plans.manage", Description = "Create/Edit/Delete plans", Category = "Plans" },

            // Roles
            new() { Name = "roles.view", Description = "View roles", Category = "Roles" },
            new() { Name = "roles.manage", Description = "Create/Edit/Delete roles", Category = "Roles" },

            // Admin
            new() { Name = "admin.access", Description = "Access admin panel", Category = "Admin" },
            new() { Name = "admin.stats", Description = "View statistics", Category = "Admin" },
        };

        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var allPermissions = await db.Permissions.ToListAsync();

        // SuperAdmin role - Full system access, cannot be deleted
        var superAdminRole = new Role
        {
            Name = "SuperAdmin",
            Description = "Super administrator with unrestricted access",
            IsSystem = true
        };
        foreach (var perm in allPermissions)
        {
            superAdminRole.RolePermissions.Add(new RolePermission { Permission = perm });
        }

        // Admin role - Administrative access
        var adminRole = new Role
        {
            Name = "Admin",
            Description = "Administrator with management access",
            IsSystem = true
        };
        var adminPermissions = allPermissions.Where(p =>
            p.Name.StartsWith("users.") ||
            p.Name.StartsWith("plans.") ||
            p.Name.StartsWith("roles.") ||
            p.Name == "admin.access" ||
            p.Name == "admin.stats");
        foreach (var perm in adminPermissions)
        {
            adminRole.RolePermissions.Add(new RolePermission { Permission = perm });
        }

        // Staff role - Limited administrative access
        var staffRole = new Role
        {
            Name = "Staff",
            Description = "Staff member with limited access",
            IsSystem = true
        };
        var staffPermissions = allPermissions.Where(p =>
            p.Name == "users.view" ||
            p.Name == "plans.view" ||
            p.Name == "admin.access");
        foreach (var perm in staffPermissions)
        {
            staffRole.RolePermissions.Add(new RolePermission { Permission = perm });
        }

        // User role - Basic user access
        var userRole = new Role
        {
            Name = "User",
            Description = "Standard user access",
            IsSystem = true
        };

        db.Roles.AddRange(superAdminRole, adminRole, staffRole, userRole);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPlansAsync(AppDbContext db)
    {
        if (await db.Plans.AnyAsync()) return;

        var plans = new List<Plan>
        {
            new()
            {
                Name = "Free",
                Description = "Get started with basic features",
                Price = 0,
                Currency = "USD",
                Interval = "free",
                Features = "Basic access,Community support,1 project",
                MaxUsers = 1,
                SortOrder = 0,
                IsRecommended = false
            },
            new()
            {
                Name = "Pro",
                Description = "For professionals and small teams",
                Price = 19.99m,
                Currency = "USD",
                Interval = "monthly",
                Features = "All Free features,Priority support,Unlimited projects,API access,Team collaboration",
                MaxUsers = 5,
                SortOrder = 1,
                IsRecommended = true
            },
            new()
            {
                Name = "Enterprise",
                Description = "For large organizations",
                Price = 99.99m,
                Currency = "USD",
                Interval = "monthly",
                Features = "All Pro features,Dedicated support,Custom integrations,SSO,Audit logs,Unlimited users",
                MaxUsers = 0, // unlimited
                SortOrder = 2,
                IsRecommended = false
            }
        };

        db.Plans.AddRange(plans);
        await db.SaveChangesAsync();
    }
}
