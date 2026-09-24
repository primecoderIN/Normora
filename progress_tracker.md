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
- [x] Synced documentation to reflect the latest UI changes, new access endpoints, and White-Label Branding capabilities

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
- [x] Added tenant-safe pgvector similarity retrieval with source chunk metadata
- [x] Added grounded Ask Normora endpoint with tenant-safe sources and no-answer behavior
- [x] Added basic employee Ask Normora chatbot screen for endpoint testing

## ✅ Completed Department & User Group-Based Answers System
- [x] Added `Department`, `UserGroup`, and junction entities to Tenants and Documents modules.
- [x] Implemented dynamic **Effective Department** resolution during JWT token generation.
- [x] Built Employer Settings UI for `Departments` and `User Groups` with full CRUD and assignment logic.
- [x] Enhanced Document Upload modal to support multi-select department assignments.
- [x] Updated Employer Document List to render department scope badges.
- [x] Secured Hybrid Search pipeline (AskNormora) to exclusively query chunks from **Company Wide** documents or documents mapped to the user's **Effective Departments**.

## ✅ Recently Completed Enhancements
- [x] **Social Login Integrations (Google & GitHub)**: Configured Keycloak Identity Providers and built `kc_idp_hint` auto-redirect logic in the Angular Login UI for both platforms.
- [x] **pgAdmin Integration**: Added pgAdmin 4 to the Docker Compose stack for easy database management, complete with automated `.env` setup.
- [x] **Authentication UX**: Upgraded PrimeNG imports to the modern standalone syntax (v18+). Added logout capability directly to the Onboarding component.
- [x] **Invitation Flow Resiliency**: Intercepted the OAuth callback to gracefully handle pending invitations (`localStorage.getItem('pending_invitation')`), overriding standard routing if the user is accepting an invite. Implemented granular UI error handling for expired links.
- [x] **Pending Invitations Banner**: Added a dynamic banner to the Employer and Employee layouts that surfaces pending invitations for users who bypass the onboarding screen (i.e. those with existing tenant memberships).
- [x] **Personal Workspace Uploads**: Modified the Document Upload modal to conditionally hide the "Departments" assignment dropdown when the active workspace is a Personal Workspace, reflecting the lack of departmental structure in that context.
- [x] **Secrets Management**: Removed `realm-export-live.json` from git tracking to prevent leaking production secrets.

## ✅ Bug Fixes & UI Stabilization
- [x] **PrimeNG Rendering Bug**: Reverted `DocumentListComponent`, `DepartmentListComponent`, and `UserGroupListComponent` from `<p-table>` to native HTML `<table>` elements with Angular's `@for` block to resolve persistent `PrimeTemplate` module import and view rendering errors in Angular 18 standalone components.
- [x] **Missing Animations**: Added `@angular/animations` and configured `provideAnimationsAsync()` in `app.config.ts` to satisfy PrimeNG dependencies.
- [x] **Document Upload Interception**: Bypassed PrimeNG's `p-fileupload` inability to use Angular HttpInterceptors by explicitly setting `withCredentials: true` and the `X-Tenant-Id` header on the `onBeforeSend` event, fixing 401 Unauthorized errors during uploads.
- [x] **SignalR Live Updates**: Added `withCredentials: true` to both `DocumentRealtimeService` and `NotificationRealtimeService` Hub connections, allowing the browser to attach the Duende BFF authentication cookie to SignalR negotiation requests, restoring live UI updates.

## ✅ Security Hardening
- [x] **BOLA Fix (SuspendTenant)**: Injected `ITenantContext` into `TenantsController` and validated the `{id}` route parameter against `tenantContext.TenantId` to prevent cross-tenant object manipulation.
- [x] **BFLA Defense**: `TenantResolutionMiddleware` validates tenant membership against the database on every request. The `[RequireTenant]` attribute enforces role-based access at the controller/action level.

## ✅ White-Label Branding & Workspace Routing
- [x] Created `TenantBranding` domain entity (separate table, 1-to-1 with `Tenant`) with `PrimaryColor`, `SecondaryColor`, `LogoUrl`, `FaviconUrl`
- [x] Added `Branding` navigation property to `Tenant.cs`
- [x] Registered `TenantBranding` in `TenantsDbContext` with 1-to-1 EF Core configuration
- [x] Created `GetTenantBrandingQuery` + handler (fetches by slug, anonymous)
- [x] Added `[AllowAnonymous] GET /api/tenants/branding/{slug}` endpoint to `TenantsController`
- [x] Generated EF Core migration `AddTenantBranding`
- [x] Created Angular `TenantBrandingService` — applies branding dynamically using CSS variables
- [x] Updated `app.ts` and `app.routes.ts`: Migrated away from Subdomain Routing to **Path-based Workspace Routing** (`/app/workspaces/:slug`). Users are seamlessly redirected to their workspace URL after login, and the correct branding is applied based on the URL path.

## ✅ Completed Conversational RAG Architecture
- [x] Phase 5: Created Conversations API (POST /api/conversations/{id}/messages) for handling RAG messaging.
- [x] Phase 6: Built Angular Conversational UI with Tailwind, smart/dumb components, and Signals state management.
- [x] Phase 7: Integrated OpenTelemetry metrics and tracing for the RAG pipeline (tokens, latency, dependencies).
- [x] Phase 8: Finalized architecture documentation and added inline codebase documentation.

## ✅ Completed Saved Answers
- [x] `SavedAnswer` domain entity (`TenantId`, `UserId`, `MessageId`, `ConversationId`)
- [x] Registered `SavedAnswer` in `ConversationsDbContext` with tenant global query filter and unique index `(UserId, MessageId)`
- [x] `AddSavedAnswers` EF Core migration generated (applied automatically on API startup)
- [x] `SaveAnswerCommand` — idempotent save of any assistant message
- [x] `UnsaveAnswerCommand` — idempotent removal by MessageId
- [x] `GetSavedAnswersQuery` — paginated list with message content + citations joined
- [x] `SavedAnswerDto` + `CitationDto` records
- [x] `SavedAnswersController` — `GET /api/saved-answers`, `POST /api/saved-answers`, `DELETE /api/saved-answers/{messageId}`
- [x] Angular `SavedAnswerService` — thin HttpClient wrapper
- [x] Bookmark (save/unsave) button added to every assistant message bubble in `ConversationChatComponent` with optimistic toggle
- [x] Full `SavedAnswers` Angular page — loading skeleton, empty state, answer cards with markdown + citations, optimistic unsave, load-more pagination

## ✅ Codebase Quality Audit & Standardization
- [x] Conducted comprehensive static and architectural audit against enterprise industry best practices.
- [x] Integrated Scalar UI for interactive OpenAPI documentation, documenting 8 core controllers with XML comments and `[ProducesResponseType]`.
- [x] Standardized all endpoint responses to use a unified `ApiResponse<T>` envelope.
- [x] Patched `GlobalExceptionHandler` to enforce the `ApiResponse<T>` schema on all generic HTTP errors and validation failures.
- [x] Eliminated "magic strings" in the backend by extracting authorization scopes and global error responses into `TenantRoles` and `ApiMessages` constants.
- [x] Eliminated "magic strings" in the Angular frontend by extracting role strings into a `tenant-roles.ts` constants file.
- [x] Resolved static analyzer and compiler warnings (e.g. `CA2024` and `CS8603`) for a zero-warning build output.
