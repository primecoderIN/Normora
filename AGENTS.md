# Normora - AI Agent Coding Guidelines

Welcome to the Normora repository. When making changes to this codebase, you MUST adhere to the following best practices and structural patterns. These are strictly enforced to maintain a secure, responsive, and consistent application.

## 1. No Magic Strings (Error Messages & Exceptions)
We use a centralized constants approach to avoid "magical error strings" across both the frontend and backend.
- **Backend**: Do not throw exceptions with hardcoded strings (e.g., `throw new Exception("Error");`). Instead, always use `Normora.Shared.Constants.ApiMessages` (e.g., `throw new InvalidOperationException(ApiMessages.TenantContextMissing);`).
- **Frontend**: Do not hardcode error toasts or alert messages. Always use the central constant object: `ApiMessages` from `client/src/app/core/constants/api-messages.ts`.

## 2. Security as Utmost Priority (BOLA & BFLA)
Security is the highest priority in this codebase. We have strict exception patterns for handling authorization failures to prevent enumeration and unauthorized access:
- **BOLA (Broken Object Level Authorization)**: Whenever an object is requested that the user does not own or have access to, throw a `BolaException()`. This is mapped globally to a `404 Not Found` to prevent attackers from enumerating resource IDs. 
- **BFLA (Broken Function Level Authorization)**: Whenever a user lacks the permissions to execute an action/endpoint, throw a `BflaException()`. This is mapped globally to a `403 Forbidden`.
- **Controllers**: Do NOT return explicit `NotFound("Specific item missing")` strings. Always return a generic `ApiMessages.NotFound` to prevent information leakage.
- **Controllers**: Do NOT return empty `Forbid()` responses. Always return `StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Failure(ApiMessages.Forbidden));` so the frontend interceptor can parse the structured JSON properly.

## 3. Tailwind CSS v4 Syntax
This project uses Tailwind CSS v4. Ensure you follow v4 syntax rules.
- **Important Modifier**: The `!important` modifier is a **suffix**, not a prefix. Use `class!` (e.g., `border-none!`, `shadow-none!`, `outline-none!`), and **never** `!class` (e.g., `!border-none`). 

## 4. UI / UX & Responsiveness
A great user experience is paramount. Interfaces should feel premium, fast, and polished.
- **Mobile First & Responsive**: All layouts must be fully responsive across mobile, tablet, and desktop. Check small screens before applying `sm:`, `md:`, and `lg:` rules.
- **Breakpoints**: 
  - Use `sm:` modifiers for tablet/phablet overrides.
  - Use `md:` or `lg:` for desktop layouts.
  - Example: `grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`
- **Dark Mode**: All components MUST fully support dark mode. Use `dark:` variants for every background, text, border, and ring color you add (e.g., `bg-white dark:bg-slate-900 text-slate-900 dark:text-white border-slate-200 dark:border-slate-700`).
- **Scrollbars**: Apply the `.custom-scrollbar` class for overflow areas (like lists or chats) to ensure a polished look across browsers.

## 5. Modern Architecture & Practices
Always default to the most modern, supported capabilities of the frameworks in use.
- **C# Primary Constructors**: ALWAYS use primary constructors for dependency injection in classes instead of explicitly declaring fields and standard constructors.
- **Angular Standalone Components**: All UI should be built using Angular standalone components.
- **Angular Signals**: Leverage Angular Signals (`signal`, `computed`) for synchronous state representation rather than complex RxJS chains, especially in UI components.

## 6. Quality, Scalability, and SEO
Ensure all code written is robust and ready for production at scale.
- **SEO Optimized**: Use semantic HTML (`<header>`, `<main>`, `<article>`, `<section>`). Maintain a logical heading hierarchy (one `<h1>` per page, followed by `<h2>`, `<h3>`). Add descriptive `alt` tags to images and avoid relying purely on client-side state for readable content where search engines index.
- **Secure**: Beyond BOLA/BFLA, ensure all user inputs are strictly validated (using FluentValidation in the API and Angular Reactive Forms). Prevent XSS by relying on Angular's built-in escaping; avoid `[innerHTML]` unless absolutely necessary and sanitized.
- **Testable**: Write small, decoupled components and services following the Single Responsibility Principle. All dependencies must be injected via constructors to allow easy mocking in unit tests. Avoid hidden global state.
- **Highly Scalable**: API services must be strictly stateless to support horizontal scaling. Implement pagination for all list-based endpoints. Keep JSON payloads minimal. In the database layer, ensure queries are optimized and properly indexed (no N+1 query problems in EF Core).
- **Maintainable Code**: Use strict TypeScript and C# typing (avoid `any` or `dynamic`). Use clear, descriptive variable names without cryptic abbreviations. Document complex business logic with comments, but let clean code speak for itself otherwise. DRY (Don't Repeat Yourself) is critical, but avoid premature abstraction.

