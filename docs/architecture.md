# Architecture

Normora follows a **Strict Modular Monolith** architecture on the backend and a standard **Single Page Application (SPA)** architecture on the frontend.

## High-Level Tech Stack
- **Frontend**: Angular, TypeScript, Nginx
- **Backend**: .NET 10 (ASP.NET Core Web API)
- **Database**: PostgreSQL 15 (managed via pgAdmin 4)
- **Identity Provider**: Keycloak 24 (with Google and GitHub Sign-In Integrations)
- **Object Storage**: MinIO
- **Infrastructure**: Docker & Docker Compose

## Backend Architecture (Modular Monolith)
The backend is structured into distinct modules to enforce separation of concerns while keeping deployment simple as a single API process. 

Crucially, there is no shared "Infrastructure" layer. Each module completely encapsulates its own persistence logic, DbContexts, and external service integrations to guarantee true decoupling.

- **Normora.Api**: The main entry point (Host). Orchestrates middleware and dependency injection for all modules.
- **Normora.Shared**: Common utilities, MediatR pipeline behaviors, and cross-cutting abstractions.
- **Modules**:
  - `Normora.Modules.Auth`: Authentication and authorization flows.
  - `Normora.Modules.Tenants`: Multi-tenancy logic, membership validation, isolated tenant data (`TenantsDbContext`), and invitation workflows (with 48-hour expiration).
  - `Normora.Modules.Users`: User profiles and management.
  - `Normora.Modules.Documents`: File metadata, isolated data storage (`DocumentsDbContext`), and object storage integration via MinIO.

Modules communicate with each other exclusively through explicitly defined contracts (e.g., MediatR CQRS commands/queries or shared interfaces) rather than directly interacting with each other's databases.

## Identity & Access Management (IAM)
Normora delegates authentication entirely to Keycloak. However, to maintain relational integrity with business data (like Tenant Memberships), the application employs **Just-In-Time (JIT) Provisioning**:
- When a user successfully authenticates via Keycloak (or Google via Keycloak), their local shadow profile (`User.cs`) is updated or created.
- The `GetCurrentUserQueryHandler` automatically syncs their latest `DisplayName` and `Email` from the Keycloak JWT token into the local PostgreSQL database on every login, guaranteeing profile consistency without relying on webhooks.

## Frontend Architecture
The Angular application resides in the `client/` directory and communicates with the .NET backend via REST APIs. During local development and production, requests to `/api/*` are proxied to the backend. The frontend seamlessly handles complex auth flows, including intercepting OAuth callbacks to redirect users accepting invitations.

## Deployment
The entire stack is containerized. `docker-compose.yml` orchestrates the services, including the PostgreSQL database, pgAdmin, Keycloak auth server, MinIO storage, the .NET API, and the Angular client served via Nginx.

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

## Application Security (BOLA & BFLA)
Security and data isolation are critical in a multi-tenant environment. Normora is specifically designed to mitigate common API vulnerabilities, notably **Broken Object Level Authorization (BOLA/IDOR)** and **Broken Function Level Authorization (BFLA)**.

### Preventing BFLA
BFLA occurs when users can execute functions (endpoints) they shouldn't have access to based on their roles.
- **Tenant Context Extraction**: The `TenantResolutionMiddleware` securely extracts the `X-Tenant-Id` header from incoming requests. Crucially, it queries the database (`TenantsDbContext`) to verify that the authenticated user genuinely belongs to that tenant.
- **Strict Role Validation**: The `[RequireTenant(params string[] roles)]` attribute acts as an authorization filter. It checks the successfully resolved `ITenantContext` to ensure the user holds the specific role (e.g., "admin", "employee") *within that specific tenant* before the controller logic is executed.

### Preventing BOLA (IDOR)
BOLA occurs when an application does not properly validate that the user is authorized to access the specific object ID they requested (e.g., manipulating a document ID or tenant ID in the URL).
- **Context-Bound Operations**: Controllers enforce BOLA protection by strictly associating operations with the authorized `ITenantContext`. Even if a route provides an `{id}` (like `DELETE /api/Documents/{id}` or `POST /api/Tenants/{id}/suspend`), the backend explicitly verifies that the requested object belongs to the user's validated `tenantContext.TenantId`. 
- **Example**: In the `SuspendTenant` endpoint, the `{id}` from the route is validated against `_tenantContext.TenantId`. This prevents a malicious tenant admin from suspending *another* tenant by injecting their own valid `X-Tenant-Id` header while manipulating the target `{id}` in the URL.

## White-Label Branding & Subdomain Routing
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
The `TenantBrandingService` (Angular) extracts the tenant slug from the subdomain on app startup and applies branding as CSS custom properties on the document root:

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

The upload request currently stores the file and creates an `Uploaded` metadata record. The employer document list displays the lifecycle state using readable API values. The background worker, text extraction, normalization, and chunking steps are intentionally separate follow-up increments so a document is never shown as `Ready` before ingestion has completed.

### Subdomain Routing
After a successful login:
- **User with no tenants**: stays on `localhost:4200` and is routed to `/onboarding`.
- **User with tenants (Phase 1)**: redirected to `{slug}.localhost:4200/{dashboard}` using their first membership's tenant slug.
- The CORS policy uses `SetIsOriginAllowed` with a wildcard to allow all `*.localhost:4200` origins.

> **Future Enhancement**: Persist the user's last selected tenant and use it for login routing instead of always defaulting to the first membership.
