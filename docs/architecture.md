# Architecture

Normora follows a **Modular Monolith** architecture on the backend and an Angular **hybrid-rendered application** on the frontend (server-rendered root route with client-rendered application routes). The API, background worker, and one-off migration runner are independently deployable processes built from the same server solution.

## High-Level Tech Stack
- **Frontend**: Angular, TypeScript, Nginx
- **Backend**: .NET 10 (ASP.NET Core Web API)
- **Database**: PostgreSQL 15 (managed via pgAdmin 4)
- **Identity Provider**: Keycloak 24 (with Google and GitHub Sign-In Integrations)
- **Object Storage**: MinIO
- **Infrastructure**: Docker & Docker Compose

The project's architectural milestones and completed phases are tracked in [Progress Tracker](../progress_tracker.md).

## Backend Architecture (Modular Monolith)
The backend is structured into distinct modules to enforce separation of concerns while keeping deployment simple.

Each module encapsulates its own persistence logic, DbContext, domain entities, jobs, and provider-neutral service contracts; host-neutral composition and external provider adapters live in `Normora.Infrastructure`. **No cross-module DbContext leakage is permitted.**

- **Normora.Api**: The HTTP/BFF host. It serves strictly as the application host, managing middleware (`TenantResolutionMiddleware`), SignalR hubs (`DocumentHub`), OIDC/BFF session management, and root dependency injection composition. All business controllers are decentralized into their respective modules (`server/Modules/{Module}/API/`). Its one-off `--migrate` mode invokes module-owned migration contracts instead of resolving module contexts.
- **Normora.Worker**: A separate generic host running Hangfire workers and scheduling recurring jobs. API replicas do not run background workers.
- **Normora.Infrastructure**: Host-neutral composition and infrastructure adapters shared by the API and worker. It depends on shared contracts and modules, but does not depend on either executable host.
- **Normora.Shared**: Shared contracts (`Normora.Shared.Contracts.*`), cross-cutting abstractions, MediatR pipeline behaviors (`AuditBehavior`), constants (`ApiMessages`, `TenantRoles`), and provider-neutral interfaces (`ITenantResolutionService`, `IDocumentNotificationService`, `IDatabaseMigrator`, `IAgentTool`, `IAgentToolRegistry`, `IEmailSender`, `IPaymentService`).
- **Modules** (`server/Modules/`):
  Each module resides in a clean directory named after its bounded context with a matching `.csproj` file. Inside each `.csproj`, `<AssemblyName>` and `<RootNamespace>` are explicitly specified as `Normora.Modules.<ModuleName>` to maintain binary stability for migration snapshots and job serializers while keeping directory structures concise:
  - `server/Modules/Tenants/Tenants.csproj` (`Normora.Modules.Tenants`): Multi-tenancy lifecycle, membership validation, departments, user profiles and groups, white-label branding, invitations, subscription plans, and isolated tenant persistence (`TenantsDbContext`). Exposes HTTP controllers in `API/`, entity mappings in `Persistence/Configurations/`, and CQRS operations in `Application/`.
  - `server/Modules/Documents/Documents.csproj` (`Normora.Modules.Documents`): Document upload/management vertical slices (`UploadDocumentCommand`, `DeleteDocumentCommand`, `GetEmployerDocumentsQuery`, `SearchDocumentsQuery`, `GetDocumentChunkQuery`), background ingestion (`DocumentProcessingJob`, `PurgeDeletedDocumentsJob`), provider-neutral extraction and embedding contracts, hybrid retrieval (`RetrievalService`), and isolated data storage (`DocumentsDbContext`). Exposes HTTP controllers in `API/`, entity mappings in `Persistence/Configurations/`, and CQRS operations in `Application/`.
  - `server/Modules/Conversations/Conversations.csproj` (`Normora.Modules.Conversations`): Multi-turn conversational RAG pipeline (`AskConversationCommand`, `AskConversationStreamCommand`), provider-neutral text generation/rewriting/suggestion contracts, autonomous agent tool calling (`AgentToolRegistry`, `CalculatePolicyLimitTool`, `EscalateInquiryTool`, `SearchCompanyDocumentsTool`), auto-titling, conversation metrics, saved answer exports (Markdown, QuestPDF, Docx), and chat history persistence (`ConversationsDbContext`). Exposes HTTP controllers in `API/`, entity mappings in `Persistence/Configurations/`, and CQRS operations in `Application/`.

### Module Architecture & CQRS Standards
Inside each module:
1. **Decentralized Controllers**: HTTP endpoints live in `server/Modules/{ModuleName}/API/` and inherit from standard ASP.NET Core `ControllerBase`.
2. **CQRS Operation Folders**: Every command and query in `Application/{Feature}/` resides in a dedicated operation folder:
   - `Commands/{OperationName}/{OperationName}Command.cs`, `{OperationName}CommandHandler.cs`, `{OperationName}CommandValidator.cs`
   - `Queries/{OperationName}/{OperationName}Query.cs`, `{OperationName}QueryHandler.cs`
   - Feature DTOs reside in `Application/{Feature}/DTOs/`
3. **Lean DbContext & Parameterless Entity Configurations**: Entity mappings are encapsulated in dedicated `IEntityTypeConfiguration<T>` classes with parameterless constructors under `server/Modules/{ModuleName}/Persistence/Configurations/` and applied automatically via `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Multi-tenant global query filters bind dynamically to the DbContext instance property (`_tenantContext.TenantId`) on each query rather than passing scoped services to configuration constructors.
4. **Composition Root**: Each module exposes an extension method `{ModuleName}ModuleExtensions.cs` to register its services and DbContext with the DI container.
5. **Host Boundary**: `Normora.Infrastructure` provides shared registrations and technology adapters used by the API and worker. The worker must not reference the API project.
6. **Background Queue Boundary**: Documents application code depends on the module-owned `IDocumentProcessingWakeup` contract and provider-neutral job handlers. Hangfire dispatchers, retry attributes, and recurring schedule registration reside in `Normora.Infrastructure` and `Normora.Worker`; replacing Hangfire should not require changes to document command handlers or ingestion/purge logic.
7. **External Provider Boundary**: Documents and Conversations own contracts for storage, extraction, embeddings, text generation, query rewriting, and suggestions; MinIO and Gemini adapters (including multimodal OCR and text extraction) and their HTTP-client registrations live in `Normora.Infrastructure`. The modules do not reference those provider implementations or the MinIO package. PayU checkout remains implemented in Tenants because callback verification and subscription fulfillment are coupled to Tenants-owned persistence; callers depend on the `IPaymentService` abstraction.

### Boundary Enforcement Rules
1. **100% DbContext Encapsulation**: Module DbContexts (`TenantsDbContext`, `DocumentsDbContext`, `ConversationsDbContext`) are used only inside their owning modules. The host can request migrations via `IDatabaseMigrator`, but does not resolve a module context directly.
2. **Contract-Driven Cross-Module Queries & Commands**: When one module or the host requires operations or data owned by another module, it dispatches a lightweight MediatR request or query contract residing in `Normora.Shared.Contracts.*`:
   - `PerformHybridRetrievalQuery`: Dispatched by `Conversations` to retrieve authoritative candidate chunks from `Documents` without directly referencing `IRetrievalService`.
   - `GetMcpToolsQuery` and `ExecuteMcpToolCommand`: Dispatched by `McpController` in `Documents` to discover and execute agent tools owned by `Conversations` without directly referencing `IAgentToolRegistry`.
   - Metrics and admin queries: `GetTenantDashboardMetricsQuery`, `GetDocumentDashboardMetricsQuery`, `GetConversationDashboardMetricsQuery`, `GetAccessibleDocumentFileNamesQuery`, `GetTenantAdminRecipientsQuery`.
3. **Boundary Interfaces**: Cross-cutting infrastructure hooks are decoupled using shared interfaces. 
   - `ITenantResolutionService`: Allows `TenantResolutionMiddleware`, `DocumentHub`, and `McpController` to validate memberships and resolve tenant slugs without direct access to `TenantsDbContext`. Backed by distributed caching (`ICacheService`) with a 30s sliding TTL to avoid redundant queries per HTTP request.
   - `IDocumentNotificationService`: Enables background workers (`DocumentProcessingJob`) in the Documents module to push real-time WebSocket progress updates without coupling the module to SignalR hub dependencies.
   - `IDatabaseMigrator`: Lets each module migrate its own persistence boundary from the one-off migration runner.
4. **Immediate Membership Revocation & Distributed Caching**: Tenant membership and department permissions are resolved via `ITenantResolutionService` backed by distributed cache with a short 30-second sliding expiration, eliminating redundant 3-table split-queries per request while guaranteeing permission changes and revocations propagate within seconds across all API replicas.
5. **Explicit Migration Lifecycle**: Schema migrations run as a serialized `dotnet Normora.Api.dll --migrate` deployment step, not during API or worker startup. Pending-model-change warnings are not suppressed.
6. **Production Infrastructure Requirements**: Production requires a distributed Redis cache, MinIO TLS, and explicit trusted reverse-proxy IPs. Forwarded headers are limited to one trusted proxy hop.

## Identity & Access Management (IAM)
Normora uses the **Backend-For-Frontend (BFF)** pattern powered by `Duende.BFF` to eliminate token exposure to the browser entirely.

### Authentication Flow

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

### Docker Reverse-Proxy Gotcha (UseForwardedHeaders)
When the API runs inside Docker behind an nginx reverse proxy, `HttpContext.Request.Host` resolves to the internal Docker hostname (`api:8080`) rather than the public-facing address (`localhost:4200`). This causes the OIDC middleware to build an incorrect `redirect_uri`, which Keycloak rejects.

**Fix**: `UseForwardedHeaders` middleware is registered in `Program.cs` to read the `X-Forwarded-Host`, `X-Forwarded-For`, and `X-Forwarded-Proto` headers that nginx injects, allowing the API to correctly reconstruct the public URL. Only explicitly configured proxy IPs are trusted; do not clear ASP.NET Core's default proxy restrictions.

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor 
                     | ForwardedHeaders.XForwardedHost 
                     | ForwardedHeaders.XForwardedProto
});
```

