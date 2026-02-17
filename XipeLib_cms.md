# Content Management System

> Canales, Cursos, Capítulos y Contenido con control de acceso por roles

**Version:** 1.0.0  
**Author:** Xipe  
**Category:** Content  
**Installed:** 2026-01-31 16:32

## Archivos Creados

### Pow.Domain

| Archivo | Descripción |
|---------|-------------|
| `Entities/Channel.cs` | Entidad de dominio |
| `Entities/Course.cs` | Entidad de dominio |
| `Entities/Chapter.cs` | Entidad de dominio |
| `Entities/Content.cs` | Entidad de dominio |
| `Entities/ChannelAdmin.cs` | Entidad de dominio |
| `Entities/ContentView.cs` | Entidad de dominio |
| `DTOs/CMSDtos.cs` | Data Transfer Objects |
| `Enums/ContentAccessType.cs` | Archivo generado |

### Pow.Api

| Archivo | Descripción |
|---------|-------------|
| `Services/ICMSService.cs` | Interface de servicio |
| `Services/CMSService.cs` | Implementación de servicio |
| `Controllers/ContentController.cs` | Controlador API |
| `Controllers/CreatorController.cs` | Controlador API |
| `Controllers/CMSAdminController.cs` | Controlador API |
| `Data/CMSSeeder.cs` | Datos iniciales |

### Pow.Web

| Archivo | Descripción |
|---------|-------------|
| `Components/Pages/Content/Read.razor` | Página Blazor |
| `Components/Pages/Content/Index.razor` | Página Blazor |
| `Components/Pages/Creator/Index.razor` | Página Blazor |
| `Components/Pages/Creator/Courses.razor` | Página Blazor |
| `Components/Pages/Creator/Chapters.razor` | Página Blazor |
| `Components/Pages/Creator/ContentEditor.razor` | Página Blazor |
| `Components/Pages/Admin/CMSChannels.razor` | Página Blazor |
| `Components/Shared/ContentCard.razor` | Archivo generado |
| `Components/Shared/ShareButtons.razor` | Archivo generado |
| `Components/Shared/InputDialog.razor` | Componente de diálogo |
| `Services/ApiService.CMS.cs` | Implementación de servicio |

## API Endpoints

### Content

Base: `api/content`

Ver el archivo del controlador para endpoints específicos.

### Creator

Base: `api/creator`

Ver el archivo del controlador para endpoints específicos.

### CMSAdmin

Base: `api/cmsadmin`

Ver el archivo del controlador para endpoints específicos.

## Páginas

| Ruta | Archivo |
|------|---------|
| `/content/read` | `Components/Pages/Content/Read.razor` |
| `/content` | `Components/Pages/Content/Index.razor` |
| `/creator` | `Components/Pages/Creator/Index.razor` |
| `/creator/courses` | `Components/Pages/Creator/Courses.razor` |
| `/creator/chapters` | `Components/Pages/Creator/Chapters.razor` |
| `/creator/contenteditor` | `Components/Pages/Creator/ContentEditor.razor` |
| `/admin/cmschannels` | `Components/Pages/Admin/CMSChannels.razor` |

## Modificaciones a Archivos Existentes

### Pow.Api

- **Data/AppDbContext.cs**: Inserción en marker `// XipeLib:DbSets`
- **Program.cs**: Inserción en marker `// XipeLib:Services`
- **Program.cs**: Inserción en marker `// XipeLib:Seed`

### Pow.Web

- **Components/Layout/MainLayout.razor**: Inserción en marker `<!-- XipeLib:NavItems -->`
- **Components/Layout/MainLayout.razor**: Inserción en marker `<!-- XipeLib:AdminNavItems -->`
- **Components/Layout/MainLayout.razor**: Inserción en marker `<!-- XipeLib:PublicNavItems -->`
- **Components/Pages/Admin/Index.razor**: Inserción en marker `<!-- XipeLib:AdminCards -->`

## Pasos Post-Instalación

- [ ] Run: dotnet ef migrations add AddCMS
- [ ] Run: dotnet ef database update
- [ ] Default channel 'General' created automatically
- [ ] Access Creator panel at /creator
- [ ] Manage channels at /admin/cms/channels (SuperAdmin only)

## Uso

Este módulo fue instalado automáticamente por Diana. 
Consulta los archivos generados para más detalles sobre la implementación.

