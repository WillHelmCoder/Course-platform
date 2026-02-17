# Pow

> Generado automáticamente el 2026-01-31 16:28

## Arquitectura

**Xipe Boilerplate** (`xipe-boilerplate`)

## Estructura de la Solución

```
Pow/
├── Pow.sln
├── Pow.Domain/
│   ├── Entities/
│   ├── Interfaces/
│   ├── DTOs/
├── Pow.Api/
│   ├── Data/
│   ├── Services/
│   ├── Controllers/
│   ├── Properties/
├── Pow.Web/
│   ├── Services/
│   ├── Components/Pages/Auth/
│   ├── Components/Layout/
│   ├── Properties/
│   ├── wwwroot/
│   ├── wwwroot/css/
└── documentation/
    └── README.md
```

## Proyectos

| Proyecto | Tipo | Descripción |
|----------|------|-------------|
| `Pow.Domain` | classlib | Entidades, DTOs y lógica de dominio |
| `Pow.Api` | webapi | API REST con Controllers y Services |
| `Pow.Web` | blazor | Proyecto Web |

## Configuración Inicial

### Puertos
- **HTTPS:** 7451
- **HTTP:** 5148

### Base de Datos
SQLite configurado en `Pow.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=pow.db"
  }
}
```

## Comandos Útiles

### Compilar
```bash
cd Pow
dotnet build
```

### Ejecutar API
```bash
cd Pow.Api
dotnet run
```

### Ejecutar Migraciones
```bash
cd Pow.Api
dotnet ef migrations add Initial
dotnet ef database update
```

## Flujo de Desarrollo con Diana

### 1. Crear Entidad
Crear archivo en `Pow.Domain/Entities/`:

```csharp
using Pow.Domain.Core;

namespace Pow.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? Description { get; set; }
}
```

### 2. Ejecutar Diana
Diana genera automáticamente:
- ✅ `ProductOutputDto.cs` - DTO de salida
- ✅ `ProductInputDto.cs` - DTO de entrada
- ✅ `ProductExtensions.cs` - Mapeos Entity ↔ DTO
- ✅ `IProductService.cs` - Interface del servicio
- ✅ `ProductService.cs` - Servicio CRUD (en Services/Services/)
- ✅ `ProductServiceDecorator.cs` - Decorador para lógica custom
- ✅ `ProductController.cs` - Endpoints REST
- ✅ Actualiza `AppDbContext.cs` con DbSet
- ✅ Actualiza `Program.cs` con registro de servicios

### 3. Agregar Lógica de Negocio
Editar el **Decorator** (NO el servicio generado):

```csharp
// Pow.Api/Services/Decorators/ProductServiceDecorator.cs
public async Task<ProductOutputDto> CreateAsync(ProductInputDto input)
{
    // Validación custom
    if (input.Price < 0)
        throw new ValidationException("Price cannot be negative");
    
    var result = await _inner.CreateAsync(input);
    
    // Post-procesamiento
    _logger.LogInformation("Product created: {Id}", result.Id);
    
    return result;
}
```

## Estructura de Carpetas por Proyecto

### Pow.Domain
| Carpeta | Contenido |
|---------|-----------|
| `/Core` | BaseEntity y clases base |
| `/Entities` | Entidades del negocio |
| `/DTOs` | Data Transfer Objects (generados) |
| `/Extensions` | Métodos de extensión para mapeo |

### Pow.Api
| Carpeta | Contenido |
|---------|-----------|
| `/Controllers` | Endpoints REST |
| `/Data` | DbContext |
| `/Services/Interfaces` | Contratos de servicios |
| `/Services/Services` | Servicios CRUD (NO EDITAR) |
| `/Services/Decorators` | Lógica custom (SEGURO EDITAR) |

### Pow.Web

## Notas

- Framework: `net9.0`
- Generado por: **LlmPipeline + Diana**
