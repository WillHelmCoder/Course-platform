using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pow.Api.Data;

// Script para revisar y publicar contenido existente en POW
var builder = Host.CreateApplicationBuilder();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=D:\\DianaProjects\\2a6f32eb-3a7a-4fa7-a51a-8e5eb3014e47\\Pow\\Pow.Api\\Pow.db"));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

Console.WriteLine("🔍 REVISANDO CONTENIDO EXISTENTE EN POW");
Console.WriteLine("=====================================");

// Verificar canales
var channels = await db.Channels.ToListAsync();
Console.WriteLine($"\n📋 CANALES ENCONTRADOS: {channels.Count}");
foreach (var channel in channels)
{
    Console.WriteLine($"  • {channel.Name} (/{channel.Slug}) - Activo: {channel.IsActive}");
}

// Verificar cursos
var courses = await db.Courses.Include(c => c.Channel).ToListAsync();
Console.WriteLine($"\n📚 CURSOS ENCONTRADOS: {courses.Count}");
foreach (var course in courses)
{
    Console.WriteLine($"  • {course.Title} - Canal: {course.Channel?.Name} - Activo: {course.IsActive} - Publicado: {course.IsPublished}");
}

// Verificar capítulos
var chapters = await db.Chapters.Include(ch => ch.Course).ToListAsync();
Console.WriteLine($"\n📖 CAPÍTULOS ENCONTRADOS: {chapters.Count}");
foreach (var chapter in chapters)
{
    Console.WriteLine($"  • {chapter.Title} - Curso: {chapter.Course?.Title}");
}

// Verificar contenidos
var contents = await db.Contents
    .Include(c => c.Chapter)
        .ThenInclude(ch => ch.Course)
            .ThenInclude(co => co.Channel)
    .ToListAsync();

Console.WriteLine($"\n📄 CONTENIDOS ENCONTRADOS: {contents.Count}");
foreach (var content in contents)
{
    var channelName = content.Chapter?.Course?.Channel?.Name ?? "N/A";
    var courseName = content.Chapter?.Course?.Title ?? "N/A";
    var chapterName = content.Chapter?.Title ?? "N/A";

    Console.WriteLine($"  • '{content.Title}'");
    Console.WriteLine($"    Canal: {channelName} | Curso: {courseName} | Capítulo: {chapterName}");
    Console.WriteLine($"    Publicado: {content.IsPublished} | Acceso: {content.AccessType} | Views: {content.ViewCount}");

    if (content.PublishedAt.HasValue)
    {
        Console.WriteLine($"    Fecha publicación: {content.PublishedAt.Value:yyyy-MM-dd HH:mm}");
    }
    Console.WriteLine();
}

// Contar contenidos por estado de publicación
var publishedCount = contents.Count(c => c.IsPublished);
var unpublishedCount = contents.Count(c => !c.IsPublished);

Console.WriteLine($"📊 RESUMEN DE CONTENIDO:");
Console.WriteLine($"  • Publicado: {publishedCount}");
Console.WriteLine($"  • No publicado: {unpublishedCount}");

// Verificar si hay contenido que cumple los criterios para mostrarse públicamente
var publicContent = contents.Where(c =>
    c.IsPublished &&
    c.Chapter != null &&
    c.Chapter.Course != null &&
    c.Chapter.Course.IsPublished &&
    c.Chapter.Course.IsActive &&
    c.Chapter.Course.Channel != null &&
    c.Chapter.Course.Channel.IsActive).ToList();

Console.WriteLine($"\n✅ CONTENIDO VISIBLE PÚBLICAMENTE: {publicContent.Count}");
foreach (var content in publicContent)
{
    Console.WriteLine($"  • {content.Title} ({content.AccessType})");
}

// Si no hay contenido público, ofrecer solución
if (publicContent.Count == 0)
{
    Console.WriteLine("\n⚠️ PROBLEMA ENCONTRADO: No hay contenido visible públicamente");
    Console.WriteLine("\n🔧 APLICANDO CORRECCIONES...");

    int updatedCourses = 0;
    int updatedContent = 0;

    // Marcar todos los cursos como publicados y activos
    foreach (var course in courses)
    {
        if (!course.IsPublished || !course.IsActive)
        {
            course.IsPublished = true;
            course.IsActive = true;
            updatedCourses++;
            Console.WriteLine($"  ✅ Curso '{course.Title}' marcado como publicado y activo");
        }
    }

    // Marcar todo el contenido como publicado
    foreach (var content in contents)
    {
        if (!content.IsPublished)
        {
            content.IsPublished = true;
            content.PublishedAt = DateTime.UtcNow;
            updatedContent++;
            Console.WriteLine($"  ✅ Contenido '{content.Title}' marcado como publicado");
        }
    }

    if (updatedCourses > 0 || updatedContent > 0)
    {
        await db.SaveChangesAsync();
        Console.WriteLine($"\n🎉 CORRECCIONES APLICADAS:");
        Console.WriteLine($"  • Cursos actualizados: {updatedCourses}");
        Console.WriteLine($"  • Contenidos publicados: {updatedContent}");
    }
}

Console.WriteLine("\n🔗 PRUEBA ESTOS ENDPOINTS:");
Console.WriteLine("  • https://localhost:7126/api/cms/content");
Console.WriteLine("  • https://localhost:7126/api/cms/content/channels");
Console.WriteLine("  • https://localhost:7126/swagger");