## 7. Modular Monolith Architecture & CQRS Patterns
Normora strictly enforces a modular monolith architecture. All backend code must adhere to these structural boundaries:

### Module Boundaries & Active Contexts
- The active business modules in `server/Modules/` are:
  - **`Tenants`**: Multi-tenancy, memberships, user profiles, user groups, invitations, departments, white-label branding, audit logs, and subscription billing.
  - **`Documents`**: Ingestion, chunking, embeddings, text extraction, search, metrics, and MCP endpoints.
  - **`Conversations`**: Conversational RAG, streaming, citations, tool execution, suggestions, and saved answers.
- **No Ghost Modules**: Never create or re-introduce standalone `Auth` or `Users` modules. Authentication is handled globally by the host BFF (`Normora.Api`), while user models, memberships, and user queries live inside `Modules/Tenants`.
- **Module Composition Roots**: Every module contains a `{ModuleName}ModuleExtensions.cs` registering its internal services, handlers, and persistence into the DI container. The host calls `services.Add{ModuleName}Module(...)`.

### Decentralized Controllers
- Controllers MUST reside directly inside their owning module's `API/` directory:
  - `server/Modules/Tenants/API/{Feature}Controller.cs`
  - `server/Modules/Documents/API/{Feature}Controller.cs`
  - `server/Modules/Conversations/API/{Feature}Controller.cs`
- `Normora.Api` contains NO business controllers. It serves strictly as the HTTP/BFF host, managing middleware, SignalR hubs, authentication pipelines, and Swagger/Scalar documentation.

### CQRS Folder-Per-Operation Structure
Inside each module's `Application/{Feature}/` directory, commands and queries MUST be organized into dedicated operation folders matching the operation name:

```
Application/{Feature}/
├── Commands/
│   └── {OperationName}/
│       ├── {OperationName}Command.cs
│       ├── {OperationName}CommandHandler.cs
│       └── {OperationName}CommandValidator.cs  (when FluentValidation is required)
├── Queries/
│   └── {OperationName}/
│       ├── {OperationName}Query.cs
│       └── {OperationName}QueryHandler.cs
└── DTOs/
    └── {Feature}Dtos.cs
```

- **File Separation**: The Command/Query record, its Handler class, and its Validator class must each reside in separate files within the operation folder. Do NOT inline handlers or validators in the command/query file.
- **DTO Placement**: Data transfer objects specific to a feature must reside in `Application/{Feature}/DTOs/`.

### Persistence & Entity Configurations
- **Lean DbContexts**: DbContext classes must remain lean and avoid large inlined Fluent API configurations in `OnModelCreating`.
- **Dedicated Configurations**: Each entity mapping MUST be defined in its own `IEntityTypeConfiguration<T>` class inside `server/Modules/{ModuleName}/Persistence/Configurations/{Entity}Configuration.cs`.
- **Parameterless Entity Configurations**: All `IEntityTypeConfiguration<T>` classes MUST be parameterless and have a public default constructor. Never pass scoped dependencies (such as `ITenantContext`) to configuration constructors, as this breaks EF Core assembly scanning (`ApplyConfigurationsFromAssembly`). Global multitenant query filters must bind dynamically to the DbContext's instance property (e.g. `_tenantContext.TenantId`) inside the DbContext.
- **Auto-Discovery**: The module DbContext applies all configurations automatically via:
  ```csharp
  modelBuilder.ApplyConfigurationsFromAssembly(typeof({ModuleName}DbContext).Assembly);
  ```
- **100% DbContext Encapsulation**: Module DbContexts are strictly private to their owning module. API controllers, middleware, hubs, and other modules must NEVER inject another module's DbContext. Cross-module reads must use MediatR contracts in `Normora.Shared.Contracts.*`.

