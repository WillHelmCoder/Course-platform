using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;
using Pow.Domain.Enums;

namespace Pow.Api.Services;

public class CMSService : ICMSService
{
    private readonly AppDbContext _db;

    public CMSService(AppDbContext db)
    {
        _db = db;
    }

    // ===== CHANNELS =====

    public async Task<List<ChannelDto>> GetChannelsAsync()
    {
        return await _db.Channels
            .OrderBy(c => c.SortOrder)
            .Select(c => new ChannelDto(
                c.Id, c.Name, c.Description, c.Slug, c.MainPicture, c.IsActive, c.SortOrder,
                c.Courses.Count,
                c.Courses.SelectMany(co => co.Chapters).SelectMany(ch => ch.Contents).Count(),
                c.InboxVisibility
            ))
            .ToListAsync();
    }

    public async Task<List<ChannelDto>> GetMyChannelsAsync(Guid userId)
    {
        return await _db.ChannelAdmins
            .Where(ca => ca.UserId == userId)
            .Select(ca => new ChannelDto(
                ca.Channel.Id, ca.Channel.Name, ca.Channel.Description,
                ca.Channel.Slug, ca.Channel.MainPicture, ca.Channel.IsActive, ca.Channel.SortOrder,
                ca.Channel.Courses.Count,
                ca.Channel.Courses.SelectMany(co => co.Chapters).SelectMany(ch => ch.Contents).Count(),
                ca.Channel.InboxVisibility
            ))
            .ToListAsync();
    }

