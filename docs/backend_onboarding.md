# Backend Onboarding & .NET Course

Welcome to the Normora backend! This guide is tailored for developers who might be coming from Node.js, Go, or Python, and need to quickly understand the architectural patterns used in this **.NET 10 Modular Monolith**.

---

## 1. The Modular Monolith Architecture

Normora is deployed as an API host, a separate Hangfire worker, and a one-off migration process. Business capabilities are divided into bounded contexts (Modules) in the `server/Modules/` directory.

**Rules of the Modular Monolith:**
1. **Module-Owned Persistence**: The `Tenants`, `Documents`, and `Conversations` modules each own a `DbContext` and a PostgreSQL schema. User profiles and tenant memberships currently belong to the Tenants module.
2. **No Direct Joins**: You cannot write a LINQ query that joins a `Document` with a `Tenant`. You must query by IDs across boundaries.
3. **Cross-Module Communication**: If the `Documents` module needs to know if a user has access, it doesn't query the `TenantsDbContext`. It sends a MediatR query to the `Tenants` module's API contract in `Normora.Shared.Contracts.Tenants`.
4. **100% DbContext Encapsulation**: DbContexts are strictly private to their owning module. API controllers, middleware, and SignalR hubs must **never** inject a module `DbContext`.
5. **Feature Slices Live in Modules**: All commands, queries, domain entities, background ingestion jobs, and services live in `server/Modules/<ModuleName>/Application/`. `Normora.Api` contains only protocol adapters (controllers, SignalR hubs, middleware, program startup).
6. **Decoupled Boundary Interfaces**: When infrastructure or middleware requires module operations, expose an abstraction in `Normora.Shared.Interfaces` (e.g. `ITenantResolutionService`, `IDocumentNotificationService`, `IEmailSender`, `IPaymentService`).

### Source Documentation Convention
Each hand-written source file documents its primary type with a concise XML summary. Add inline comments only for non-obvious business rules, security boundaries, retry behavior, or lifecycle decisions; generated EF migrations and snapshots remain generated and unedited.

### Module Directory & Assembly Conventions
Modules live in `server/Modules/<ModuleName>/<ModuleName>.csproj` (e.g., `server/Modules/Tenants/Tenants.csproj`).
Inside each `.csproj`, we explicitly specify:
```xml
<PropertyGroup>
  <AssemblyName>Normora.Modules.<ModuleName></AssemblyName>
  <RootNamespace>Normora.Modules.<ModuleName></RootNamespace>
</PropertyGroup>
```
**Why this is required:**
- **Clean Folder Naming**: Eliminates repetitive folder prefixes (`Modules/Tenants/` instead of `Modules/Normora.Modules.Tenants/`).
- **Binary & Snapshot Stability**: In .NET, MSBuild defaults the output assembly to the project name (`Tenants.dll`). Setting `<AssemblyName>` preserves the compiled binary name (`Normora.Modules.Tenants.dll`), ensuring that existing EF Core migration snapshots and Hangfire background job type serializers continue to resolve without type mismatches.
- **Namespace Consistency**: `<RootNamespace>` ensures new C# types created in the module default to `Normora.Modules.<ModuleName>.*`.

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