Nginx must also set `proxy_set_header X-Forwarded-Host $host;` on all proxy blocks pointing to the API.

### SignalR & BFF Authentication Gotcha
When unauthenticated users attempt to connect to SignalR Hubs (`/hub/negotiate`), ASP.NET Core's default behavior is to return a `302 Redirect` to the Identity Provider (Keycloak). Because this is an AJAX/Fetch request from the SignalR client, the browser will automatically follow the redirect in the background, resulting in failed or cancelled cross-origin API calls to Keycloak's `/auth` endpoint.

**Fix**: All SignalR Hub route mappings in `Program.cs` must be explicitly appended with `.AsBffApiEndpoint()`. This tells the Duende BFF middleware to intercept unauthenticated requests to the hub and correctly return a `401 Unauthorized` response instead of a redirect.

```csharp
app.MapHub<DocumentHub>("/hubs/documents").AsBffApiEndpoint();
```

### Multi-Host SignalR Redis Backplane
Because `Normora.Worker` (which executes background ingestion jobs) and `Normora.Api` (which hosts the client WebSocket connections) run in isolated containers, SignalR requires a distributed backplane to route real-time events across process boundaries.
- **Backplane Package**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.
- **Shared Extension**: `services.AddNormoraSignalR(configuration)` in `Normora.Infrastructure` configures SignalR with Redis pub/sub using `RedisChannel.Literal("normora:signalr")`.
- **Broadcast Flow**: When `Normora.Worker` publishes a `DocumentStatusChanged` event through `IDocumentNotificationService`, the event is dispatched to the Redis backplane, received by `Normora.Api`, and immediately streamed to the employer's connected browser over WebSocket.


### Token Lifecycle
- **Access tokens** and **refresh tokens** are encrypted inside the `__Host-spa` cookie by the BFF — the browser never sees them.
- **Token renewal** is fully automatic. When the access token expires, `Duende.BFF` performs a back-channel refresh with Keycloak and updates the cookie transparently.
- **CSRF protection**: All mutating requests (`POST`, `PUT`, `DELETE`) must include the `X-CSRF: 1` header, enforced globally via `.AsBffApiEndpoint()`.

### Angular Route Guard Synchronization
The SPA's routing decisions (`AuthGuard`) rely on the authentication state provided by the BFF (`/bff/user`). Because the BFF network check is asynchronous, `AuthGuard` must wait for the initial check to complete before executing.
- If `AuthGuard` evaluates the state before the check completes (e.g., during a hard page reload), it will incorrectly assume the user is unauthenticated and trigger a premature redirect to the `/auth/login` route.
- **Fix**: The `AuthService` uses a nullable `BehaviorSubject<boolean | null>` to track initialization. The `AuthGuard` uses an RxJS `filter(state => state !== null)` pipe to pause route evaluation until the BFF check conclusively returns `true` or `false`.
- **CSRF protection**: All mutating requests (`POST`, `PUT`, `DELETE`) must include the `X-CSRF: 1` header, enforced globally via `.AsBffApiEndpoint()`.

### JIT User Provisioning
When a user successfully authenticates via Keycloak (or Google/GitHub via Keycloak), their local shadow profile (`User.cs`) is updated or created. The `GetCurrentUserQueryHandler` automatically syncs their latest `DisplayName` and `Email` from the OIDC claims (populated from the BFF cookie) into PostgreSQL on every login.

## Observability
The backend relies on **OpenTelemetry (OTLP)** for a unified observability layer:
- **Tracing**: Traces HTTP requests (ASP.NET), outgoing HTTP calls (e.g. Gemini), and Entity Framework database queries.
- **Metrics**: Captures standard process metrics alongside custom application metrics (e.g. LLM tokens consumed, RAG latency, background job completion).
- **Exporters**: Designed to export to the console during development and a vendor-neutral OTLP collector in production.

## Frontend Architecture
The Angular application resides in the `client/` directory and communicates with the .NET backend via REST APIs. Its root route is server-rendered, while wildcard application routes are client-rendered. During local development and production, requests to `/api/*` are proxied to the backend. The frontend seamlessly handles complex auth flows, including intercepting OAuth callbacks to redirect users accepting invitations.

## Deployment
The local stack is containerized. `docker-compose.yml` orchestrates PostgreSQL, pgAdmin, Keycloak, MinIO, Redis, a one-off schema migrator, the .NET API, a separate Hangfire worker, and the Angular client served via Nginx.

