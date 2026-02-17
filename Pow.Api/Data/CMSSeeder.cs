using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pow.Api.Data;
using Pow.Domain.Entities;
using Pow.Domain.Enums;

namespace Pow.Api.Data;

/// <summary>
/// Seeds CMS data for testing all access scenarios:
/// - Superadmin@demo.com (SuperAdmin) - full access
/// - Admin@demo.com (Admin) - owns a channel with plans
/// - User@demo.com (User) - subscribed to Admin's channel
/// </summary>
public static class CMSSeeder
{
    private const string SuperAdminEmail = "superadmin@demo.com";
    private const string AdminEmail = "admin@demo.com";
    private const string UserEmail = "user@demo.com";
    private const string DefaultPassword = "Demo123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Step 1: Ensure tenant exists
        var tenant = await EnsureTenantAsync(db);

        // Step 2: Create test users
        var superAdmin = await EnsureUserAsync(db, tenant.Id, SuperAdminEmail, "SuperAdmin");
        var admin = await EnsureUserAsync(db, tenant.Id, AdminEmail, "Admin");
        var user = await EnsureUserAsync(db, tenant.Id, UserEmail, "User");

        // Step 3: Create Admin's channel with plans
        var channel = await EnsureChannelAsync(db, tenant.Id, admin);

        // Step 4: Create channel plans
        var (freePlan, basicPlan, proPlan) = await EnsureChannelPlansAsync(db, tenant.Id, channel);

        // Step 5: Subscribe User to Admin's channel with Basic plan
        await EnsureSubscriptionAsync(db, tenant.Id, user, channel, basicPlan);

        // Step 6: Seed demo content
        await SeedDemoContentAsync(db, tenant.Id, channel);

