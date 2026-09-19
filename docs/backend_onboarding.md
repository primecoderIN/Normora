# Backend Onboarding & .NET Course

Welcome to the Normora backend! This guide is tailored for developers who might be coming from Node.js, Go, or Python, and need to quickly understand the architectural patterns used in this **.NET 10 Modular Monolith**.

---

## 1. The Modular Monolith Architecture

Normora is deployed as a single API process, but internally it is strictly divided into bounded contexts (Modules) in the `server/Modules/` directory.

**Rules of the Modular Monolith:**
1. **No Shared Database**: Each module (`Tenants`, `Documents`, `Conversations`, `Users`) has its own `DbContext` and its own schema in PostgreSQL.
2. **No Direct Joins**: You cannot write a LINQ query that joins a `Document` with a `Tenant`. You must query by IDs across boundaries.
3. **Cross-Module Communication**: If the `Documents` module needs to know if a user has access, it doesn't query the `TenantsDbContext`. It sends a MediatR query to the `Tenants` module's API contract.

---

## 2. MediatR and CQRS

We use the **Command Query Responsibility Segregation (CQRS)** pattern heavily, facilitated by the `MediatR` library. 

Instead of bloated "Service" classes, every action is a standalone class.
- **Commands**: Modify state (e.g., `CreateDocumentCommand`). They return `void` or the ID of the created entity.
- **Queries**: Read state (e.g., `GetConversationQuery`). They do not modify the database.

### Example: A Request Lifecycle

1. **The Controller**: Receives the HTTP request and immediately dispatches it.
```csharp
[HttpPost]
[RequireTenant("employer")]
public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command)
{
    var id = await _mediator.Send(command);
    return Ok(new ApiResponse<Guid>(id));
}
```

2. **The Handler**: The actual business logic lives here.
```csharp
public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Guid>
{
    private readonly TenantsDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateDepartmentCommandHandler(TenantsDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext; // Automatically resolved from headers
    }

    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var dept = new Department { Name = request.Name, TenantId = _tenantContext.TenantId };
        _db.Departments.Add(dept);
        await _db.SaveChangesAsync(ct);
        return dept.Id;
    }
}
```

---

## 3. Authentication & BFF

Normora uses the **Backend-For-Frontend (BFF)** pattern powered by `Duende.BFF`.
1. **No JWTs in the Browser**: The Angular SPA does not receive access tokens.
2. **Encrypted Cookies**: The API exchanges the OIDC code with Keycloak and issues a secure `__Host-spa` cookie containing the encrypted token payload.
3. **Anti-Forgery (CSRF)**: All mutating requests (`POST`, `PUT`, `DELETE`) require an `X-CSRF: 1` header, which is enforced globally by the `AsBffApiEndpoint()` convention in `Program.cs`.

### Docker Reverse-Proxy Gotcha
When running in Docker, the API sits behind an nginx reverse proxy. The API container's internal hostname is `api:8080`, but the browser-facing public URL is `localhost:4200`. 

If not corrected, the OIDC middleware will build an `redirect_uri` pointing to `api:8080/signin-oidc`, which Keycloak will reject as an unregistered URI.

**Fix**: `UseForwardedHeaders` is registered early in `Program.cs`:
```csharp
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor 
                     | ForwardedHeaders.XForwardedHost 
                     | ForwardedHeaders.XForwardedProto
};
// IMPORTANT: By default, ASP.NET Core only trusts loopback proxies (127.0.0.1).
// In Docker, nginx runs on an internal bridge network (172.x.x.x) which is NOT loopback.
// Without clearing these lists, the X-Forwarded-Host header is silently ignored
// and redirect_uri is built from the internal Docker hostname (api:8080), not localhost:4200.
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);
```

nginx injects `proxy_set_header X-Forwarded-Host $host;` so the API correctly reads `localhost:4200` as the public host when building OIDC URLs.

> **Rule of thumb**: Always register `UseForwardedHeaders` **before** `UseAuthentication` and `UseBff` in any Docker/reverse-proxy deployment. Always clear `KnownNetworks` and `KnownProxies` when nginx is in a Docker network.

---

## 4. Security: BOLA & BFLA Prevention

We strictly prevent Broken Object Level Authorization (BOLA) and Broken Function Level Authorization (BFLA) using context injection.

### `ITenantContext`
Never trust the `TenantId` sent in a JSON body. The `TenantResolutionMiddleware` intercepts the `X-Tenant-Id` header, verifies the user is actually a member of that tenant in the database, and injects the `ITenantContext` into the DI container.

### `[RequireTenant]` Attribute
Controllers use this to enforce BFLA. 
```csharp
[RequireTenant("employer")] // Only Employers in the current tenant can call this
public async Task<IActionResult> DeleteDocument(Guid id) ...
```

---

## 4. Entity Framework Core (EF Core)

We use EF Core as our ORM. 

### Global Query Filters
We use global filters to enforce soft deletes and tenant isolation at the database level.
```csharp
// In TenantsDbContext.cs
builder.Entity<Tenant>().HasQueryFilter(t => !t.IsDeleted);
```

### Migrations
Because we have multiple `DbContexts`, you must specify the context when creating migrations:
```bash
dotnet ef migrations add AddUserGroups --context TenantsDbContext --project Modules/Normora.Modules.Tenants
dotnet ef database update --context TenantsDbContext
```

---

## 5. Dependency Injection (DI)

.NET has built-in DI. Services are registered in the `Program.cs` or in extension methods (e.g., `ApplicationServiceExtensions.cs`).

- `AddTransient`: Created every time they are requested.
- `AddScoped`: Created once per HTTP request (Use this for DbContexts and tenant-aware services).
- `AddSingleton`: Created once for the lifetime of the application.
