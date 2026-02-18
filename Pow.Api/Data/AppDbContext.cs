using Microsoft.EntityFrameworkCore;
using Pow.Domain.Entities;

namespace Pow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    // XipeLib:DbSets
    public DbSet<Plan> Plans { get; set; }
    public DbSet<UserPlan> UserPlans { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<ChannelPlan> ChannelPlans { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Content> Contents { get; set; }
    public DbSet<ChannelAdmin> ChannelAdmins { get; set; }
    public DbSet<ChannelSubscription> ChannelSubscriptions { get; set; }
    public DbSet<ContentView> ContentViews { get; set; }
    public DbSet<ContentAttachment> ContentAttachments { get; set; }
    public DbSet<StripeWebhookEvent> StripeWebhookEvents { get; set; }
    public DbSet<CoursePurchase> CoursePurchases { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
    public DbSet<ChannelMessage> ChannelMessages { get; set; }
    public DbSet<ChannelEvent> ChannelEvents { get; set; }
    public DbSet<ChannelFollow> ChannelFollows { get; set; }
    public DbSet<Form> Forms { get; set; }
    public DbSet<FormQuestion> FormQuestions { get; set; }
    public DbSet<FormAnswerOption> FormAnswerOptions { get; set; }
    public DbSet<FormResponse> FormResponses { get; set; }
    public DbSet<FormResponseAnswer> FormResponseAnswers { get; set; }
    public DbSet<ContentRead> ContentReads { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("User");

            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Users)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChannelPlan configuration
        modelBuilder.Entity<ChannelPlan>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Channel)
                  .WithMany(c => c.Plans)
                  .HasForeignKey(e => e.ChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChannelSubscription configuration
        modelBuilder.Entity<ChannelSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ChannelId, e.ChannelPlanId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ChannelSubscriptions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Channel)
                  .WithMany(c => c.Subscriptions)
                  .HasForeignKey(e => e.ChannelId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ChannelPlan)
                  .WithMany(p => p.Subscriptions)
                  .HasForeignKey(e => e.ChannelPlanId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ChannelAdmin configuration
        modelBuilder.Entity<ChannelAdmin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ChannelId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ChannelAdmins)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Channel)
                  .WithMany(c => c.ChannelAdmins)
                  .HasForeignKey(e => e.ChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Content configuration
        modelBuilder.Entity<Content>(entity =>
        {
            entity.HasMany(e => e.Attachments)
                  .WithOne(a => a.Content)
                  .HasForeignKey(a => a.ContentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentAttachment configuration
        modelBuilder.Entity<ContentAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(100);
        });

        // CoursePurchase configuration
        modelBuilder.Entity<CoursePurchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                  .WithMany()
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ChannelFollow configuration
        modelBuilder.Entity<ChannelFollow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ChannelId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ChannelFollows)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Channel)
                  .WithMany(c => c.Followers)
                  .HasForeignKey(e => e.ChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Form configuration
        modelBuilder.Entity<Form>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Channel)
                  .WithMany()
                  .HasForeignKey(e => e.ChannelId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TriggerCourse)
                  .WithMany()
                  .HasForeignKey(e => e.TriggerCourseId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Questions)
                  .WithOne(q => q.Form)
                  .HasForeignKey(q => q.FormId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Responses)
                  .WithOne(r => r.Form)
                  .HasForeignKey(r => r.FormId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FormQuestion configuration
        modelBuilder.Entity<FormQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasMany(e => e.AnswerOptions)
                  .WithOne(o => o.Question)
                  .HasForeignKey(o => o.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ResponseAnswers)
                  .WithOne(a => a.Question)
                  .HasForeignKey(a => a.QuestionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // FormAnswerOption configuration
        modelBuilder.Entity<FormAnswerOption>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // FormResponse configuration
        modelBuilder.Entity<FormResponse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FormId, e.UserId });

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Answers)
                  .WithOne(a => a.Response)
                  .HasForeignKey(a => a.ResponseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // FormResponseAnswer configuration
        modelBuilder.Entity<FormResponseAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.SelectedOption)
                  .WithMany()
                  .HasForeignKey(e => e.SelectedOptionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ContentRead configuration
        modelBuilder.Entity<ContentRead>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ContentId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Content)
                  .WithMany()
                  .HasForeignKey(e => e.ContentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