2. **The Handler**: The actual business logic lives here (using modern C# Primary Constructors for clean DI):
```csharp
public class CreateDepartmentCommandHandler(TenantsDbContext db, ITenantContext tenantContext) 
    : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var dept = new Department { Name = request.Name, TenantId = tenantContext.TenantId };
        db.Departments.Add(dept);
        await db.SaveChangesAsync(ct);
        return dept.Id;
    }
}
```

3. **Pipeline Behaviors**: Cross-cutting concerns are handled via open pipeline behaviors (e.g., `AuditBehavior<TRequest, TResponse>`). By implementing marker interfaces such as `IAuditableRequest`, commands automatically trigger structured audit event logging on completion without polluting handler business logic.

---

## 3. Authentication & BFF

Normora uses the **Backend-For-Frontend (BFF)** pattern powered by `Duende.BFF`.

```mermaid
sequenceDiagram
    autonumber
    participant Browser as Angular (Browser)
    participant API as .NET API (BFF)
    participant Keycloak as Keycloak (OIDC)

    Note over Browser, Keycloak: 1. Initiation
    Browser->>API: 1. GET /bff/login
    API->>Keycloak: 2. Generate PKCE & redirect to Keycloak

    Note over Browser, Keycloak: 2. User Authentication
    Keycloak->>Browser: 3. Displays Login / GitHub / Google Screen
    Browser->>Keycloak: 4. User enters credentials & OTP
    Keycloak->>API: 5. Redirect to /signin-oidc with an Auth Code

    Note over API, Keycloak: 3. Secure Backchannel Exchange
    API->>Keycloak: 6. API trades Auth Code for Access/Refresh Tokens (Internal Network)
    Keycloak->>API: 7. Returns Tokens to API

    Note over Browser, API: 4. Creating the Secure Session
    API->>API: 8. API encrypts tokens and stores them in a secure Cookie
    API->>Browser: 9. Returns HttpOnly, Secure, SameSite=Strict Cookie & Redirects to /
    
    Note over Browser, API: 5. Authenticated API Calls
    Browser->>API: 10. GET /api/data (Browser automatically attaches Cookie)
    API->>API: 11. API decrypts Cookie, extracts Access Token
    API->>API: 12. API uses Access Token to authorize the request
    API->>Browser: 13. Returns secure data
```

### Detailed Breakdown

#### 1. Initiation
When you click "Login" in Angular, it doesn't talk to Keycloak directly. Instead, it navigates to the .NET API at `/bff/login`. The API generates a secure challenge (PKCE) and redirects your browser to Keycloak.

#### 2. User Authentication
You land on the Keycloak login screen (`http://localhost:8080`). You enter your password, do the OTP challenge, or click GitHub. Keycloak verifies who you are.

#### 3. The Callback & Backchannel (Where the magic happens)
Once Keycloak approves you, it redirects your browser *back* to the API at `http://localhost:4200/signin-oidc` with a temporary, one-time-use **Authorization Code**.
- The API takes this code and secretly talks to Keycloak over the internal Docker network (`http://keycloak:8080`).
- The API trades the Authorization Code for your real Access Token and Refresh Token. 
- *Crucially, these tokens never touch your browser.*

#### 4. The Secure Cookie
Because your Angular app needs a way to prove it's logged in, the API takes those tokens, encrypts them, and wraps them in a highly secure **Cookie** (named `normora-auth` or `__Host-spa`).
- **HttpOnly**: JavaScript cannot read it (immune to XSS attacks).
- **Secure**: It can only be sent over HTTPS.
- **SameSite=Strict**: It can only be sent to your exact API, preventing Cross-Site Request Forgery (CSRF).

#### 5. Authenticated API Calls
From now on, whenever Angular makes an HTTP request to the API (e.g., fetching a user profile), the browser *automatically* attaches that secure cookie. 
The API decrypts the cookie, finds the Keycloak Access Token inside, validates it, and processes your request!

> [!NOTE]
> All of the "weird" configuration we did earlier (like setting `KC_HOSTNAME_URL` and creating the `DockerOidcBackchannelHandler`) was necessary to ensure Step 3 and Step 6 could happen seamlessly across both the public browser network and the internal Docker network simultaneously.

#### 6. Server-Side Session Persistence
By default, Duende BFF stores user sessions in memory. This means every time the backend container restarts, all users are logged out. To fix this, we use the official `Duende.BFF.EntityFramework` library to persist sessions in our **PostgreSQL** database. 
- You do **not** need to manually create C# models for this table. The library provides the `SessionDbContext` and the required models out of the box. 
- The EF Core migrations tool reads these built-in models and automatically generates the `UserSessions` table for us.

3. **Anti-Forgery (CSRF)**: All mutating requests (`POST`, `PUT`, `DELETE`) require an `X-CSRF: 1` header, which is enforced globally by the `AsBffApiEndpoint()` convention in `Program.cs`.

### Docker Reverse-Proxy Gotcha
When running in Docker, the API sits behind an nginx reverse proxy. The API container's internal hostname is `api:8080`, but the browser-facing public URL is `localhost:4200`. 

If not corrected, the OIDC middleware will build an `redirect_uri` pointing to `api:8080/signin-oidc`, which Keycloak will reject as an unregistered URI.

**Fix**: `UseForwardedHeaders` is registered early in `Program.cs`. Configure the exact reverse-proxy IPs in `ReverseProxy:KnownProxies`:
```csharp
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor 
                     | ForwardedHeaders.XForwardedHost 
                     | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.ForwardLimit = 1;
forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(trustedProxyIp));
app.UseForwardedHeaders(forwardedHeadersOptions);
```

The local Compose network pins the Nginx container IP and configures it as trusted. Production must configure the actual proxy IPs; the API rejects startup if the list is empty or malformed. Never clear the trusted proxy lists or trust an entire broad network.

> **Rule of thumb**: Always register `UseForwardedHeaders` **before** `UseAuthentication` and `UseBff`, and trust only known ingress proxy addresses.

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
dotnet ef migrations add AddUserGroups --context TenantsDbContext --project Modules/Tenants
dotnet ef database update --context TenantsDbContext
```

### Controlled Migrations
The API does not modify the schema during normal startup. Run migrations as a one-off release step, and wait for success before rolling out API or worker instances:
```powershell
dotnet .\server\Normora.Api\bin\Release\net10.0\Normora.Api.dll --migrate
```
The local Compose stack runs the same migration mode as a one-shot `migrator` service before starting the API and worker. Each module registers its own `IDatabaseMigrator`; the API does not directly resolve module `DbContext` types. Pending-model-change warnings remain visible and should be fixed rather than suppressed.

---

## 5. Dependency Injection (DI)

.NET has built-in DI. Services are registered in the `Program.cs` or in extension methods (e.g., `ApplicationServiceExtensions.cs`).

- `AddTransient`: Created every time they are requested.
- `AddScoped`: Created once per HTTP request (Use this for DbContexts and tenant-aware services).
- `AddSingleton`: Created once for the lifetime of the application.
