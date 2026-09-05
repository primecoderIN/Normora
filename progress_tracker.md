# Implementation Progress Tracker

This document tracks all features, infrastructure, and tasks that have been successfully implemented so far in the Normora project.

## ✅ Completed Infrastructure & DevOps
- [x] Initial scaffold of ASP.NET Core 10 modular monolith server
- [x] Initial scaffold of Angular client application
- [x] Dockerization of the API Server (Multi-stage build)
- [x] Dockerization of the Angular Client (Multi-stage build with Nginx)
- [x] Environment variable configuration mechanism (`.env` file auto-generation)
- [x] Docker Compose setup including:
  - PostgreSQL database
  - Keycloak Identity and Access Management
  - MinIO Object Storage
  - .NET API container
  - Angular Client container
- [x] Creation of `start.ps1` and `stop.ps1` automation scripts for local environment spin-up

## ✅ Completed Tenant Management & Data Isolation
- [x] Create TenantsModule (Tenant, TenantMembership, User)
- [x] Create TenantsDbContext (isolated from main db)
- [x] Setup Tenant Resolution Middleware (via header)
- [x] Implement ITenantContext & RequireTenantAttribute for authorization
- [x] Configure Global Query Filters in AppDbContext for tenant isolation

## ✅ Completed Refactoring & Code Organization
- [x] Flattened backend directory structure (removed redundant `src` folder from `server`)
- [x] Updated Solution file (`Normora.slnx`) and Dockerfiles to reflect the flattened structure

## ✅ Completed Documentation
- [x] Created `docs/overview.md` for high-level project summary
- [x] Created `docs/architecture.md` detailing the modular monolith and tech stack
- [x] Created `docs/getting_started.md` for new developer onboarding
- [x] Updated root `README.md` with badges, impressive architectural summary, and quick links to documentation

## ✅ Completed Frontend Features (Angular + PrimeNG)
- [x] Bootstrapped Angular 17+ with TailwindCSS and PrimeNG UI library
- [x] Integrated `angular-auth-oidc-client` for Keycloak JWT Authentication
- [x] Implemented Auth callback routing and protected Route Guards
- [x] Designed responsive Login UI
- [x] Designed Employer Dashboard UI with dynamic sidebars and layout routing
- [x] Implemented Document Management UI with `<p-fileupload>` and PrimeNG tables
- [x] Used new Angular `@for` control flow and dynamic class bindings for file types

## ✅ Completed Backend Features & Integration
- [x] Set up Entity Framework Core migrations with PostgreSQL and created the `Documents` table
- [x] Integrated `MinioClient` for direct S3-compatible object storage uploads
- [x] Implemented CQRS pipeline (MediatR) for `UploadDocumentCommand` and `GetEmployerDocumentsQuery`
- [x] Configured backend JWT token validation (mapped nested `realm_access.roles` into ASP.NET Core `ClaimTypes.Role`)
- [x] Secured API endpoints with `[Authorize(Roles = "employer")]`
- [x] Successfully routed client Docker requests to Keycloak and MinIO through internal container DNS (`keycloak:8080`, `minio:9000`)
- [x] Finalized end-to-end document upload, storage, database tracking, and UI retrieval flow
- [x] Implemented robust invitation token generation and 48-hour expiration logic (`AcceptInvitationCommandHandler`)
- [x] Added Just-In-Time (JIT) provisioning to sync user profiles (Name/Email) from Keycloak to PostgreSQL on every login (`GetCurrentUserQueryHandler`)
- [x] Added explicit document processing states (`Uploaded`, `Processing`, `Ready`, `Failed`) with a data-preserving EF migration
- [x] Exposed document states as readable JSON values and displayed them in the employer document list
- [x] Added PostgreSQL-backed Hangfire worker and tenant-scoped `DocumentProcessingJob` boundary
- [x] Added Apache Tika extraction with persisted text and `Ready`/`Failed` transitions
- [x] Added normalized, tenant-owned document chunks with idempotent retry behavior
- [x] Added tenant-validated SignalR document status events for upload and processing transitions
- [x] Added optional Gemini chunk embeddings with PostgreSQL pgvector storage

## ✅ Recently Completed Enhancements
- [x] **Social Login Integrations (Google & GitHub)**: Configured Keycloak Identity Providers and built `kc_idp_hint` auto-redirect logic in the Angular Login UI for both platforms.
- [x] **pgAdmin Integration**: Added pgAdmin 4 to the Docker Compose stack for easy database management, complete with automated `.env` setup.
- [x] **Authentication UX**: Upgraded PrimeNG imports to the modern standalone syntax (v18+). Added logout capability directly to the Onboarding component.
- [x] **Invitation Flow Resiliency**: Intercepted the OAuth callback to gracefully handle pending invitations (`localStorage.getItem('pending_invitation')`), overriding standard routing if the user is accepting an invite. Implemented granular UI error handling for expired links.
- [x] **Secrets Management**: Removed `realm-export-live.json` from git tracking to prevent leaking production secrets.

## ✅ Security Hardening
- [x] **BOLA Fix (SuspendTenant)**: Injected `ITenantContext` into `TenantsController` and validated the `{id}` route parameter against `tenantContext.TenantId` to prevent cross-tenant object manipulation.
- [x] **BFLA Defense**: `TenantResolutionMiddleware` validates tenant membership against the database on every request. The `[RequireTenant]` attribute enforces role-based access at the controller/action level.

## ✅ White-Label Branding & Subdomain Routing
- [x] Created `TenantBranding` domain entity (separate table, 1-to-1 with `Tenant`) with `PrimaryColor`, `SecondaryColor`, `LogoUrl`, `FaviconUrl`
- [x] Added `Branding` navigation property to `Tenant.cs`
- [x] Registered `TenantBranding` in `TenantsDbContext` with 1-to-1 EF Core configuration
- [x] Created `GetTenantBrandingQuery` + handler (fetches by slug, anonymous)
- [x] Added `[AllowAnonymous] GET /api/tenants/branding/{slug}` endpoint to `TenantsController`
- [x] Updated CORS to allow wildcard subdomains (`*.localhost:4200`) via `SetIsOriginAllowed`
- [x] Generated EF Core migration `AddTenantBranding`
- [x] Created Angular `TenantBrandingService` — reads slug from subdomain, fetches branding, injects CSS variables, handles subdomain redirect
- [x] Updated `app.ts` routing: users with tenants are redirected to `{slug}.localhost:4200`, users with no tenants stay on base `localhost:4200/onboarding`