### Cross-Module Communication & Boundaries
- **MediatR Contracts for Cross-Module Calls**: Cross-module operations must strictly communicate using MediatR contracts in `Normora.Shared.Contracts.{ModuleName}`. Never inject internal services or interfaces across module boundaries:
  - **Cross-Module Hybrid Retrieval**: `Conversations` (such as `ConversationTurnPreparer` and `SearchCompanyDocumentsTool`) queries knowledge chunks in `Documents` via `PerformHybridRetrievalQuery` in `Normora.Shared.Contracts.Documents` — never directly inject `IRetrievalService`.
  - **Cross-Module MCP Tools**: `Documents` (such as `McpController`) discovers and executes tools residing in `Conversations` via `GetMcpToolsQuery` and `ExecuteMcpToolCommand` in `Normora.Shared.Contracts.Conversations` — never directly inject `IAgentToolRegistry`.
- **No Module Internals in Shared**: `Normora.Shared` must remain clean and provider-neutral. Module-specific abstractions (like `ITextEmbeddingService`), domain constants (like `RagConstants`, `ConversationConstants`), and domain entities must reside strictly inside their owning module, not in `Normora.Shared`.
- **Decoupled External Identity (`ExternalUserId`)**: Always reference external OIDC identity provider subject IDs as `ExternalUserId` (e.g. `currentUser.ExternalUserId`). Never re-introduce vendor-specific names like `KeycloakUserId` in domain entities, CQRS handlers, queries, or audit behaviors.

### Host, Worker & Infrastructure Boundaries
- **`Normora.Api`**: The HTTP/BFF host. It serves strictly as the application host, managing middleware, authentication pipelines, OpenAPI/Scalar, and root DI composition. Contains NO business controllers.
- **`Normora.Worker`**: Independent generic host executing Hangfire background jobs (ingestion, embeddings, purge). Must NEVER reference `Normora.Api`. References `Normora.Infrastructure` and necessary module contracts.
- **`Normora.Infrastructure`**: Host-neutral class library containing cross-cutting infrastructure (Hangfire, Redis distributed caching, OpenTelemetry, SignalR hub adapters, database migration runner) and external third-party provider adapters. Must NEVER reference executable hosts (`Normora.Api`, `Normora.Worker`).

### External Provider Boundary (Provider Pattern)
- **Zero Third-Party Vendor SDKs in Business Modules**: Business modules (`Documents`, `Conversations`, `Tenants`) must NEVER reference external third-party vendor SDK packages (such as `Minio`, Google AI packages, AWS SDK, Azure SDK).
- **Domain Interfaces (Ports)**: Modules declare provider-neutral domain contracts:
  - `Documents`: `IDocumentStorageService`, `IDocumentTextExtractor`, `ITextEmbeddingService`
  - `Conversations`: `ITextGenerationService`, `IQueryRewriterService`, `ISuggestionsService`
  - `Tenants`: `IBrandingStorageService`
- **Infrastructure Adapters (Providers)**: Third-party implementations live strictly in `server/Normora.Infrastructure/Providers/`:
  - `Storage/`: `MinioDocumentStorageService`, `MinioBrandingStorageService`, `MinioOptions`
  - `Gemini/`: `GeminiEmbeddingService`, `GeminiTextGenerationService`, `GeminiQueryRewriterService`, `GeminiSuggestionsService`, `GeminiOptions`, `GeminiModels`
  - `DocumentExtraction/`: `TikaDocumentTextExtractor`, `TikaOptions`
- **Dedicated Provider DI**: Provider wiring is registered via `ProviderServiceExtensions.cs` (`AddExternalProviders`, `AddStorageProviders`, `AddAiProviders`, `AddDocumentExtractionProviders`), keeping `DatabaseServiceExtensions.cs` strictly focused on PostgreSQL, DbContexts, and Hangfire.

### Transactional Outbox Pattern for Document Uploads
- **Atomic Metadata & Work Dispatch**: Document upload operations in `UploadDocumentCommandHandler` must insert the document metadata, document version, and a `DocumentProcessingOutboxMessage` in the same database transaction.
- **Durable Processing**: The handler signals `IDocumentProcessingWakeup` as an asynchronous wake-up. Hangfire enqueues `HangfireDocumentProcessingOutboxDispatcher` to claim and dispatch pending outbox messages, backed by a minutely recurring Hangfire job for automatic recovery. Downstream notification or queue dispatch failures must never fail an already-committed upload.


