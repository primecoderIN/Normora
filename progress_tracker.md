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
- [x] **Dark/Light Mode Contrast**: Configured PrimeNG to respond to the `.app-dark` selector. Injected Tailwind `dark:` variants across all HTML and inline templates. Fixed invalid dynamic class bindings in Angular components (`[class.x]`) that broke text visibility, and correctly structured pseudo-classes (`dark:hover:`) to prevent light-mode hover colors from overriding dark mode states.
- [x] **Dashboard Summary Translation Error**: Refactored the `GetDashboardSummary` EF Core query to fetch filenames into memory, bypassing backend SQL translation limits (`Substring`/`LastIndexOf`) that caused 500 Server Errors.
- [x] **PrimeNG Rendering Bug**: Reverted `DocumentListComponent`, `DepartmentListComponent`, and `UserGroupListComponent` from `<p-table>` to native HTML `<table>` elements with Angular's `@for` block to resolve persistent `PrimeTemplate` module import and view rendering errors in Angular 18 standalone components.
- [x] **Missing Animations**: Added `@angular/animations` and configured `provideAnimationsAsync()` in `app.config.ts` to satisfy PrimeNG dependencies.
- [x] **Document Upload Interception**: Bypassed PrimeNG's `p-fileupload` inability to use Angular HttpInterceptors by explicitly setting `withCredentials: true` and the `X-Tenant-Id` header on the `onBeforeSend` event, fixing 401 Unauthorized errors during uploads.
- [x] **SignalR Live Updates**: Added `withCredentials: true` to both `DocumentRealtimeService` and `NotificationRealtimeService` Hub connections, allowing the browser to attach the Duende BFF authentication cookie to SignalR negotiation requests, restoring live UI updates.

## ✅ Security Hardening
- [x] **BOLA Fix (SuspendTenant)**: Injected `ITenantContext` into `TenantsController` and validated the `{id}` route parameter against `tenantContext.TenantId` to prevent cross-tenant object manipulation.
- [x] **BFLA Defense**: `TenantResolutionMiddleware` validates tenant membership against the database on every request. The `[RequireTenant]` attribute enforces role-based access at the controller/action level.
- [x] **OWASP ZAP Remediations**: Configured Nginx to correctly emit strict security headers (`Permissions-Policy`, `X-Content-Type-Options`, `Content-Security-Policy`, etc.) across all cached static asset routes (`.js`, `.css`, `.html`), fixing a bug where `Cache-Control` overrides were silently stripping security policies.

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

## ✅ Completed Export Answers (Phase 17)
- [x] `IAnswerExporter` abstraction — defines `ExportResult`, `AnswerExportData`, `CitationExportData` records and the `IAnswerExporter` contract
- [x] `MarkdownAnswerExporter` — pure string-building `.md` exporter; no external dependencies
- [x] `PdfAnswerExporter` — QuestPDF-based PDF exporter with branded header, metadata, question/answer blocks, citations table, and page footer; QuestPDF 2025.5.0 added to Conversations module
- [x] `DocxAnswerExporter` — Open XML SDK-based `.docx` exporter with proper heading styles, indented quote block, citations table, and footer; DocumentFormat.OpenXml 3.3.0 added
- [x] `ExportSavedAnswerQuery` — MediatR query that resolves the preceding user question from the conversation, builds `AnswerExportData`, and delegates to the correct exporter
- [x] `GET /api/saved-answers/{id}/export?format=Markdown|Pdf|Docx` endpoint added to `SavedAnswersController`; streams file as a download attachment with a dated filename
- [x] Exporters registered as singletons in `ApplicationServiceExtensions`; QuestPDF community licence set in `PdfAnswerExporter` static constructor (lives in the module that owns the package)
- [x] `ExportFormat` union type (`'Markdown' | 'Pdf' | 'Docx'`) and `exportAnswer()` method added to `SavedAnswerService` — uses `responseType: 'blob'` and a programmatic `<a>` click to trigger the browser save-as dialog
- [x] Export dropdown UI added to each saved-answer card in `SavedAnswers` Angular component — download icon triggers an animated format picker (Markdown / PDF / Word); spinner shown during in-flight requests; `HostListener` on `document:click` closes the menu when clicking outside

## ✅ Completed Answer Feedback (Phase 18)
- [x] `MessageFeedback` domain entity with `TenantId`, `UserId`, `MessageId`, `Rating` (enum), and `Comment`
- [x] `MessageFeedbacks` `DbSet` and unique constraint on `(UserId, MessageId)` in `ConversationsDbContext`
- [x] Added `Feedbacks` navigation property to `Message`
- [x] EF Core migration `AddMessageFeedbacksEntity` created and applied automatically on API startup
- [x] `SubmitFeedbackCommand` + handler (validates Assistant role and user identity)
- [x] `POST /api/conversations/{id}/messages/{messageId}/feedback` endpoint
- [x] `MessageDto` updated with `Feedback` rating mapping inside `GetConversationQueryHandler`
- [x] Angular `MessageDto` updated with `feedback` property and `submitFeedback` method in `ConversationService`
- [x] Thumb Up/Down feedback buttons added to `ConversationChatComponent` next to bookmarks, with optimistic state toggling