### Ephemeral Migration Runner (`migrator`)
Database migrations run exclusively through a dedicated, ephemeral `migrator` service executing `dotnet Normora.Api.dll --migrate`.
- **Lean Host Isolation (`MigrationHostExtensions`)**: When invoked with `--migrate`, [`Program.cs`](file:///c:/Users/Sanjeev%20Kumar/Codes/Normora/server/Normora.Api/Program.cs) delegates immediately to [`MigrationHostExtensions.RunMigrationsAsync`](file:///c:/Users/Sanjeev%20Kumar/Codes/Normora/server/Normora.Api/Extensions/MigrationHostExtensions.cs) using a minimal `Host.CreateApplicationBuilder`. This prevents initializing Kestrel, OpenTelemetry diagnostic listeners, or Duende background services, ensuring clean zero-exit container teardown without native SIGSEGV crashes.
- **Pre-Rollout Dependency**: Both `api` and `worker` depend on `migrator` with `condition: service_completed_successfully`, preventing any HTTP traffic or background jobs from starting until the database schema is confirmed ready.
- **Race Condition Prevention**: Isolating schema execution in a dedicated one-off container eliminates race conditions and PostgreSQL table lock contention that would otherwise occur if multiple API or worker replicas ran migrations simultaneously upon boot.
- **Idempotent Pending Checks**: The runner checks `__EFMigrationsHistory` on every startup. If all migrations in the code have already been recorded, EF Core performs a no-op (`No migrations were applied. The database is already up to date.`) and exits cleanly in milliseconds.
- **Production Alignment**: Production deployments mirror this lifecycle by executing `dotnet Normora.Api.dll --migrate` as a serialized release pipeline task prior to rolling out API and worker containers. Production also enforces explicitly configured trusted proxy IPs, Redis clustering, and MinIO TLS.

## Multi-Tenancy and Scalable Architecture
Normora implements a highly scalable **Multi-Tenant Architecture** using a many-to-many relationship between Users and Tenants. This is a foundational design choice that supports both business growth and long-term development velocity.

### Junction-Based Identity Model
Rather than a 1-to-1 mapping where one user belongs to one organization, Normora uses a `TenantMembership` junction entity. This decouples the core user identity from their contextual authorization.

**Key Benefits for B2B SaaS:**
1. **Single Sign-On (Unified Identity)**: Users maintain a single account (one email/password) across the platform, regardless of how many organizations they are invited to.
2. **Context-Specific Roles (Granular Access Control)**: Because the `TenantRole` (e.g., Employer, Employee) is stored on the `TenantMembership` rather than the `User` record, a user can have entirely different permission levels depending on which tenant workspace they are currently viewing.
3. **Frictionless Collaboration**: This architecture inherently supports complex B2B scenarios, allowing accountants, contractors, and parent companies to seamlessly switch between multiple clients' or subsidiaries' workspaces without logging out.
4. **Simplified User Experience**: The frontend provides a "workspace switcher." The backend dynamically enforces the correct permissions based on the requested tenant context.
5. **Efficient Lifecycle Management**: If a user updates their profile or leaves the platform entirely, only one `User` record needs to be updated or deactivated. If they leave a specific organization, only their specific `TenantMembership` is removed.

### Scalable Development
This design physically prevents the accumulation of technical debt regarding identity management:
- **Architectural Scalability**: Eliminates the need for users to create duplicate accounts with different emails just to join a second company, preventing database bloat and terrible UX.
- **Development Scalability**: Future features can be added cleanly. For example, adding new specialized roles (e.g., "Billing Admin") only requires updating the `TenantMembership` authorization logic, leaving the core authentication and identity systems untouched.

### Department & User Group Scoping (Isolated Answers)
To support large organizations where sensitive documents (like HR policies or Executive reports) must be restricted, Normora extends its multi-tenancy with **Internal Data Scoping**:
- **Effective Departments Resolution**: At login, the `Tenants` module resolves all departments a user is part of (both direct assignments and those inherited via `UserGroups`). These `DepartmentIds` are embedded in the user's OIDC claims.
- **Scoped Hybrid Search**: When querying the RAG pipeline (`AskNormora`), the backend reads these claims and dynamically filters the `pgvector` hybrid search. It restricts results strictly to chunks from documents marked as **Company Wide** (no departments) or matching the user's **Effective Departments**. This guarantees that an employee cannot retrieve AI-generated answers from restricted departmental documents.

### Personal Workspaces
Every user is automatically provisioned a private `Tenant` marked as `IsPersonal = true`. This ensures that individual users have a secure, isolated sandbox to upload and query their own private documents without polluting corporate workspace databases or search results.
- **Simplified Capabilities**: Personal workspaces bypass complex B2B features. For example, document uploads in a personal workspace automatically hide the Department Assignment UI, as there is no organizational hierarchy within a personal tenant.
- **Unified Data Model**: By treating the personal sandbox as simply another `Tenant`, the backend requires zero specialized schema alterations. The standard data isolation, hybrid search, and generative boundaries work flawlessly and uniformly.

## Code Quality Standards
To ensure a maintainable and robust codebase, Normora adheres to strict clean code principles:
- **No Magic Strings**: Authorization roles and API response messages are centralized into statically typed constant classes (e.g., `TenantRoles.cs` and `ApiMessages.cs` in the backend, and equivalent TypeScript constants in the frontend). This prevents typo-driven security vulnerabilities and simplifies localization or future refactoring.
- **Unified API Responses**: Every API endpoint uses a generic `ApiResponse<T>` wrapper, guaranteeing that successful data payloads, validation errors, and server exceptions all return identical JSON envelopes.
- **OpenAPI Documentation**: The backend leverages Scalar UI to auto-generate interactive, developer-friendly OpenAPI specifications that strictly map to the `ApiResponse<T>` schemas and XML comments.
- **Configuration & Options Pattern**: All configuration values are strongly typed using the `IOptions<T>` pattern. Magic strings (URLs, claim types, scopes) are strictly avoided. See [Configuration Architecture](configuration.md) for details.

## Application Security (BOLA & BFLA)
Security and data isolation are critical in a multi-tenant environment. Normora is specifically designed to mitigate common API vulnerabilities, notably **Broken Object Level Authorization (BOLA/IDOR)** and **Broken Function Level Authorization (BFLA)**.

### Preventing BFLA
BFLA occurs when users can execute functions (endpoints) they shouldn't have access to based on their roles.
- **Tenant Context Extraction**: The `TenantResolutionMiddleware` securely extracts the `X-Tenant-Id` header from incoming requests. Crucially, it queries the database (`TenantsDbContext`) to verify that the authenticated user genuinely belongs to that tenant and that the workspace is active (`TenantStatus.Active`).
- **Strict Role Validation**: The `[RequireTenant(params string[] roles)]` attribute acts as an authorization filter. It checks the successfully resolved `ITenantContext` to ensure the user holds the specific role (e.g., "admin", "employee") *within that specific tenant* before the controller logic is executed.
- **Authentication Status Normalization**: In `RequireTenantAttribute`, when an authenticated user attempts to access an endpoint for a tenant they do not belong to, the filter returns `403 Forbidden` (`ApiMessages.TenantContextMissing`) rather than `401 Unauthorized`. This prevents the client-side HTTP interceptor from mistaking the permission rejection for an expired session and destroying the user's active Keycloak authentication.
- **Financial BFLA Guard**: In subscription management (`UpdateTenantPlanTierCommand`), tenants are strictly blocked from upgrading to paid tiers (`Pro`, `Enterprise`) via direct API calls. Upgrades to paid tiers must execute via verified payment checkout callbacks (`PayUPaymentService`), preventing unauthorized plan escalation.
- **Active Member Seat Drain Protection**: In employee invitations (`InviteEmployeeCommand`), handlers check whether the target email address is already an active member (`m.RemovedAt == null`). This blocks malicious or erroneous attempts to artificially exhaust subscription seat quotas (`plan.MaxEmployees`) with duplicate pending invites.

### Preventing BOLA (IDOR)
BOLA occurs when an application does not properly validate that the user is authorized to access the specific object ID they requested (e.g., manipulating a document ID or tenant ID in the URL).
- **Context-Bound Operations**: Controllers enforce BOLA protection by strictly associating operations with the authorized `ITenantContext`. Even if a route provides an `{id}` (like `DELETE /api/Documents/{id}` or `POST /api/Tenants/{id}/suspend`), the backend explicitly verifies that the requested object belongs to the user's validated `tenantContext.TenantId`. 
- **Entity Boundary Stamping on Sub-Aggregates**: All entities implementing `ITenantEntity` (such as `ConversationShare`, `MessageFeedback`, and `DocumentDepartment`) are explicitly stamped with `TenantId` upon instantiation rather than relying solely on change-tracking defaults. In `ShareConversationCommandHandler`, initializing `TenantId = conversation.TenantId;` guarantees that public share verification in `GetSharedConversationQueryHandler` performs deterministic tenant boundary checks.
- **Example**: In the `SuspendTenant` endpoint, the `{id}` from the route is validated against `_tenantContext.TenantId`. This prevents a malicious tenant admin from suspending *another* tenant by injecting their own valid `X-Tenant-Id` header while manipulating the target `{id}` in the URL.
- **Generic 404 Response Mappings**: When a BOLA violation occurs, `BolaException` maps to a standard 404 Not Found response (`ApiMessages.NotFound`), preventing attackers from enumerating resource identifiers across foreign tenants.

## Enterprise Security & Runtime Hardening

### 1. ASP.NET Core Middleware Pipeline Ordering
The pipeline order in `Normora.Api/Program.cs` is strictly enforced:
1. `UseAuthentication()`: Decodes session cookie into `ClaimsPrincipal`.
2. `UseBff()`: Applies BFF anti-forgery token and session validation.
3. `UseRateLimiter()`: Rate limiters evaluate the authenticated `sub` (User ID) claim. Placing rate limiting after authentication prevents authenticated users from being grouped into the anonymous IP bucket, mitigating Denial-of-Service cascades.
4. `TenantResolutionMiddleware`: Resolves `X-Tenant-Id` and binds effective department clearances.
5. `UseAuthorization()`: Evaluates endpoint role and tenant policies (`[Authorize]`, `[RequireTenant]`).

### 2. Outbound Secret Hygiene (Header-Based Auth)
Outbound LLM and embedding integrations with Google Gemini API pass credentials exclusively via the `x-goog-api-key` HTTP request header rather than URL query parameters. This eliminates secret exposure in OpenTelemetry traces, distributed APM spans, and reverse proxy access logs.

### 3. Stored XSS & File Upload Defense
Branding assets (logos and favicons) undergo three layers of validation before persisting to MinIO storage:
1. **Size Bound**: Strict 5MB limit prevents storage exhaustion.
2. **Extension Allowlist**: Only raster/icon extensions (`.png`, `.jpg`, `.jpeg`, `.webp`, `.ico`) are accepted. SVG files are strictly prohibited to block inline `<script>` tags and XML External Entity (XXE) vectors.
3. **Magic Byte Verification**: File headers are inspected to verify that payload bytes match the declared image format.

### 4. Payment Gateway Security & Replay Defense
The PayU payment integration enforces:
- **Transaction Idempotency**: `PaymentTransactionId` is recorded and checked before applying subscription renewals, blocking callback replay attacks.
- **Catalog Price Validation**: The incoming paid `amount` is verified against the backend plan pricing catalog for the specified currency, preventing client-side price tampering.

### 5. OWASP API4 Resource Exhaustion Protections & Rate Limiting
- **MCP Endpoint Rate Limiting**: `McpController` (`/api/mcp`) is guarded with `[EnableRateLimiting("ai")]` (20 requests/minute per authenticated user partition), preventing automated agents from saturating server capacity and exhausting Gemini token limits.
- **Generative Suggestions Rate Limiting**: `SuggestionsController` (`/api/suggestions`) is guarded with `[EnableRateLimiting("ai")]` to cap LLM question starter synthesis.
- **Unauthenticated Webhook Rate Limiting**: `SubscriptionController.HandlePaymentCallback` is guarded with `[EnableRateLimiting("anon")]` (5 requests/minute per client IP partition) to prevent denial-of-service from SHA-512 cryptographic verification flood attacks.
- **Bounded Pagination**: List queries (such as `GetTenantEmployeesQueryHandler`) enforce strict parameter bounding (`Math.Clamp(request.PageSize, 1, 100)` and `Math.Max(1, request.Page)`), preventing multi-gigabyte memory allocations and division-by-zero errors.

### 6. Input Validation Defense-in-Depth (FluentValidation)
Incoming CQRS commands are validated via `ValidationBehavior<TRequest, TResponse>` in the MediatR pipeline:
- **Tenant Provisioning**: `CreateTenantCommandValidator` enforces organization name length (1-100 characters) and validates workspace slugs against `^[a-z0-9]+(-[a-z0-9]+)*$` (3-50 characters), preventing path-traversal sequences and URL routing corruptions.
- **Invitations**: `InviteEmployeeCommandValidator` enforces valid RFC email structure and 255 character length bounds.
- **Conversational RAG**: `AskConversationCommandValidator` caps prompts to 4000 characters and clamps retrieval limits to between 1 and 20 candidates. `AskConversationStreamCommandHandler` performs equivalent stream-level boundary verification.

### 7. Production Exception Masking & Information Leakage Prevention
`GlobalExceptionHandler` enforces strict production error masking without destroying actionable client feedback:
- **Sanctioned Business Constants**: All constant string literals declared in `Normora.Shared.Constants.ApiMessages` are cached dynamically via reflection (`SafeDomainMessages`).
- **Safe Propagation**: Legitimate domain rule rejections (`CannotRemoveLastAdmin`, `PendingInvitationExists`, `UserAlreadyMember`, `DepartmentExists`) are preserved in production responses, ensuring frontend toast notifications remain clear.
- **Infrastructure Masking**: Arbitrary runtime, LINQ, or database exceptions not matching `SafeDomainMessages` are masked with generic `ApiMessages.BadRequest` or `ApiMessages.InternalServerError`, preventing internal stack traces or connection details from leaking to clients.

### 8. High-Throughput & GC Optimizations
- **Static SSE Serialization**: Streaming RAG Server-Sent Events use a pre-allocated static `JsonSerializerOptions` in `ConversationsController`, eliminating Gen-0 allocations and reflection overhead per token chunk.
- **Chunk Boundary Overlap**: Document chunking incorporates a 200-character sliding overlap across paragraph boundaries, maintaining semantic continuity for vector search.
- **Immediate Cache Eviction**: Tenant resolution caching (`tenant_access:{tenantId}:{userId}`) is evicted immediately upon employee removal, invitation acceptance, department reassignment, or workspace suspension.

## White-Label Branding & Workspace Routing
Normora supports per-tenant white-labeling without separate application builds. Each tenant can have its own brand identity applied dynamically at runtime.

### Data Model
Branding is stored in a dedicated `TenantBranding` table (1-to-1 with `Tenant`) rather than embedded directly in the `Tenant` row, keeping branding concerns fully separated:

```
TenantBranding
├── Id
├── TenantId       (FK → Tenant)
├── PrimaryColor   (e.g. "#3b82f6")
├── SecondaryColor
├── LogoUrl
└── FaviconUrl
```

### Anonymous Branding API
`GET /api/tenants/branding/{slug}` is an `[AllowAnonymous]` endpoint so the Angular app can fetch and apply a tenant's brand *before* the user logs in.

### Frontend: Dynamic CSS Variables
The `TenantBrandingService` (Angular) extracts the tenant slug from the workspace path URL on app startup and applies branding as CSS custom properties on the document root:

```typescript
document.documentElement.style.setProperty('--brand-primary', branding.primaryColor);
```

This causes the entire app to reskin automatically without any rebuild.

## Document Processing Lifecycle

Document metadata is stored in PostgreSQL while the original file is stored in MinIO. The document status is represented by the shared `DocumentStatus` enum:

```text
Uploaded -> Processing -> Ready
                         \-> Failed
```

The upload request stores the file and creates an `Uploaded` metadata record. The employer document list displays the lifecycle state using readable API values. The background worker, text extraction, normalization, and chunking steps are intentionally separate so a document is never shown as `Ready` before ingestion has completed.

Hangfire currently provides the queue adapter, using PostgreSQL storage, and runs document-processing and purge handlers in the separate `Normora.Worker` process. Documents application code has no Hangfire reference: the upload handler signals the module-owned `IDocumentProcessingWakeup` abstraction, while Hangfire-specific dispatch wrappers and retry configuration live in `Normora.Infrastructure`. The MinIO object, document metadata, and a processing outbox record are persisted together. The adapter enqueues work and removes each outbox record only after enqueue succeeds; a recurring dispatch provides recovery if the immediate wake-up fails. Since dispatch is at-least-once, `DocumentProcessingJob` atomically claims only `Uploaded` or `Failed` versions before processing. The job validates both the document ID and tenant ID outside the HTTP tenant context, downloads the tenant-owned object from MinIO, extracts text and Markdown tables using Gemini multimodal OCR (for PDFs and images) or native stream parsing (for DOCX/TXT), stores the extracted text, normalizes it into bounded `DocumentChunk` rows, and moves the document to `Ready`. Extraction failures move the document to `Failed` and are rethrown for the queue adapter's retry policy. The Hangfire dashboard is available at `/hangfire` in Development only.

Chunks are tenant-owned and uniquely ordered per document. A retry removes the document's existing chunks and recreates them, preventing duplicate chunk records. Each chunk is capped at approximately 4,000 characters and preserves paragraph boundaries where possible. Embeddings and vector search are separate later stages.

When `Gemini:ApiKey` is configured, the ingestion job generates a 768-dimensional Gemini embedding for each chunk and stores it in PostgreSQL `pgvector` before publishing `Ready`. Without a key, local ingestion still completes through chunking and leaves embeddings null; retrieval is disabled until embeddings are available.

The tenant-protected `GET /api/documents/search?query=...&limit=5` endpoint embeds the query, applies the current tenant's chunk filter, ranks results by pgvector cosine distance, and returns the matching source document, chunk index, content, and similarity score. It is retrieval only; answer generation and citations belong to the next RAG slice.

The employee `POST /api/ask` endpoint uses the same tenant-filtered retrieval, rejects weak matches below the similarity threshold, and sends only the selected chunks to Gemini generation. Its response contains the grounded answer and document/chunk citations; when evidence is insufficient it returns a no-answer message instead of guessing.

### Realtime Document Events & Multi-Host SignalR Backplane

Authenticated clients connect to `/hubs/documents` and explicitly join a tenant group. SignalR automatically forwards the secure `__Host-spa` cookie for authentication. The hub validates the current user's membership before adding the connection. Upload and processing transitions publish `DocumentStatusChanged` events to that tenant group, allowing the employer document list to update without polling.

#### Evolution: In-Process vs. Multi-Host SignalR
1. **Earlier Architecture (Single-Process In-Memory)**:
   - When Hangfire background processing and WebSockets ran inside the single `Normora.Api` host, SignalR utilized ASP.NET Core's default in-memory lifetime manager (`DefaultHubLifetimeManager`).
   - When `DocumentProcessingJob` finished, it called `hubContext.Clients.Group("tenant:...").SendAsync(...)`. Both the publisher (background job thread) and the subscriber (open browser WebSocket connection) shared the exact same process memory space, so events reached the browser without any external broker.
2. **The Multi-Process Boundary Problem**:
   - To prevent intensive background workloads (multimodal document OCR, PDF vision parsing, Gemini 768-dim vector embeddings) from blocking HTTP request threads, the worker was decoupled into an independent generic host process: `Normora.Worker`.
   - In this decoupled architecture, browser WebSockets connect exclusively to the HTTP BFF (`Normora.Api`), while background ingestion executes inside `Normora.Worker`.
   - Without an external backplane, `Normora.Worker` invoked its own local in-memory SignalR hub context, which had **0 connected clients**, causing all status transition events to be silently dropped within the worker container.
3. **The Solution: Distributed Redis Pub/Sub Backplane**:
   - Both `Normora.Api` and `Normora.Worker` are configured with `Microsoft.AspNetCore.SignalR.StackExchangeRedis` via `AddNormoraSignalR(configuration)` in `Normora.Infrastructure`.
   - When `Normora.Worker` finishes embedding chunks, it publishes the event to Redis under the `normora:signalr` channel.
   - `Normora.Api` receives the event via Redis pub/sub and immediately pushes it down the active WebSocket connection to the browser.

```mermaid
flowchart LR
    Browser["Angular Browser Client"] <-->|"WebSocket (/hubs/documents)"| Api["normora_api Host"]
    Worker["normora_worker Host<br/>(DocumentProcessingJob)"] -->|"PUBLISH normora:signalr"| Redis[("normora_redis<br/>(Pub/Sub Backplane)")]
    Redis -->|"SUBSCRIBE normora:signalr"| Api
    Api -->|"Live DocumentStatusChanged"| Browser
```


### Path-Based Workspace Routing
After a successful login:
- **User with no tenants**: stays on base `localhost:4200` and is routed to `/onboarding`.
- **User with tenants**: redirected to `/app/workspaces/{slug}/{dashboard}` using their default (non-personal) membership's tenant slug.

> **Future Enhancement**: Persist the user's last selected tenant and use it for login routing instead of always defaulting to the first membership.

## Export Answers

Employees can export any saved answer to a portable file format directly from the **Saved Answers** page. Exports are generated synchronously in the API process — no Hangfire job or polling is required for the current document sizes.

### Abstraction Design

The export system follows the **Strategy Pattern** via the `IAnswerExporter` interface, keeping the MediatR query handler completely decoupled from any specific library:

```
IAnswerExporter
    ├── MarkdownAnswerExporter   → .md  (pure string-building, no dependencies)
    ├── PdfAnswerExporter        → .pdf (QuestPDF 2025.5.0)
    └── DocxAnswerExporter       → .docx (DocumentFormat.OpenXml 3.3.0)
```

All three exporters receive an `AnswerExportData` record containing:

| Field | Source |
|---|---|
| `Question` | The most recent preceding `User` message in the same conversation |
| `Answer` | The saved assistant message content |
| `Citations` | All `MessageCitation` records attached to the message, ordered by rank |
| `SavedAt` | `SavedAnswer.CreatedAt` |
| `EmployeeName` | `ICurrentUser.DisplayName` or `Email` |
| `TenantName` | The conversation `Title` (used as contextual label) |

The exporters are registered as **singletons** in `ApplicationServiceExtensions` — they are stateless and safe to share across requests.

### QuestPDF License

The community licence is set once in `PdfAnswerExporter`'s static constructor, which runs the first time the singleton is resolved. This keeps the licence setting inside the module (`Normora.Modules.Conversations`) that owns the QuestPDF package reference, avoiding a transitive package dependency on `Normora.Api`.

```csharp
static PdfAnswerExporter() => QuestPDF.Settings.License = LicenseType.Community;
```

### PDF Layout

The QuestPDF document is structured as:

```
┌──────────────────────────────────────────┐
│  Normora                  Saved Answer   │  ← Page header
│  ──────────────────────────────────────  │
│  Employee: …   Saved: …                  │  ← Metadata
│                                          │
│  Question                                │  ← H2 heading
│  ┌──────────────────────────────────┐    │
│  │  > Question text (shaded block)  │    │
│  └──────────────────────────────────┘    │
│                                          │
│  Answer                                  │  ← H2 heading
│  Plain-text body (Markdown stripped)     │
│                                          │
│  Sources                                 │  ← H3 heading
│  ┌──────────────────┬─────────────┐      │
│  │ Document         │ Relevance   │      │  ← Citation table
│  ├──────────────────┼─────────────┤      │
│  │ Policy.pdf       │ 92%         │      │
│  └──────────────────┴─────────────┘      │
│                                          │
│  Page 1 of 1                             │  ← Page footer
└──────────────────────────────────────────┘
```

### DOCX Layout

The Open XML SDK document uses proper paragraph styles with no external Word installation required:

- **Title**: Bold, large, indigo `#6366f1`
- **Metadata**: Semi-bold label + plain value pairs
- **Question**: Left-bordered paragraph (indigo accent) with italic text
- **Answer**: Paragraphs split by double-newline, Markdown symbols stripped
- **Citations**: Native `Table` with header row shading
- **Footer**: Small italic attribution line

### API Endpoint

```
GET /api/saved-answers/{id}/export?format=Markdown|Pdf|Docx
```

- The `ExportSavedAnswerQuery` MediatR handler enforces tenant isolation via EF Core global query filters and validates `UserId` ownership before fetching the document.
- The controller streams the `byte[]` result using `File(bytes, contentType, fileName)`, which sets `Content-Disposition: attachment` automatically.
- The filename is derived deterministically: `normora-answer-{yyyy-MM-dd}{ext}`.

### Frontend Download Pattern

The Angular `SavedAnswerService.exportAnswer()` method uses `responseType: 'blob'` to receive the binary response, then creates a temporary `<a>` element with `URL.createObjectURL()` to trigger the browser's native save-as dialog — no third-party download library required:

```typescript
const anchor = document.createElement('a');
anchor.href = URL.createObjectURL(blob);
anchor.download = `normora-answer-${today}.pdf`;
anchor.click();
URL.revokeObjectURL(anchor.href);
```

The export dropdown on each saved-answer card shows a spinner during the in-flight request and uses a `HostListener` on `document:click` to close the menu when the user clicks outside.

## Soft Deletion and Data Retention Strategy
Normora relies heavily on soft deletion (`DeletedAt`, `RemovedAt`) combined with EF Core **Global Query Filters** to immediately hide deleted resources (Documents, Employees) from all RAG lookups, dashboard lists, and searches without altering individual queries.

- **Document Purge**: Soft-deleted documents broadcast a SignalR event for optimistic UI updates. They are physically deleted from MinIO (and database rows purged) in bounded, retry-safe batches via a nightly queue-adapter schedule after a 30-day grace period.
- **Local-First Identity Sync**: Removing an employee soft-deletes their `TenantMembership` locally first. Access is instantly revoked via Global Query Filters. Afterward, a best-effort `IHttpClientFactory` call attempts to un-sync the user from the Keycloak Admin API, safely logging any errors without rolling back the local transaction. This protects against identity provider outages preventing critical access revocations.

## Audit Logging Architecture

Normora implements automatic, cross-cutting audit logging via MediatR's open pipeline behavior pattern (`IPipelineBehavior<TRequest, TResponse>`).

### 1. The `IAuditableRequest` Abstraction
Any MediatR command that modifies tenant state implements the `IAuditableRequest` marker interface:

```csharp
public interface IAuditableRequest
{
    string Action { get; }
    string? TargetType => null;
    string? TargetId => null;
    object? GetAuditMetadata() => null;
}
```

Audited operations include:
- `UploadDocumentCommand` (`Document.Upload`)
- `DeleteDocumentCommand` (`Document.Delete`)
- `InviteEmployeeCommand` (`Employee.Invite`)
- `AcceptInvitationCommand` (`Employee.AcceptInvitation`)
- `RemoveEmployeeCommand` (`Employee.Remove`)
- Department CRUD (`Department.Create`, `Department.Update`, `Department.Delete`)
- User Group CRUD (`UserGroup.Create`, `UserGroup.Update`, `UserGroup.Delete`)
- `UpdateTenantBrandingCommand` (`Tenant.UpdateBranding`)
- `SuspendTenantCommand` (`Tenant.Suspend`)

### 2. MediatR `AuditBehavior`
The pipeline behavior intercepts requests before and after execution:
1. Checks if the incoming request implements `IAuditableRequest`.
2. Resolves active tenant context (`ITenantContext`) and authenticated actor (`ICurrentUser`).
3. Executes the inner request handler (`await next()`).
4. If successful, creates an `AuditLog` domain record via `IAuditService` containing:
   - `TenantId` (guaranteed tenant boundary)
   - `UserId` (authenticated actor GUID)
   - `Action` (e.g. `Document.Upload`)
   - `TargetType` & `TargetId` (affected resource)
   - `Metadata` (serialized JSON context)
   - `CreatedAtUtc` (UTC timestamp)

```csharp
public class AuditBehavior<TRequest, TResponse>(
    IAuditService auditService,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();
        if (request is IAuditableRequest auditable && tenantContext.HasTenant)
        {
            await auditService.LogAsync(
                tenantContext.TenantId,
                currentUser.UserId,
                auditable.Action,
                auditable.TargetType,
                auditable.TargetId,
                auditable.GetAuditMetadata(),
                ct);
        }
        return response;
    }
}
```

### 3. Querying and UI
Employers access audit logs via `GET /api/audit-logs` (`GetAuditLogsQuery`). The endpoint supports cursor-style or page-based pagination, date-range filtering (`fromDate`, `toDate`), action category filtering, and keyword search against metadata and actor display names.

---

## Transactional Email Architecture (Provider-Agnostic)

Normora provides transactional email notifications (employee invitations, document processing failure alerts, document-ready notifications, and HR escalation tickets) built on a provider-agnostic abstraction.

### 1. Transport Abstraction (`IEmailSender`)
Email delivery logic does not couple to third-party SDKs:
- **`IEmailSender`** (`Normora.Shared.Interfaces`): Core interface declaring `SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct)`.
- **`ResendEmailSender`**: Production implementation using Resend REST API via `IHttpClientFactory` (`https://api.resend.com/emails`).
- **`NullEmailSender`**: Safe fallback implementation for automated tests or local setups where `RESEND_API_KEY` is omitted, avoiding unhandled crashes.

### 2. Configuration & Options
Configured in `Normora.Shared.Options.EmailOptions`:
- `Provider`: `"Resend"` or `"Null"`
- `ApiKey`: Provider API key (`RESEND_API_KEY`)
- `FromEmail`: Verified sender address (`RESEND_FROM_EMAIL`)
- `FromName`: Sender display name (defaults to `"Normora"`)
- `DevOverrideRecipient`: Development interceptor (`DEV_OVERRIDE_EMAIL`) that redirects all outbound emails to the developer's verified sandbox inbox.

### 3. Asynchronous Worker Execution
Email dispatch is fully non-blocking:
- Handlers invoke `IEmailService.SendAsync(...)`.
- The request is serialized and enqueued into **Hangfire** (`SendEmailJob`) with automatic retry policies (10s, 30s, 60s), ensuring transient provider rate limits or network blips never fail the originating HTTP request.

---

## Subscription & Plan Limits Architecture

Multi-tenant SaaS scalability requires strict governance over shared compute, storage, and AI resource consumption. Normora implements tier-based quota management directly in the core application pipeline.

### 1. Tier Definitions
Each workspace is associated with a `TenantPlan` record (`tenants.tenant_plans`):

| Plan Tier | Max Documents | Max Employees | Max Storage Bytes | Description |
|---|---|---|---|---|
| **Free** | 10 | 5 | 50 MB | Starter tier for small teams and evaluations. |
| **Pro** | 100 | 50 | 5 GB | Growth tier with department and user group scoping. |
| **Enterprise** | 10,000 | 5,000 | 500 GB | Full-scale enterprise tier with white-label branding, pgvector search, and audit trails. |

### 2. Cross-Module Quota Verification
Because `Documents` and `Tenants` are separate modules with isolated DbContexts:
- The `Documents` module exposes CQRS query contracts (`GetTenantDocumentUsageQuery` and `CheckTenantUploadLimitQuery`).
- When a document upload is initiated (`UploadDocumentCommandHandler`):
  1. The handler retrieves the tenant's active `TenantPlan`.
  2. Queries current document usage (count of non-deleted documents and sum of stored file sizes).
  3. Verifies that `currentDocs + 1 <= MaxDocuments` and `currentBytes + incomingFile.Length <= MaxStorageBytes`.
  4. If either quota would be exceeded, throws a `PlanLimitExceededException`.
- Similarly, `InviteEmployeeCommandHandler` validates current active memberships (`Count < MaxEmployees`) before dispatching new invitations.

### 3. HTTP 402 Payment Required & Client Handling
- `PlanLimitExceededException` is caught by `GlobalExceptionHandler` and mapped to HTTP `402 Payment Required` with a structured `ApiResponse.Failure(ApiMessages.PlanLimitExceeded)`.
- The Angular frontend interceptor handles 402 responses and surfaces clear, actionable upgrade guidance rather than generic failure toasts.
- The `SubscriptionComponent` provides real-time resource meters (visual progress bars indicating document, seat, and storage utilization percentages) alongside tier cards for one-click plan upgrades.

### 4. Decoupled Payment Gateway Architecture (`IPaymentService`)
To eliminate vendor lock-in, payment processing is decoupled behind the `IPaymentService` abstraction:
- **Provider-Agnostic Interface**: `IPaymentService` defines contracts for generating hosted checkout parameters (`CreateCheckoutSessionAsync`), processing cryptographic return callbacks (`ProcessCallbackAsync`), and generating billing portal links (`CreatePortalSessionAsync`).
- **Agnostic Database Schema**: Workspace payment state in `TenantPlan` stores vendor-neutral columns `PaymentTransactionId` and `PaymentReferenceId` rather than provider-branded columns.
- **Configurable Provider**: Configured in `appsettings.json` via `Payment:Provider` (e.g. `PayU`).

### 5. PayU Hosted Checkout & Cryptographic SHA-512 Verification
The initial implementation `PayUPaymentService` integrates PayU India:
- **Hosted Checkout Sessions (`POST /api/subscription/checkout`)**:
  - Eliminates PCI-DSS compliance scope by utilizing PayU India hosted checkout (`https://test.payu.in/_payment` in development, `https://secure.payu.in/_payment` in production).
  - The server securely determines plan pricing based on the selected tier and workspace currency, assigns a unique transaction ID (`txnid`), and attaches User Defined Fields (`udf1` = tenantId, `udf2` = tier, `udf3` = currency, `udf4` = tenantSlug, `udf5` = userEmail).
  - Computes a tamper-proof SHA-512 request hash:
    `sha512(key|txnid|amount|productinfo|firstname|email|udf1|udf2|udf3|udf4|udf5||||||SALT)`
  - The frontend dynamically constructs and automatically submits a signed hidden HTML form (`submitPayUForm`) to hand off to PayU.
- **Local vs International Payment Support**:
  - **UPI (Local)**: Full UPI support (Google Pay, PhonePe, Paytm, BHIM, and QR code) for Indian Rupee (`inr`) transactions.
  - **Credit & Debit Cards (Global & Domestic)**: Cards (Visa, Mastercard, RuPay, American Express, Diners Club) supporting international payments in major currencies (`inr`, `usd`, `eur`, `gbp`, `cad`, `aud`, `jpy`, `sgd`) as well as domestic card payments.
- **Workspace Currency Persistence**:
  - The workspace billing currency is persisted in `TenantPlan.Currency` (defaulting to `"inr"`).
  - When users travel abroad, the application retains the workspace's configured INR currency, preventing accidental foreign transaction markups and preserving local UPI options.
- **Cryptographic Callback Verification (`POST /api/subscription/payu-callback`)**:
  - Public return endpoint (`[AllowAnonymous]` + `[IgnoreAntiforgeryToken]`) securely receives the browser postback from PayU (`surl` / `furl`).
  - Recomputes the reverse SHA-512 hash against the server's private `MerchantSalt`:
    - With additional charges: `sha512(additionalCharges|SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key)`
    - Standard: `sha512(SALT|status||||||udf5|udf4|udf3|udf2|udf1|email|firstname|productinfo|amount|txnid|key)`
  - Rejects hash mismatches immediately with `ApiMessages.PayUHashMismatch`.
  - On verified `success`, updates workspace quota limits to the purchased tier, records `PaymentTransactionId` and `PaymentReferenceId`, updates `SubscriptionStatus = 'active'`, and records an immutable audit log entry.
  - Redirects the user back to the tenant-scoped subscription settings with `?status=success` or `?status=failed&error=...`.
- **Tier Hierarchy & Action Locking**:
  - The UI models a strict tier hierarchy: `Free` (0) < `Pro` (1) < `Enterprise` (2).
  - When a workspace is subscribed to a tier (e.g., Enterprise), all lower/previous tier buttons are permanently disabled (`cursor-not-allowed`) and indicate `"Included in {Plan}"`.
  - Only upgrade pathways to strictly higher tiers are selectable.

---

## Code Quality, Security & Engineering Standards (Remediation & Best Practices)

Following a comprehensive senior architectural audit, the codebase implements strict patterns across security, tenancy, performance, and frontend resilience:

### 1. Security & Authorization (BOLA, BFLA & Zero Magic Strings)
- **Centralized Error & Response Constants (`ApiMessages`)**:
  - Hardcoded strings in exceptions, controllers, and alerts are strictly prohibited.
  - All messages reside in `Normora.Shared.Constants.ApiMessages` on the backend and `@core/constants/api-messages.ts` on the frontend.
- **BOLA Protection (Broken Object Level Authorization)**:
  - When a user requests a resource they do not own or have access to within the tenant boundary, handlers throw `BolaException()`.
  - Mapped globally to `404 Not Found` with generic `ApiMessages.NotFound` to completely eliminate resource ID enumeration vulnerabilities.
- **BFLA Protection (Broken Function Level Authorization)**:
  - When an authenticated user lacks permissions for an operation, handlers throw `BflaException()`.
  - Mapped globally to `403 Forbidden` returning a structured `ApiResponse.Failure(ApiMessages.Forbidden)`.
- **RAG Soft-Delete Data Isolation**:
  - In cross-department joins where `.IgnoreQueryFilters()` is applied to evaluate user permissions, queries explicitly filter `x.doc.DeletedAt == null` to prevent soft-deleted documents from ever entering semantic or keyword retrieval pipelines.

### 2. Multi-Tenancy & Database Schema Robustness
- **Soft-Delete Unique Constraints**:
  - Re-inviting previously removed members cannot trigger duplicate key violations because `TenantMemberships` enforces a partial unique index:
    `HasIndex(m => new { m.TenantId, m.UserId }).IsUnique().HasFilter("\"RemovedAt\" IS NULL")`
  - Handlers like `AcceptInvitationCommandHandler` check `.IgnoreQueryFilters()` to detect prior memberships and reactivate them cleanly (`RemovedAt = null`).
- **Vector Search Indexing**:
  - `DocumentChunks.Embedding` is indexed using PostgreSQL pgvector HNSW (`vector_cosine_ops`, `m = 16`, `ef_construction = 64`) ensuring $O(\log N)$ approximate nearest neighbor search under enterprise-scale chunk volumes.
- **Concurrency & JIT Auto-Provisioning**:
  - Just-in-Time user auto-provisioning catches `DbUpdateException` to safely handle concurrent parallel requests on initial authentication without race conditions.

### 3. Clean Architecture & Decoupling
- **Zero Cross-Module DbContext Leakage**:
  - Direct cross-module DbContext injection is forbidden across the entire codebase. API controllers, middleware, and hubs never inject `TenantsDbContext`, `DocumentsDbContext`, or `ConversationsDbContext`.
  - The legacy God Aggregator `GetDashboardSummary` (which previously injected 3 DbContexts simultaneously) was refactored into a pure MediatR orchestrator dispatching lightweight, decoupled contracts: `GetTenantDashboardMetricsQuery`, `GetDocumentDashboardMetricsQuery`, and `GetConversationDashboardMetricsQuery`.
  - `McpController` queries the Documents module through `GetMcpResourceDocumentsQuery` and `GetMcpDocumentContentQuery`, and validates tenants using `ITenantResolutionService`.
- **Decoupled MCP Tool Registry & Execution via MediatR**:
  - `McpController` in the `Documents` module does not inject `IAgentToolRegistry` from `Conversations`. Instead, it communicates via MediatR contracts in `Normora.Shared.Contracts.Conversations` (`GetMcpToolsQuery` and `ExecuteMcpToolCommand`), maintaining zero direct project or type coupling between `Documents` and `Conversations`.
- **Decoupled Cross-Module Hybrid Document Retrieval**:
  - The conversational RAG pipeline in `Conversations` (`ConversationTurnPreparer`, `SearchCompanyDocumentsTool`) retrieves candidate document chunks across module boundaries by dispatching `PerformHybridRetrievalQuery` (`Normora.Shared.Contracts.Documents`) via MediatR rather than directly injecting `IRetrievalService`.
- **Parameterless Entity Configurations & Assembly Auto-Discovery**:
  - All EF Core entity configurations in `Documents` and `Conversations` implement `IEntityTypeConfiguration<T>` with parameterless constructors, enabling standard `modelBuilder.ApplyConfigurationsFromAssembly(...)` discovery. Multitenant global query filters bind dynamically to the DbContext instance property (`_tenantContext.TenantId`) on each query rather than passing scoped services to configuration constructors.
- **High-Performance Distributed Caching for Tenant Resolution**:
  - `TenantResolutionService` wraps user membership and slug resolution in distributed caching (`ICacheService`) with a 30-second sliding expiration, eliminating redundant 3-table split-queries per HTTP request across middleware, SignalR hubs, and controllers while maintaining immediate membership revocation freshness.
- **Elimination of Module Internals from Shared**:
  - `ITextEmbeddingService` is homed inside `Normora.Modules.Documents.Application.Services`, while `RagConstants` and `ConversationConstants` reside in `Normora.Modules.Conversations.Domain`. `Normora.Shared` contains exclusively public contracts and cross-cutting abstractions.
- **Architectural Fitness Tests (`Normora.Architecture.Tests`)**:
  - `ModularMonolithArchitectureTests` uses reflection and ArchUnit-style conventions to assert that all entity configurations have parameterless constructors, no controllers inject DbContexts, and zero internal types leak between `Documents` and `Conversations`.
- **Feature Slices & Controllers Re-Homed into Modules**:
  - All command and query handlers, background ingestion workers, and embedding services reside directly in their respective modules (`server/Modules/<ModuleName>/Application/`).
  - Controllers are decentralized into `server/Modules/<ModuleName>/API/`, eliminating monolithic API host controller structures.
  - `Normora.Api` contains no business logic, controllers, or feature handlers; it serves exclusively as the HTTP/BFF protocol host for hubs, middleware, and startup configuration.
- **Decoupled External Identity (`ExternalUserId`)**:
  - The domain entity `User` maps external OIDC identity provider subject IDs to the database column `ExternalUserId` rather than vendor-specific names like `KeycloakUserId`. LINQ queries and junction tables operate against `ExternalUserId`, guaranteeing that swapping or federating identity providers requires zero schema or query alterations.
- **Provider-Neutral Storage & AI Naming**:
  - Storage references use `StorageObjectName` instead of vendor-specific names (`MinioObjectName`).
  - LLM errors and tool registry contracts use vendor-neutral terminology (`ITextGenerationService`, `IAgentToolRegistry`, `ApiMessages.AiQuotaExceeded`, `ApiMessages.AiServiceUnavailable`).
- **Modern Primary Constructors**:
  - All dependency injection across `DbContext`s, controllers, handlers, and background services utilizes C# primary constructors rather than verbose private fields and constructor boilerplate.
- **Query Optimization**:
  - Read-only middleware and verification queries apply `.AsNoTracking()` to avoid unnecessary EF Core change-tracker overhead.
  - List queries project directly into DTOs via `.Select()` rather than loading heavy entity graphs with redundant `.Include()` statements.

### 4. Frontend Resilience & Strict Typing (Angular)
- **Centralized HTTP Interceptor Handling**:
  - `api.interceptor.ts` centrally catches HTTP 401 (redirect), 402 (plan quota upgrade notification), 403 (BFLA toast), 404 (BOLA toast on non-GETs), 429 (rate-limit backoff), and 5xx (server error).
  - `tenant.interceptor.ts` uses strict URL prefix matching (`.startsWith('/api/') || .startsWith(environment.apiUrl)`) to ensure `X-Tenant-Id` headers are reliably attached without contaminating third-party URLs.
- **Contract Realignment & Strict Types**:
  - All Angular services and components use strict TypeScript typing, avoiding `: any`.
  - Route guards (`workspace.guard.ts`, `role.guard.ts`) strictly type user profiles using `CurrentUser` and `UserTenantMembership`.
  - Paginated list components use typed `PagedResult<T>` and PrimeNG `PaginatorState`.
  - UI reactive state uses Angular Signals (`signal`, `computed`) and eliminates nested RxJS subscriptions.

## Deployment Topology & Operational Architecture

Normora's operational architecture is designed for cloud-native resilience, horizontal scale, and zero-downtime rolling deployments.

### Process Boundaries

```mermaid
flowchart LR
    LB["Reverse Proxy / Ingress<br/>(TLS Termination, Port 443)"]
    
    subgraph AppCluster ["Application Host Cluster"]
        API1["Normora.Api (Replica 1)<br/>BFF, Hubs, REST API"]
        API2["Normora.Api (Replica 2)<br/>BFF, Hubs, REST API"]
    end

    subgraph WorkerCluster ["Background Worker Cluster"]
        W1["Normora.Worker (Replica 1)<br/>Hangfire Ingestion & Purge"]
        W2["Normora.Worker (Replica 2)<br/>Hangfire Ingestion & Purge"]
    end

    subgraph MigratorStep ["Serialized Migration Process"]
        MIG["dotnet Normora.Api.dll --migrate<br/>(IDatabaseMigrator Execution)"]
    end

    subgraph StateTier ["State & Persistence Tier"]
        PG[("PostgreSQL 15<br/>Schemas: tenants, documents, conversations, session")]
        RED[("Redis 7 (Distributed Cache)")]
        S3[("MinIO (S3 Object Storage)")]
        KC["Keycloak 24 (OIDC Identity)"]
    end

    LB --> API1 & API2
    MIG -.->|1. Runs to completion before rollout| PG
    API1 & API2 --> StateTier
    WorkerCluster --> StateTier
```

1. **HTTP/BFF API Replicas (`Normora.Api`)**:
   - Stateless web hosts serving Angular assets, BFF authentication endpoints (`/bff/*`), SignalR WebSocket hubs (`/hubs/*`), and modular HTTP controllers (`/api/*`).
   - Does NOT run Hangfire job processing servers; all background computation is offloaded to the worker cluster.
2. **Background Ingestion Workers (`Normora.Worker`)**:
   - Dedicated generic host processes running Hangfire server workers.
   - Executes resource-intensive tasks asynchronously: Gemini multimodal document extraction and OCR, token chunking, pgvector embedding generation via Google Gemini API, and deferred physical cleanup jobs (`PurgeDeletedDocumentsJob`).
3. **Serialized Database Migration Runner (`--migrate`)**:
   - Executed as an isolated initialization step (`dotnet Normora.Api.dll --migrate`) before any API or Worker replicas start.
   - Iterates sequentially through module-owned `IDatabaseMigrator` implementations (`TenantsDatabaseMigrator`, `DocumentsDatabaseMigrator`, `ConversationsDatabaseMigrator`, `SessionDatabaseMigrator`).
   - Prevents database deadlocks, startup race conditions, and corrupted migration history across parallel replicas.

---

### Database Schema Isolation

Each module operates strictly within its dedicated PostgreSQL schema:
- **`tenants`**: Workspaces, users, memberships, invitations, branding, departments, user groups, audit logs, and subscriptions.
- **`documents`**: Document metadata, versions, text chunks, and 768-dimensional vector embeddings with HNSW cosine similarity indexing (`vector_cosine_ops`).
- **`conversations`**: Chat sessions, messages, citations, feedback, and saved answers.
- **`session`**: Duende BFF encrypted user session records with automated PostgreSQL cleanup.

---

### Distributed Caching Architecture

Normora employs Redis via the provider-neutral `ICacheService` abstraction with short sliding expiration windows to eliminate redundant database queries while ensuring permission revocations propagate across all replicas within seconds:

| Cache Key Pattern | TTL / Policy | Invalidation Trigger | Usage |
|---|---|---|---|
| `tenant:res:{slug}:{userId}` | 30s sliding | Natural expiry | Membership resolution in `TenantResolutionMiddleware` & `DocumentHub` |
| `branding:{slug}` | 5m sliding | `UpdateTenantBrandingCommand` | Dynamic workspace CSS variables and logo loading |
| `dashboard:{tenantId}` | 2m sliding | Document uploads, deletions, version changes | Employer telemetry and storage metrics |
| `suggested-questions:{tenantId}:{deptKey}` | 6h sliding | Document updates | Pre-computed conversational RAG suggested prompts |

---

### Operational Verification

- **Build Integrity**: Clean compilation using `dotnet build server/Normora.slnx --no-restore -m:1`.
- **Architecture Fitness Testing**: Automated assertions in `Normora.Architecture.Tests` verify that:
  1. All EF Core entity configurations implement parameterless constructors.
  2. No API controller or middleware injects a module `DbContext`.
  3. No cross-module type leakage exists between `Documents` and `Conversations`.