        Console.WriteLine("✅ CMSSeeder: Seeding complete");
        Console.WriteLine($"   - SuperAdmin: {SuperAdminEmail} / {DefaultPassword}");
        Console.WriteLine($"   - Admin: {AdminEmail} / {DefaultPassword}");
        Console.WriteLine($"   - User: {UserEmail} / {DefaultPassword} (subscribed to {channel.Name} - {basicPlan.Name})");
    }

    private static async Task<Tenant> EnsureTenantAsync(AppDbContext db)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = "Demo",
                Slug = "demo",
                IsActive = true
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            Console.WriteLine("   Created tenant: Demo");
        }
        return tenant;
    }

    private static async Task<User> EnsureUserAsync(AppDbContext db, Guid tenantId, string email, string role)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                TenantId = tenantId,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword),
                Role = role,
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            Console.WriteLine($"   Created user: {email} ({role})");
        }
        return user;
    }

    private static async Task<Channel> EnsureChannelAsync(AppDbContext db, Guid tenantId, User admin)
    {
        var channel = await db.Channels
            .Include(c => c.ChannelAdmins)
            .FirstOrDefaultAsync(c => c.Slug == "tech-tutorials");

        if (channel == null)
        {
            channel = new Channel
            {
                TenantId = tenantId,
                Name = "Tech Tutorials",
                Description = "Learn programming, web development, and more",
                Slug = "tech-tutorials",
                MainPicture = "/images/channels/tech-tutorials.jpg",
                IsActive = true,
                SortOrder = 0,
                CreatedAt = DateTime.UtcNow
            };
            db.Channels.Add(channel);
            await db.SaveChangesAsync();
            Console.WriteLine($"   Created channel: {channel.Name}");
        }

        // Ensure admin is channel admin
        if (!channel.ChannelAdmins.Any(ca => ca.UserId == admin.Id))
        {
            var channelAdmin = new ChannelAdmin
            {
                TenantId = tenantId,
                ChannelId = channel.Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.ChannelAdmins.Add(channelAdmin);
            await db.SaveChangesAsync();
            Console.WriteLine($"   Assigned {admin.Email} as admin of {channel.Name}");
        }

        return channel;
    }

    private static async Task<(ChannelPlan free, ChannelPlan basic, ChannelPlan pro)> EnsureChannelPlansAsync(
        AppDbContext db, Guid tenantId, Channel channel)
    {
        var existingPlans = await db.ChannelPlans
            .Where(p => p.ChannelId == channel.Id)
            .ToListAsync();

        ChannelPlan? freePlan = existingPlans.FirstOrDefault(p => p.Name == "Free");
        ChannelPlan? basicPlan = existingPlans.FirstOrDefault(p => p.Name == "Basic");
        ChannelPlan? proPlan = existingPlans.FirstOrDefault(p => p.Name == "Pro");

        if (freePlan == null)
        {
            freePlan = new ChannelPlan
            {
                TenantId = tenantId,
                ChannelId = channel.Id,
                Name = "Free",
                Description = "Access to free content only",
                Price = 0,
                Currency = "USD",
                Interval = "free",
                SortOrder = 0,
                IsActive = true
            };
            db.ChannelPlans.Add(freePlan);
            Console.WriteLine($"   Created plan: Free");
        }

        if (basicPlan == null)
        {
            basicPlan = new ChannelPlan
            {
                TenantId = tenantId,
                ChannelId = channel.Id,
                Name = "Basic",
                Description = "Access to all subscriber content",
                Price = 9.99m,
                Currency = "USD",
                Interval = "monthly",
                SortOrder = 10,
                IsActive = true
            };
            db.ChannelPlans.Add(basicPlan);
            Console.WriteLine($"   Created plan: Basic ($9.99/month)");
        }

        if (proPlan == null)
        {
            proPlan = new ChannelPlan
            {
                TenantId = tenantId,
                ChannelId = channel.Id,
                Name = "Pro",
                Description = "Full access to all premium content + exclusive materials",
                Price = 29.99m,
                Currency = "USD",
                Interval = "monthly",
                SortOrder = 20,
                IsActive = true
            };
            db.ChannelPlans.Add(proPlan);
            Console.WriteLine($"   Created plan: Pro ($29.99/month)");
        }

        await db.SaveChangesAsync();
        return (freePlan, basicPlan, proPlan);
    }

    private static async Task EnsureSubscriptionAsync(
        AppDbContext db, Guid tenantId, User user, Channel channel, ChannelPlan plan)
    {
        var existing = await db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.ChannelId == channel.Id);

        if (existing == null)
        {
            var subscription = new ChannelSubscription
            {
                TenantId = tenantId,
                UserId = user.Id,
                ChannelId = channel.Id,
                ChannelPlanId = plan.Id,
                SubscribedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active,
                PricePaid = plan.Price,
                Currency = plan.Currency
            };
            db.ChannelSubscriptions.Add(subscription);
            await db.SaveChangesAsync();
            Console.WriteLine($"   Subscribed {user.Email} to {channel.Name} ({plan.Name} plan)");
        }
    }

    private static async Task SeedDemoContentAsync(AppDbContext db, Guid tenantId, Channel channel)
    {
        // Check if we already have content for this channel
        var existingCourse = await db.Courses.FirstOrDefaultAsync(c => c.ChannelId == channel.Id);
        if (existingCourse != null)
            return;

        // Get Pro plan for SpecificPlan content
        var proPlan = await db.ChannelPlans
            .FirstOrDefaultAsync(p => p.ChannelId == channel.Id && p.Name == "Pro");

        // Create demo course
        var course = new Course
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChannelId = channel.Id,
            Title = "Web Development Fundamentals",
            Description = "Complete guide to modern web development",
            Slug = "web-dev-fundamentals",
            MainPicture = "/images/course-webdev.jpg",
            IsActive = true,
            IsPublished = true,
            AccessType = CourseAccessType.Free,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        };
        db.Courses.Add(course);

        // Create demo chapter
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CourseId = course.Id,
            Title = "Getting Started",
            Description = "Introduction and setup",
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow
        };
        db.Chapters.Add(chapter);

        // Create content at different access levels
        var contents = new[]
        {
            // 1. PUBLIC - Anyone can see
            new Content
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChapterId = chapter.Id,
                Title = "Welcome to Web Development",
                Description = "Your journey starts here - completely free!",
                Body = @"# 🌐 Welcome to Web Development

This is **PUBLIC** content - anyone can view this without logging in.

## What you'll learn
- HTML basics
- CSS styling
- JavaScript fundamentals

## Why this is free
We believe everyone should have access to foundational knowledge. Start your journey here!",
                Slug = "welcome-web-dev",
                AccessLevel = ContentAccessLevel.Public,
                IsPublished = true,
                SortOrder = 0,
                ViewCount = 125,
                PublishedAt = DateTime.UtcNow.AddDays(-7),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },

            // 2. LOGGED IN - Any authenticated user
            new Content
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChapterId = chapter.Id,
                Title = "Setting Up Your Environment",
                Description = "Configure your development tools",
                Body = @"# 🔧 Setting Up Your Environment