    public async Task<ChannelDto?> GetChannelAsync(Guid id)
    {
        return await _db.Channels
            .Where(c => c.Id == id)
            .Select(c => new ChannelDto(
                c.Id, c.Name, c.Description, c.Slug, c.MainPicture, c.IsActive, c.SortOrder,
                c.Courses.Count,
                c.Courses.SelectMany(co => co.Chapters).SelectMany(ch => ch.Contents).Count(),
                c.InboxVisibility
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ChannelDto?> GetChannelBySlugAsync(string slug)
    {
        return await _db.Channels
            .Where(c => c.Slug == slug)
            .Select(c => new ChannelDto(
                c.Id, c.Name, c.Description, c.Slug, c.MainPicture, c.IsActive, c.SortOrder,
                c.Courses.Count,
                c.Courses.SelectMany(co => co.Chapters).SelectMany(ch => ch.Contents).Count(),
                c.InboxVisibility
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<List<ChannelPlanDto>> GetChannelPlansAsync(Guid channelId)
    {
        return await _db.ChannelPlans
            .Where(p => p.ChannelId == channelId)
            .OrderBy(p => p.SortOrder)
            .Select(p => new ChannelPlanDto(
                p.Id,
                p.ChannelId,
                p.Name,
                p.Description,
                p.Price,
                p.Currency,
                p.Interval,
                p.SortOrder,
                p.IsActive,
                p.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active)
            ))
            .ToListAsync();
    }

    public async Task<ChannelPlanDto> CreateChannelPlanAsync(CreateChannelPlanDto dto, Guid tenantId)
    {
        var plan = new ChannelPlan
        {
            TenantId = tenantId,
            ChannelId = dto.ChannelId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Currency = dto.Currency,
            Interval = dto.Interval,
            SortOrder = dto.SortOrder,
            IsActive = true
        };

        _db.ChannelPlans.Add(plan);
        await _db.SaveChangesAsync();

        return new ChannelPlanDto(plan.Id, plan.ChannelId, plan.Name, plan.Description,
            plan.Price, plan.Currency, plan.Interval, plan.SortOrder, plan.IsActive, 0);
    }

    public async Task<ChannelPlanDto?> UpdateChannelPlanAsync(Guid id, UpdateChannelPlanDto dto)
    {
        var plan = await _db.ChannelPlans.FindAsync(id);
        if (plan == null) return null;

        plan.Name = dto.Name;
        plan.Description = dto.Description;
        plan.Price = dto.Price;
        plan.Currency = dto.Currency;
        plan.Interval = dto.Interval;
        plan.SortOrder = dto.SortOrder;
        plan.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();

        var subscriberCount = await _db.ChannelSubscriptions
            .CountAsync(s => s.ChannelPlanId == id && s.Status == SubscriptionStatus.Active);

        return new ChannelPlanDto(plan.Id, plan.ChannelId, plan.Name, plan.Description,
            plan.Price, plan.Currency, plan.Interval, plan.SortOrder, plan.IsActive, subscriberCount);
    }

    public async Task<bool> DeleteChannelPlanAsync(Guid id)
    {
        var plan = await _db.ChannelPlans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return false;

        // Don't allow deletion if there are active subscribers
        if (plan.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active))
            return false;

        _db.ChannelPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ChannelSubscriberDto>> GetChannelSubscribersAsync(Guid channelId)
    {
        return await _db.ChannelSubscriptions
            .Where(s => s.ChannelId == channelId)
            .OrderByDescending(s => s.SubscribedAt)
            .Select(s => new ChannelSubscriberDto(
                s.Id,
                s.UserId,
                s.User.Email,
                s.ChannelPlanId,
                s.ChannelPlan.Name,
                s.SubscribedAt,
                s.ExpiresAt,
                s.Status,
                s.PricePaid,
                s.Currency
            ))
            .ToListAsync();
    }

    public async Task<ChannelSubscriberDto?> GetSubscriptionAsync(Guid subscriptionId)
    {
        return await _db.ChannelSubscriptions
            .Where(s => s.Id == subscriptionId)
            .Select(s => new ChannelSubscriberDto(
                s.Id,
                s.UserId,
                s.User.Email,
                s.ChannelPlanId,
                s.ChannelPlan.Name,
                s.SubscribedAt,
                s.ExpiresAt,
                s.Status,
                s.PricePaid,
                s.Currency
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, Guid userId)
    {
        var subscription = await _db.ChannelSubscriptions
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

        if (subscription == null)
            return false;

        subscription.Status = SubscriptionStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ChannelSubscriberDto?> UpdateSubscriptionAsync(Guid subscriptionId, UpdateSubscriptionDto dto)
    {
        var subscription = await _db.ChannelSubscriptions
            .Include(s => s.User)
            .Include(s => s.ChannelPlan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        if (subscription == null)
            return null;

        if (dto.Status.HasValue)
            subscription.Status = dto.Status.Value;

        if (dto.ExpiresAt.HasValue)
            subscription.ExpiresAt = dto.ExpiresAt.Value;

        if (dto.NewPlanId.HasValue)
        {
            var newPlan = await _db.ChannelPlans.FindAsync(dto.NewPlanId.Value);
            if (newPlan != null)
                subscription.ChannelPlanId = dto.NewPlanId.Value;
        }

        await _db.SaveChangesAsync();

        // Reload to get updated plan name
        await _db.Entry(subscription).Reference(s => s.ChannelPlan).LoadAsync();

        return new ChannelSubscriberDto(
            subscription.Id,
            subscription.UserId,
            subscription.User.Email,
            subscription.ChannelPlanId,
            subscription.ChannelPlan.Name,
            subscription.SubscribedAt,
            subscription.ExpiresAt,
            subscription.Status,
            subscription.PricePaid,
            subscription.Currency
        );
    }

    public async Task<bool> ExtendSubscriptionAsync(Guid subscriptionId, int days)
    {
        var subscription = await _db.ChannelSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            return false;

        var baseDate = subscription.ExpiresAt ?? DateTime.UtcNow;
        if (baseDate < DateTime.UtcNow)
            baseDate = DateTime.UtcNow;

        subscription.ExpiresAt = baseDate.AddDays(days);
        subscription.Status = SubscriptionStatus.Active;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ChannelDto> CreateChannelAsync(CreateChannelDto dto, Guid tenantId)
    {
        var channel = new Channel
        {
            Name = dto.Name,
            Description = dto.Description,
            Slug = GenerateSlug(dto.Name),
            TenantId = tenantId,
            IsActive = true
        };

        _db.Channels.Add(channel);
        await _db.SaveChangesAsync();

        return new ChannelDto(channel.Id, channel.Name, channel.Description,
            channel.Slug, channel.MainPicture, channel.IsActive, channel.SortOrder, 0, 0);
    }

    public async Task<ChannelDto?> UpdateChannelAsync(Guid id, UpdateChannelDto dto)
    {
        var channel = await _db.Channels.FindAsync(id);
        if (channel == null) return null;

        channel.Name = dto.Name;
        channel.Description = dto.Description;
        channel.IsActive = dto.IsActive;
        channel.SortOrder = dto.SortOrder;

        await _db.SaveChangesAsync();

        var courseCount = await _db.Courses.CountAsync(c => c.ChannelId == id);
        var contentCount = await _db.Contents.CountAsync(c => c.Chapter.Course.ChannelId == id);
        return new ChannelDto(channel.Id, channel.Name, channel.Description,
            channel.Slug, channel.MainPicture, channel.IsActive, channel.SortOrder, courseCount, contentCount);
    }

    public async Task<bool> DeleteChannelAsync(Guid id)
    {
        var channel = await _db.Channels.FindAsync(id);
        if (channel == null) return false;

        _db.Channels.Remove(channel);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== CHANNEL ADMINS =====

    public async Task<List<ChannelAdminDto>> GetChannelAdminsAsync(Guid channelId)
    {
        return await _db.ChannelAdmins
            .Where(ca => ca.ChannelId == channelId)
            .Select(ca => new ChannelAdminDto(
                ca.ChannelId, ca.Channel.Name, ca.UserId, ca.User.Email
            ))
            .ToListAsync();
    }

    public async Task<bool> AssignChannelAdminAsync(Guid channelId, Guid userId)
    {
        if (await _db.ChannelAdmins.AnyAsync(ca => ca.ChannelId == channelId && ca.UserId == userId))
            return true;

        var channel = await _db.Channels.FindAsync(channelId);
        _db.ChannelAdmins.Add(new ChannelAdmin
        {
            ChannelId = channelId,
            UserId = userId,
            TenantId = channel?.TenantId ?? Guid.Empty
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveChannelAdminAsync(Guid channelId, Guid userId)
    {
        var ca = await _db.ChannelAdmins.FirstOrDefaultAsync(
            x => x.ChannelId == channelId && x.UserId == userId);
        if (ca == null) return false;

        _db.ChannelAdmins.Remove(ca);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsChannelAdminAsync(Guid channelId, Guid userId)
    {
        return await _db.ChannelAdmins.AnyAsync(ca => ca.ChannelId == channelId && ca.UserId == userId);
    }

    // ===== COURSES =====

    public async Task<List<CourseDto>> GetCoursesAsync(Guid? channelId = null)
    {
        var query = _db.Courses.AsQueryable();
        if (channelId.HasValue)
            query = query.Where(c => c.ChannelId == channelId.Value);

        return await query
            .OrderBy(c => c.SortOrder)
            .Select(c => new CourseDto(
                c.Id, c.ChannelId, c.Channel.Name, c.Title, c.Description,
                c.MainPicture, c.Slug, c.IsActive, c.IsPublished, c.AccessType,
                c.RequiredPlanId, c.RequiredPlan != null ? c.RequiredPlan.Name : null,
                c.SortOrder,
                c.Chapters.Count,
                c.Chapters.SelectMany(ch => ch.Contents).Count(),
                c.AllowPurchase, c.Price, c.Currency
            ))
            .ToListAsync();
    }

    public async Task<List<CourseDto>> GetMyCoursesAsync(Guid userId)
    {
        var channelIds = await _db.ChannelAdmins
            .Where(ca => ca.UserId == userId)
            .Select(ca => ca.ChannelId)
            .ToListAsync();

        return await _db.Courses
            .Where(c => channelIds.Contains(c.ChannelId))
            .OrderBy(c => c.SortOrder)
            .Select(c => new CourseDto(
                c.Id, c.ChannelId, c.Channel.Name, c.Title, c.Description,
                c.MainPicture, c.Slug, c.IsActive, c.IsPublished, c.AccessType,
                c.RequiredPlanId, c.RequiredPlan != null ? c.RequiredPlan.Name : null,
                c.SortOrder,
                c.Chapters.Count,
                c.Chapters.SelectMany(ch => ch.Contents).Count(),
                c.AllowPurchase, c.Price, c.Currency
            ))
            .ToListAsync();
    }

    public async Task<CourseDto?> GetCourseAsync(Guid id)
    {
        return await _db.Courses
            .Where(c => c.Id == id)
            .Select(c => new CourseDto(
                c.Id, c.ChannelId, c.Channel.Name, c.Title, c.Description,
                c.MainPicture, c.Slug, c.IsActive, c.IsPublished, c.AccessType,
                c.RequiredPlanId, c.RequiredPlan != null ? c.RequiredPlan.Name : null,
                c.SortOrder,
                c.Chapters.Count,
                c.Chapters.SelectMany(ch => ch.Contents).Count(),
                c.AllowPurchase, c.Price, c.Currency
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<CourseDto?> GetCourseBySlugAsync(string slug)
    {
        return await _db.Courses
            .Where(c => c.Slug == slug)
            .Select(c => new CourseDto(
                c.Id, c.ChannelId, c.Channel.Name, c.Title, c.Description,
                c.MainPicture, c.Slug, c.IsActive, c.IsPublished, c.AccessType,
                c.RequiredPlanId, c.RequiredPlan != null ? c.RequiredPlan.Name : null,
                c.SortOrder,
                c.Chapters.Count,
                c.Chapters.SelectMany(ch => ch.Contents).Count(),
                c.AllowPurchase, c.Price, c.Currency
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<CourseDto> CreateCourseAsync(CreateCourseDto dto, Guid tenantId)
    {
        // Find channel and validate it exists and belongs to the tenant
        var channel = await _db.Channels
            .Where(c => c.Id == dto.ChannelId && c.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (channel == null)
            throw new InvalidOperationException($"Channel {dto.ChannelId} not found or doesn't belong to tenant {tenantId}");

        var course = new Course
        {
            ChannelId = dto.ChannelId,
            Title = dto.Title,
            Description = dto.Description,
            MainPicture = dto.MainPicture,
            Slug = GenerateSlug(dto.Title),
            AccessType = dto.AccessType,
            RequiredPlanId = dto.RequiredPlanId,
            AllowPurchase = dto.AllowPurchase,
            Price = dto.Price,
            Currency = dto.Currency,
            TenantId = tenantId
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        // Get plan name if specified
        string? planName = null;
        if (dto.RequiredPlanId.HasValue)
        {
            planName = await _db.ChannelPlans
                .Where(p => p.Id == dto.RequiredPlanId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        return new CourseDto(course.Id, course.ChannelId, channel.Name,
            course.Title, course.Description, course.MainPicture, course.Slug,
            course.IsActive, course.IsPublished, course.AccessType,
            course.RequiredPlanId, planName,
            course.SortOrder, 0, 0, course.AllowPurchase, course.Price, course.Currency);
    }

    public async Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto dto)
    {
        var course = await _db.Courses.Include(c => c.Channel).FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return null;

        course.Title = dto.Title;
        course.Description = dto.Description;
        course.MainPicture = dto.MainPicture;
        course.IsActive = dto.IsActive;
        course.IsPublished = dto.IsPublished;
        course.AccessType = dto.AccessType;
        course.RequiredPlanId = dto.RequiredPlanId;
        course.SortOrder = dto.SortOrder;
        course.AllowPurchase = dto.AllowPurchase;
        course.Price = dto.Price;
        course.Currency = dto.Currency;

        await _db.SaveChangesAsync();

        var chapterCount = await _db.Chapters.CountAsync(ch => ch.CourseId == id);
        var contentCount = await _db.Contents.CountAsync(c => c.Chapter.CourseId == id);

        // Get plan name if specified
        string? planName = null;
        if (dto.RequiredPlanId.HasValue)
        {
            planName = await _db.ChannelPlans
                .Where(p => p.Id == dto.RequiredPlanId.Value)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
        }

        return new CourseDto(course.Id, course.ChannelId, course.Channel.Name,
            course.Title, course.Description, course.MainPicture, course.Slug,
            course.IsActive, course.IsPublished, course.AccessType,
            course.RequiredPlanId, planName,
            course.SortOrder, chapterCount, contentCount, course.AllowPurchase, course.Price, course.Currency);
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course == null) return false;

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== CHAPTERS =====

    public async Task<List<ChapterDto>> GetChaptersAsync(Guid courseId)
    {
        return await _db.Chapters
            .Where(ch => ch.CourseId == courseId)
            .OrderBy(ch => ch.SortOrder)
            .Select(ch => new ChapterDto(
                ch.Id, ch.CourseId, ch.Title, ch.Description, ch.SortOrder,
                ch.Contents.Count
            ))
            .ToListAsync();
    }

    public async Task<ChapterDto?> GetChapterAsync(Guid id)
    {
        return await _db.Chapters
            .Where(ch => ch.Id == id)
            .Select(ch => new ChapterDto(
                ch.Id, ch.CourseId, ch.Title, ch.Description, ch.SortOrder,
                ch.Contents.Count
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ChapterDto> CreateChapterAsync(CreateChapterDto dto, Guid tenantId)
    {
        var chapter = new Chapter
        {
            CourseId = dto.CourseId,
            Title = dto.Title,
            Description = dto.Description,
            TenantId = tenantId
        };

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync();

        return new ChapterDto(chapter.Id, chapter.CourseId, chapter.Title,
            chapter.Description, chapter.SortOrder, 0);
    }

    public async Task<ChapterDto?> UpdateChapterAsync(Guid id, UpdateChapterDto dto)
    {
        var chapter = await _db.Chapters.FindAsync(id);
        if (chapter == null) return null;

        chapter.Title = dto.Title;
        chapter.Description = dto.Description;
        chapter.SortOrder = dto.SortOrder;

        await _db.SaveChangesAsync();

        var contentCount = await _db.Contents.CountAsync(c => c.ChapterId == id);
        return new ChapterDto(chapter.Id, chapter.CourseId, chapter.Title,
            chapter.Description, chapter.SortOrder, contentCount);
    }

    public async Task<bool> DeleteChapterAsync(Guid id)
    {
        var chapter = await _db.Chapters.FindAsync(id);
        if (chapter == null) return false;

        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== CONTENT =====

    public async Task<List<ContentListDto>> GetContentsAsync(Guid? chapterId = null)
    {
        var query = _db.Contents.AsQueryable();
        if (chapterId.HasValue)
            query = query.Where(c => c.ChapterId == chapterId.Value);

        return await query
            .OrderBy(c => c.SortOrder)
            .Select(c => new ContentListDto(
                c.Id, c.Title, c.Description, c.MainPicture, c.Slug,
                c.AccessLevel, c.ViewCount, c.PublishedAt,
                null, null, null,
                null, null, null
            ))
            .ToListAsync();
    }

    public async Task<ContentDto?> GetContentAsync(Guid id)
    {
        return await _db.Contents
            .Include(c => c.Attachments)
            .Where(c => c.Id == id)
            .Select(c => new ContentDto(
                c.Id, c.ChapterId, c.Chapter.Title, c.Chapter.Course.Title,
                c.Chapter.Course.Channel.Name, c.Title, c.Description, c.Body,
                c.MainPicture, c.VideoUrl, c.Slug, c.AccessLevel,
                c.AccessCode != null, c.IsPublished, c.SortOrder, c.ViewCount,
                c.PublishedAt, c.CreatedAt,
                c.Attachments.OrderBy(a => a.SortOrder).Select(a => new ContentAttachmentDto(
                    a.Id, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.SortOrder
                )).ToList()
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ContentDto?> GetContentBySlugAsync(string slug)
    {
        return await _db.Contents
            .Include(c => c.Attachments)
            .Where(c => c.Slug == slug)
            .Select(c => new ContentDto(
                c.Id, c.ChapterId, c.Chapter.Title, c.Chapter.Course.Title,
                c.Chapter.Course.Channel.Name, c.Title, c.Description, c.Body,
                c.MainPicture, c.VideoUrl, c.Slug, c.AccessLevel,
                c.AccessCode != null, c.IsPublished, c.SortOrder, c.ViewCount,
                c.PublishedAt, c.CreatedAt,
                c.Attachments.OrderBy(a => a.SortOrder).Select(a => new ContentAttachmentDto(
                    a.Id, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.SortOrder
                )).ToList()
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<ContentDto> CreateContentAsync(CreateContentDto dto, Guid tenantId)
    {
        var chapter = await _db.Chapters
            .Include(ch => ch.Course).ThenInclude(c => c.Channel)
            .FirstOrDefaultAsync(ch => ch.Id == dto.ChapterId);

        var content = new Content
        {
            ChapterId = dto.ChapterId,
            Title = dto.Title,
            Description = dto.Description,
            Body = dto.Body,
            MainPicture = dto.MainPicture,
            VideoUrl = dto.VideoUrl,
            Slug = GenerateSlug(dto.Title),
            AccessLevel = dto.AccessLevel,
            AccessCode = dto.AccessCode,
            TenantId = tenantId
        };

        _db.Contents.Add(content);
        await _db.SaveChangesAsync();

        return new ContentDto(content.Id, content.ChapterId,
            chapter?.Title ?? "", chapter?.Course.Title ?? "",
            chapter?.Course.Channel.Name ?? "", content.Title, content.Description,
            content.Body, content.MainPicture, content.VideoUrl, content.Slug, content.AccessLevel,
            content.AccessCode != null, content.IsPublished,
            content.SortOrder, content.ViewCount, content.PublishedAt, content.CreatedAt,
            new List<ContentAttachmentDto>());
    }

    public async Task<ContentDto?> UpdateContentAsync(Guid id, UpdateContentDto dto)
    {
        var content = await _db.Contents
            .Include(c => c.Chapter).ThenInclude(ch => ch.Course).ThenInclude(co => co.Channel)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (content == null) return null;

        content.Title = dto.Title;
        content.Description = dto.Description;
        content.Body = dto.Body;
        content.MainPicture = dto.MainPicture;
        content.VideoUrl = dto.VideoUrl;
        content.AccessLevel = dto.AccessLevel;
        content.AccessCode = dto.AccessCode;
        content.IsPublished = dto.IsPublished;
        content.SortOrder = dto.SortOrder;

        if (dto.IsPublished && content.PublishedAt == null)
            content.PublishedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new ContentDto(content.Id, content.ChapterId,
            content.Chapter.Title, content.Chapter.Course.Title,
            content.Chapter.Course.Channel.Name, content.Title, content.Description,
            content.Body, content.MainPicture, content.VideoUrl, content.Slug, content.AccessLevel,
            content.AccessCode != null, content.IsPublished, content.SortOrder, content.ViewCount,
            content.PublishedAt, content.CreatedAt,
            content.Attachments.OrderBy(a => a.SortOrder).Select(a => new ContentAttachmentDto(
                a.Id, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.SortOrder
            )).ToList());
    }

    public async Task<bool> DeleteContentAsync(Guid id)
    {
        var content = await _db.Contents.FindAsync(id);
        if (content == null) return false;

        _db.Contents.Remove(content);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PublishContentAsync(Guid id)
    {
        var content = await _db.Contents.FindAsync(id);
        if (content == null) return false;

        content.IsPublished = true;
        content.PublishedAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnpublishContentAsync(Guid id)
    {
        var content = await _db.Contents.FindAsync(id);
        if (content == null) return false;

        content.IsPublished = false;
        await _db.SaveChangesAsync();
        return true;
    }

    // ===== PUBLIC READER =====

    public async Task<ContentReaderDto?> GetContentForReaderAsync(string slug)
    {
        var content = await _db.Contents
            .Include(c => c.Chapter).ThenInclude(ch => ch.Course).ThenInclude(co => co.Channel)
                .ThenInclude(ch => ch.ChannelAdmins).ThenInclude(a => a.User)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsPublished);

        if (content == null) return null;

        // Get previous/next content
        var siblings = await _db.Contents
            .Where(c => c.ChapterId == content.ChapterId && c.IsPublished)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Title, c.Slug, c.SortOrder })
            .ToListAsync();

        var currentIndex = siblings.FindIndex(s => s.Id == content.Id);
        var previous = currentIndex > 0 ? siblings[currentIndex - 1] : null;
        var next = currentIndex < siblings.Count - 1 ? siblings[currentIndex + 1] : null;

        // Get author info (first channel admin)
        var author = content.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).FirstOrDefault();

        // Get attachments
        var attachments = content.Attachments.OrderBy(a => a.SortOrder)
            .Select(a => new ContentAttachmentDto(a.Id, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.SortOrder))
            .ToList();

        return new ContentReaderDto(
            content.Id, content.Title, content.Description, content.Body,
            content.MainPicture, content.VideoUrl, content.Slug, content.Chapter.Title,
            content.Chapter.Course.Title, content.Chapter.Course.Channel.Name,
            content.AccessLevel, content.AccessCode != null, content.PublishedAt,
            previous != null ? new ContentReaderNavDto(previous.Id, previous.Title, previous.Slug) : null,
            next != null ? new ContentReaderNavDto(next.Id, next.Title, next.Slug) : null,
            attachments,
            author?.UserId,
            author?.User.DisplayName ?? author?.User.Email,
            author?.User.ProfilePicture
        );
    }

    public async Task<List<ContentListDto>> GetPublishedContentsAsync(int take = 20, int skip = 0)
    {
        return await _db.Contents
            .Include(c => c.Chapter)
                .ThenInclude(ch => ch.Course)
                    .ThenInclude(co => co.Channel)
                        .ThenInclude(ch => ch.ChannelAdmins)
                            .ThenInclude(a => a.User)
            .Where(c => c.IsPublished
                && c.Chapter.Course.IsPublished
                && c.Chapter.Course.IsActive
                && c.Chapter.Course.Channel.IsActive)
            .OrderByDescending(c => c.PublishedAt)
            .Skip(skip).Take(take)
            .Select(c => new ContentListDto(
                c.Id, c.Title, c.Description, c.MainPicture, c.Slug,
                c.AccessLevel, c.ViewCount, c.PublishedAt,
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.UserId).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.DisplayName ?? a.User.Email).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.ProfilePicture).FirstOrDefault(),
                c.Chapter.Course.ChannelId,
                c.Chapter.Course.Channel.Name,
                c.Chapter.Course.Channel.Slug
            ))
            .ToListAsync();
    }

    public async Task<List<ContentListDto>> GetPublishedContentsByChannelAsync(string channelSlug)
    {
        return await _db.Contents
            .Include(c => c.Chapter)
                .ThenInclude(ch => ch.Course)
                    .ThenInclude(co => co.Channel)
                        .ThenInclude(ch => ch.ChannelAdmins)
                            .ThenInclude(a => a.User)
            .Where(c => c.IsPublished
                && c.Chapter.Course.IsPublished
                && c.Chapter.Course.IsActive
                && c.Chapter.Course.Channel.IsActive
                && c.Chapter.Course.Channel.Slug == channelSlug)
            .OrderByDescending(c => c.PublishedAt)
            .Select(c => new ContentListDto(
                c.Id, c.Title, c.Description, c.MainPicture, c.Slug,
                c.AccessLevel, c.ViewCount, c.PublishedAt,
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.UserId).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.DisplayName ?? a.User.Email).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.ProfilePicture).FirstOrDefault(),
                c.Chapter.Course.ChannelId,
                c.Chapter.Course.Channel.Name,
                c.Chapter.Course.Channel.Slug
            ))
            .ToListAsync();
    }

    public async Task<List<ContentListDto>> GetPublishedContentsByCourseAsync(string courseSlug)
    {
        return await _db.Contents
            .Include(c => c.Chapter)
                .ThenInclude(ch => ch.Course)
                    .ThenInclude(co => co.Channel)
                        .ThenInclude(ch => ch.ChannelAdmins)
                            .ThenInclude(a => a.User)
            .Where(c => c.IsPublished
                && c.Chapter.Course.IsPublished
                && c.Chapter.Course.IsActive
                && c.Chapter.Course.Channel.IsActive
                && c.Chapter.Course.Slug == courseSlug)
            .OrderBy(c => c.Chapter.SortOrder).ThenBy(c => c.SortOrder)
            .Select(c => new ContentListDto(
                c.Id, c.Title, c.Description, c.MainPicture, c.Slug,
                c.AccessLevel, c.ViewCount, c.PublishedAt,
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.UserId).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.DisplayName ?? a.User.Email).FirstOrDefault(),
                c.Chapter.Course.Channel.ChannelAdmins.OrderBy(a => a.Id).Select(a => a.User.ProfilePicture).FirstOrDefault(),
                c.Chapter.Course.ChannelId,
                c.Chapter.Course.Channel.Name,
                c.Chapter.Course.Channel.Slug
            ))
            .ToListAsync();
    }

    public async Task<bool> ValidateAccessCodeAsync(Guid contentId, string code)
    {
        var content = await _db.Contents.FindAsync(contentId);
        return content?.AccessCode == code;
    }

    public async Task TrackViewAsync(Guid contentId, Guid? userId, string? ipAddress, string? userAgent)
    {
        _db.ContentViews.Add(new ContentView
        {
            ContentId = contentId,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        await _db.Contents.Where(c => c.Id == contentId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ViewCount, c => c.ViewCount + 1));

        await _db.SaveChangesAsync();
    }

    // ===== STATS =====

    public async Task<CMSStatsDto> GetStatsAsync()
    {
        var viewsByChannel = await _db.Contents
            .Where(c => c.IsPublished)
            .GroupBy(c => c.Chapter.Course.Channel.Name)
            .Select(g => new { Channel = g.Key, Views = g.Sum(c => c.ViewCount) })
            .ToDictionaryAsync(x => x.Channel, x => x.Views);

        return new CMSStatsDto(
            await _db.Channels.CountAsync(),
            await _db.Courses.CountAsync(),
            await _db.Chapters.CountAsync(),
            await _db.Contents.CountAsync(),
            await _db.Contents.CountAsync(c => c.IsPublished),
            await _db.Contents.SumAsync(c => c.ViewCount),
            viewsByChannel
        );
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLower()
            .Replace(" ", "-")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n")
            + "-" + Guid.NewGuid().ToString()[..8];
    }
}