## ✅ Completed Document Versioning (Phase 19)
- [x] Extracted version-specific fields (`MinioObjectName`, `Status`, `ExtractedText`) from `Document` into a new `DocumentVersion` entity.
- [x] Updated `Document` with a `Versions` collection; modified `DocumentChunk` to point to `DocumentVersionId`.
- [x] Applied `AddDocumentVersioning` EF Core migration and re-synced `DocumentsDbContext`.
- [x] Refactored `UploadDocumentCommand` to either create a new document + v1, or fetch an existing document by `DocumentId` and append a new, active version while deactivating older versions.
- [x] Refactored `SearchDocumentsQuery` and `RetrievalService` RAG pipelines to strictly join `DocumentVersions` filtered by `IsActive = true`.
- [x] Updated `DocumentsController.cs` to accept an optional `[FromForm] Guid? documentId` for the `UploadDocument` POST endpoint.
- [x] Updated Angular `DocumentService` with the `DocumentVersion` interface and included `versions` array inside `DocumentDto`.
- [x] Added "Version History" expansion table to the Angular `DocumentListComponent`, showing historical records and timestamps.
- [x] Added "Upload new version" action button on the Angular Employer Document List to upload new files retaining the same parent document identifier.
- [x] Added `isOutdated` flag to citations in `GetConversationQuery` and `GetSavedAnswersQuery` by cross-checking `documents."DocumentVersions"` for `IsActive = false`.
- [x] Added a warning banner in `ConversationChatComponent` and `SavedAnswers` UI to alert users when an AI answer relies on outdated source documents.

## ✅ Completed Document Soft Deletion (Phase 20)
- [x] Added `DeletedAt` and `DeletedByUserId` audit fields to the `Document` entity.
- [x] Configured a Global Query Filter in `DocumentsDbContext` (`d.DeletedAt == null`) so soft-deleted documents vanish from RAG lookups, searches, and UI lists without altering individual MediatR queries.
- [x] Refactored `DeleteDocumentCommand` to perform a soft delete and broadcast a `DocumentDeleted` SignalR event instead of a hard delete.
- [x] Updated `DocumentsController` to resolve the Keycloak `sub` claim for the `DeletedByUserId` audit field.
- [x] Added `PurgeDeletedDocumentsJob` scheduled via Hangfire to run nightly at 3:00 AM UTC, leveraging `IgnoreQueryFilters()` to physically purge MinIO files and database rows older than the 30-day grace period.
- [x] Updated Angular `DocumentRealtimeService` with a `DocumentDeleted` listener.
- [x] Extracted inline soft-delete confirmation strip into `document-list.component.ts` and integrated optimistic UI updates in `documents.ts`.

## ✅ Completed Employee Management & Directory (Phase 21)
- [x] Added `RemovedAt` and `RemovedByUserId` to `TenantMembership` entity.
- [x] Extended `TenantsDbContext` with a Global Query Filter to automatically exclude removed employees from `TenantResolutionMiddleware` and downstream queries.
- [x] Upgraded `GetTenantUsersQuery` to `GetTenantEmployeesQuery`, implementing `.Skip().Take()` pagination, dynamic search filters, and including navigational properties to attach `Departments` and `UserGroups`.
- [x] Implemented `RemoveEmployeeCommand` with local-first soft-deletion and a safeguard protecting the last Workspace Admin from being removed (`CannotRemoveLastAdmin`).
- [x] Added a best-effort API sync via `IHttpClientFactory` to Keycloak that gracefully logs errors without breaking the local database transaction.
- [x] Extracted the `Employees` view template into `employees.html` and rebuilt the table with a PrimeNG `p-paginator`, search input, and a department filter dropdown.
- [x] Replaced the static ellipsis button with a fully functional `p-menu` triggering an inline optimistic confirmation strip for removing an employee with a single click.

## ✅ Completed Employee Profile (Phase 22)
- [x] Backend: Updated `CurrentUserDto` and `GetCurrentUserQuery` to include the user's `CreatedAt` (member since) date.
- [x] Frontend: Created a dedicated `ProfileComponent` for employees to view their personal details and active workspaces.
- [x] Integrated Keycloak account management URL into `AuthService.manageAccount()` to let users update their password/MFA securely.
- [x] Added Profile navigation link to the Employee layout sidebars (both desktop and mobile views).
- [x] Updated `app.routes.ts` with the new `/employee/profile` route protected by auth and role guards.

## ✅ Completed Document Preview (Phase 23)
- [x] Backend: Added `GetDocumentChunkQuery` and an API endpoint (`GET /api/documents/chunks/{chunkId}`) to retrieve individual document chunks.
- [x] Security: Enforced strict BOLA checks to validate tenant ownership and department-level visibility before returning chunk content.
- [x] Frontend: Created `DocumentChunkPreviewDto` and integrated the API call into `DocumentService`.
- [x] Implemented a slide-out preview drawer using PrimeNG `<p-sidebar>` in both `ConversationChatComponent` and `SavedAnswers` components.
- [x] Made AI citations clickable, allowing users to instantly view the original source text in the sidebar without leaving the chat interface.
