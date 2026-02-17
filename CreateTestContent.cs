using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pow.Api.Data;
using Pow.Domain.Entities;
using Pow.Domain.Enums;

// Script para crear contenido de prueba en POW
var builder = Host.CreateApplicationBuilder();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=D:\\DianaProjects\\2a6f32eb-3a7a-4fa7-a51a-8e5eb3014e47\\Pow\\Pow.Api\\Pow.db"));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

Console.WriteLine("🚀 CREANDO CONTENIDO DE PRUEBA PARA POW");
Console.WriteLine("=====================================");

// Obtener el tenant ID del SuperAdmin
var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@admin.com");
if (adminUser == null)
{
    Console.WriteLine("❌ No se encontró el usuario admin@admin.com");
    return;
}

var tenantId = adminUser.TenantId;

// Crear o obtener canal "General"
var generalChannel = await db.Channels.FirstOrDefaultAsync(c => c.Slug == "general");
if (generalChannel == null)
{
    generalChannel = new Channel
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = "General",
        Description = "Canal general para contenido público",
        Slug = "general",
        IsActive = true,
        SortOrder = 0,
        CreatedAt = DateTime.UtcNow
    };
    db.Channels.Add(generalChannel);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Canal 'General' creado");
}

// Crear curso de prueba
var demoCourse = await db.Courses.FirstOrDefaultAsync(c => c.Slug.StartsWith("curso-demo"));
if (demoCourse == null)
{
    demoCourse = new Course
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        ChannelId = generalChannel.Id,
        Title = "Curso Demo",
        Description = "Curso de demostración con contenido gratuito",
        Slug = "curso-demo-" + Guid.NewGuid().ToString()[..8],
        IsActive = true,
        IsPublished = true,
        SortOrder = 0,
        CreatedAt = DateTime.UtcNow
    };
    db.Courses.Add(demoCourse);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Curso 'Demo' creado");
}

// Crear capítulo
var demoChapter = await db.Chapters.FirstOrDefaultAsync(ch => ch.CourseId == demoCourse.Id);
if (demoChapter == null)
{
    demoChapter = new Chapter
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        CourseId = demoCourse.Id,
        Title = "Capítulo 1: Introducción",
        Description = "Capítulo introductorio",
        SortOrder = 0,
        CreatedAt = DateTime.UtcNow
    };
    db.Chapters.Add(demoChapter);
    await db.SaveChangesAsync();
    Console.WriteLine("✅ Capítulo creado");
}

// Crear contenidos de prueba
var existingContents = await db.Contents.CountAsync(c => c.ChapterId == demoChapter.Id);
if (existingContents == 0)
{
    var contents = new[]
    {
        new Content
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChapterId = demoChapter.Id,
            Title = "Bienvenida a POW",
            Description = "Artículo de bienvenida a la plataforma POW",
            Body = @"# ¡Bienvenido a POW!

POW es una plataforma moderna de gestión de contenido con las siguientes características:

## ✨ Características principales:
- 🎯 **Contenido estructurado**: Organizado en canales, cursos y capítulos
- 🔒 **Control de acceso**: Contenido gratuito, premium y protegido por código
- 📱 **Responsive**: Funciona perfectamente en móviles y desktop
- 🚀 **Rápido**: Construido con .NET y Blazor

## 🆓 Contenido gratuito
Este artículo es **completamente gratuito** y público para demostrar las capacidades de la plataforma.

¡Explora más contenido y descubre todo lo que POW puede ofrecerte!",
            Slug = "bienvenida-pow-" + Guid.NewGuid().ToString()[..8],
            AccessType = ContentAccessType.Free,
            IsPublished = true,
            SortOrder = 1,
            ViewCount = 0,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        },
        new Content
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChapterId = demoChapter.Id,
            Title = "Guía de navegación",
            Description = "Cómo navegar por la plataforma POW",
            Body = @"# 📖 Guía de navegación

Aprende a navegar por POW de forma eficiente:

## 🏠 Página principal
- Accede a todo el contenido público desde `/content`
- Filtra por canales usando el selector
- Busca contenido específico con la barra de búsqueda

## 🔍 Tipos de contenido
- **🆓 Gratuito**: Accesible para todos
- **💎 Premium**: Solo para suscriptores
- **🔐 Código**: Requiere código de acceso

## 📱 Interfaz
La interfaz utiliza Material Design con:
- Cards responsivas para el contenido
- Skeleton loaders mientras carga
- Navegación intuitiva entre artículos

¡Disfruta explorando el contenido!",
            Slug = "guia-navegacion-" + Guid.NewGuid().ToString()[..8],
            AccessType = ContentAccessType.Free,
            IsPublished = true,
            SortOrder = 2,
            ViewCount = 0,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        },
        new Content
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChapterId = demoChapter.Id,
            Title = "Contenido premium de ejemplo",
            Description = "Ejemplo de contenido premium (solo para demostrar)",
            Body = @"# 💎 Contenido Premium

Este es un ejemplo de contenido premium. En un entorno real, solo los usuarios con suscripción activa podrían ver este contenido.

## Características premium:
- Contenido exclusivo de mayor calidad
- Tutoriales avanzados
- Soporte prioritario
- Acceso anticipado a nuevas funcionalidades

*Nota: Este es solo contenido de demostración*",
            Slug = "contenido-premium-" + Guid.NewGuid().ToString()[..8],
            AccessType = ContentAccessType.Premium,
            IsPublished = true,
            SortOrder = 3,
            ViewCount = 0,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }
    };

    db.Contents.AddRange(contents);
    await db.SaveChangesAsync();
    Console.WriteLine($"✅ {contents.Length} contenidos creados");
}

Console.WriteLine("\n🎉 ¡CONTENIDO DE PRUEBA CREADO EXITOSAMENTE!");
Console.WriteLine($"📊 Estadísticas:");
Console.WriteLine($"  - Canales: {await db.Channels.CountAsync()}");
Console.WriteLine($"  - Cursos: {await db.Courses.CountAsync()}");
Console.WriteLine($"  - Capítulos: {await db.Chapters.CountAsync()}");
Console.WriteLine($"  - Contenidos: {await db.Contents.CountAsync()}");
Console.WriteLine($"  - Contenidos publicados: {await db.Contents.CountAsync(c => c.IsPublished)}");

Console.WriteLine("\n🔗 Prueba los endpoints:");
Console.WriteLine("  - GET http://localhost:5148/api/cms/content/channels");
Console.WriteLine("  - GET http://localhost:5148/api/cms/content");
Console.WriteLine("  - GET http://localhost:5148/swagger");