# Membership Management - XipeLib

> Gestión de planes de suscripción, roles, permisos y panel de administración para Pow.

**Instalado:** 2026-01-31

---

## Descripción

Este módulo agrega un sistema completo de membresías que incluye:

- **Planes de suscripción** - Free, Pro, Enterprise (personalizables)
- **Roles y permisos** - Sistema granular de autorización
- **Panel de administración** - Gestión centralizada
- **Flujo post-registro** - Redirección automática a selección de plan

---

## API Endpoints

### Públicos (`api/membership`)

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/membership/plans` | Lista planes activos |
| POST | `/api/membership/select-plan` | Seleccionar plan (auth requerida) |
| GET | `/api/membership/my-plan` | Obtener plan actual del usuario |
| GET | `/api/membership/has-plan` | Verificar si usuario tiene plan |
| GET | `/api/membership/my-permissions` | Listar permisos del usuario |
| GET | `/api/membership/has-permission/{name}` | Verificar permiso específico |

### Administración (`api/admin`) - Requiere rol Admin

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/admin/stats` | Estadísticas del dashboard |
| GET | `/api/admin/plans` | Lista todos los planes |
| POST | `/api/admin/plans` | Crear nuevo plan |
| PUT | `/api/admin/plans/{id}` | Actualizar plan |
| DELETE | `/api/admin/plans/{id}` | Eliminar plan (soft delete) |
| GET | `/api/admin/roles` | Lista todos los roles |
| POST | `/api/admin/roles` | Crear nuevo rol |
| PUT | `/api/admin/roles/{id}` | Actualizar rol |
| DELETE | `/api/admin/roles/{id}` | Eliminar rol |
| GET | `/api/admin/permissions` | Lista todos los permisos |
| GET | `/api/admin/users` | Lista usuarios con membresía |
| POST | `/api/admin/users/{id}/roles/{roleId}` | Asignar rol a usuario |
| DELETE | `/api/admin/users/{id}/roles/{roleId}` | Quitar rol de usuario |
| POST | `/api/admin/users/{id}/plan` | Asignar plan a usuario |

---

## Páginas Blazor

| Ruta | Descripción | Acceso |
|------|-------------|--------|
| `/plans` | Selección de plan post-registro | Autenticado |
| `/admin` | Dashboard de administración | Admin |
| `/admin/membership` | Gestión de planes, roles y usuarios | Admin |

---

## Entidades

### Plan
```csharp
public class Plan : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } // USD, EUR, MXN
    public string Interval { get; set; } // free, monthly, yearly, lifetime
    public string? Features { get; set; } // Comma-separated
    public int MaxUsers { get; set; } // 0 = unlimited
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsRecommended { get; set; }
}
```

### Role
```csharp
public class Role : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; } // Admin, User no se pueden borrar
}
```

### Permission
```csharp
public class Permission : BaseEntity
{
    public string Name { get; set; } // ej: "users.view", "plans.manage"
    public string? Description { get; set; }
    public string? Category { get; set; } // Users, Plans, Roles, Admin
}
```

---

## Datos Iniciales (Seeder)

El `MembershipSeeder` crea automáticamente:

### Permisos
- `users.view`, `users.create`, `users.edit`, `users.delete`
- `plans.view`, `plans.manage`
- `roles.view`, `roles.manage`
- `admin.access`, `admin.stats`

### Roles
- **Admin** - Todos los permisos (sistema)
- **User** - Permisos básicos (sistema)

### Planes
- **Free** - $0, features básicos
- **Pro** - $19.99/mes, features profesionales (recomendado)
- **Enterprise** - $99.99/mes, features enterprise

---

## Uso en Código

### Verificar permiso en un controlador
```csharp
[Authorize]
public class MyController : ControllerBase
{
    private readonly IMembershipService _membership;

    public async Task<IActionResult> SensitiveAction()
    {
        var userId = GetUserId();
        if (!await _membership.UserHasPermissionAsync(userId, "sensitive.action"))
            return Forbid();

        // ...
    }
}
```

### Verificar plan en Blazor
```razor
@inject ApiService Api

@code {
    protected override async Task OnInitializedAsync()
    {
        var hasPlan = await Api.HasPlanAsync();
        if (!hasPlan)
        {
            Nav.NavigateTo("/plans");
        }
    }
}
```

### Verificar permiso en Blazor
```razor
@inject ApiService Api

@if (await Api.HasPermissionAsync("admin.access"))
{
    <MudButton Href="/admin">Admin Panel</MudButton>
}
```

---

## Pasos Post-Instalación

1. [ ] Verificar que los markers existan en los archivos:
   - `Data/AppDbContext.cs` → `// XipeLib:DbSets`
   - `Program.cs` → `// XipeLib:Services` y `// XipeLib:Seed`
   - `Services/ApiService.cs` → `// XipeLib:Methods`

2. [ ] Ejecutar migraciones:
   ```bash
   cd Pow.Api
   dotnet ef migrations add AddMembership
   dotnet ef database update
   ```

3. [ ] Asignar rol Admin al primer usuario (manual o vía API)

4. [ ] Personalizar planes en `/admin/membership`

---

## Troubleshooting

### Error: "Authorization requires a cascading parameter of type Task<AuthenticationState>"
Asegúrate de que `Routes.razor` tenga el wrapper `<CascadingAuthenticationState>`.

### Los planes no aparecen
Verifica que `MembershipSeeder.SeedAsync` se esté llamando en `Program.cs` después de crear el DbContext.

### No puedo acceder a /admin
Necesitas tener el rol "Admin" asignado. Puedes hacerlo directamente en la base de datos o crear un endpoint temporal para asignarte el rol.

---

*Generado automáticamente por Diana - XipeLib v2.0.0*
