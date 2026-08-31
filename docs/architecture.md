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