This is **LOGGED IN** content - you need to create an account to view this.

## Tools we'll use
- VS Code
- Node.js
- Git

## Why require login?
We track your progress and personalize your learning experience.",
                Slug = "setup-environment",
                AccessLevel = ContentAccessLevel.LoggedIn,
                IsPublished = true,
                SortOrder = 1,
                ViewCount = 87,
                PublishedAt = DateTime.UtcNow.AddDays(-6),
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },

            // 3. ALL SUBSCRIBERS - Any plan subscriber
            new Content
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChapterId = chapter.Id,
                Title = "HTML Deep Dive",
                Description = "Master HTML with practical examples",
                Body = @"# 📝 HTML Deep Dive

This is **SUBSCRIBER** content - you need an active subscription (any plan) to view this.

## Topics covered
- Semantic HTML
- Forms and validation
- Accessibility best practices

## Value for subscribers
In-depth content with practical exercises and downloadable resources.",
                Slug = "html-deep-dive",
                AccessLevel = ContentAccessLevel.AllSubscribers,
                IsPublished = true,
                SortOrder = 2,
                ViewCount = 45,
                PublishedAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },

            // 4. SPECIFIC PLAN - Pro plan required
            new Content
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChapterId = chapter.Id,
                Title = "Advanced CSS Techniques",
                Description = "Pro-level styling secrets",
                Body = @"# 🎨 Advanced CSS Techniques

This is **SUBSCRIBERS ONLY** content - you need a subscription to access.

## Exclusive content
- CSS Grid mastery
- Advanced animations
- Performance optimization

## Why subscribers only?
This is our most valuable content with hours of exclusive tutorials.",
                Slug = "advanced-css",
                AccessLevel = ContentAccessLevel.AllSubscribers,
                IsPublished = true,
                SortOrder = 3,
                ViewCount = 12,
                PublishedAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },

            // 5. CODE PROTECTED - Access code required
            new Content
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChapterId = chapter.Id,
                Title = "Workshop: Building Your First App",
                Description = "Exclusive workshop content",
                Body = @"# 🎯 Workshop: Building Your First App

This is **CODE PROTECTED** content - only accessible with the access code: WORKSHOP2024

## What's included
- Live coding session recording
- Source code download
- Certificate of completion

## How to get access
This code was provided to workshop attendees. Contact us if you need access.",
                Slug = "workshop-first-app",
                AccessLevel = ContentAccessLevel.Public, // Access code overrides all
                AccessCode = "WORKSHOP2024",
                IsPublished = true,
                SortOrder = 4,
                ViewCount = 8,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        db.Contents.AddRange(contents);
        await db.SaveChangesAsync();

        Console.WriteLine($"   Created course: {course.Title}");
        Console.WriteLine($"   Created {contents.Length} content items with different access levels:");
        Console.WriteLine($"      - Public: 'Welcome to Web Development'");
        Console.WriteLine($"      - LoggedIn: 'Setting Up Your Environment'");
        Console.WriteLine($"      - AllSubscribers: 'HTML Deep Dive'");
        Console.WriteLine($"      - SpecificPlan (Pro): 'Advanced CSS Techniques'");
        Console.WriteLine($"      - CodeProtected (WORKSHOP2024): 'Workshop: Building Your First App'");
    }
}
