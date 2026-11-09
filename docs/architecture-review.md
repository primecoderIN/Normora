# Normora Architecture Review

**Review date:** 2026-11-09  
**Scope:** Backend modular boundaries, hosting/composition, upload/job reliability, architecture fitness tests, and alignment between implementation and architecture documentation. This is a source-based architecture review, not a penetration test or a full operational/load assessment.

## Executive summary

Normora has a sound modular-monolith direction: business modules are separate projects, module persistence is owned by those projects, and cross-module interactions are generally represented by shared MediatR contracts. The review identified four issues; all four have since been addressed. The upload flow now uses a persistent outbox, the worker depends on a host-neutral infrastructure project rather than the API, architecture tests enforce those boundaries, and documentation/comments reflect the implemented rendering and migration lifecycles.

## Findings

### A-1 — Upload compensation can delete a committed document's object

**Severity: High**  
**Confidence: High**

**Status: Resolved**

`UploadDocumentCommandHandler` writes the file to object storage, then saves the `Document` and `DocumentVersion` to PostgreSQL. The same `try` block continues through cache invalidation, SignalR notification, and Hangfire enqueueing. Its catch block always attempts to delete the stored object.

If `SaveChangesAsync` succeeds but any later operation fails—for example, Hangfire is unavailable when enqueueing—the catch deletes the object while leaving the committed database rows in place. The document then points to a missing file and retries may not repair that state. Conversely, if cleanup itself fails, that failure is silently swallowed and can leave an orphaned object.

**Evidence:** [UploadDocumentCommandHandler.cs](../server/Modules/Documents/Application/Documents/Commands/UploadDocument/UploadDocumentCommandHandler.cs), especially the object upload, database save, post-save operations, and compensation.

**Resolution:** Compensation now applies only to failures while persisting metadata; cleanup failures are logged. The upload and its processing outbox record are saved together. The queue adapter removes an outbox record only after enqueueing, is also scheduled periodically for recovery, and uses an atomic status claim to make duplicate dispatch safe. Post-commit notification/wakeup failures are logged without reporting a persisted upload as failed.

### A-2 — Worker is coupled to the HTTP API project

**Severity: Medium**  
**Confidence: High**

**Status: Resolved**

`Normora.Worker` has a project reference to `Normora.Api` and imports API extension methods for configuration, database, application services, and telemetry. This makes the background process depend on the web host's composition surface rather than only on the modules and infrastructure it needs. It also makes API project changes more likely to affect worker builds, deployment artifacts, and dependency resolution.

**Evidence:** [Normora.Worker.csproj](../server/Normora.Worker/Normora.Worker.csproj) line 13 and [Program.cs](../server/Normora.Worker/Program.cs) lines 4 and 14–20.

**Resolution:** Shared composition and adapters were moved into `Normora.Infrastructure`. The worker now references that project and its required module(s), not `Normora.Api`; the Docker build restores the new project. Hangfire-specific job wrappers, retry configuration, and dispatch live in the infrastructure adapter rather than the Documents module.

### A-3 — Architecture tests do not enforce the full boundary policy

**Severity: Medium**  
**Confidence: High**

**Status: Resolved**

The tests enforce configuration constructors and controller constructor dependencies, but cross-module checks inspect only constructor parameter types, and only for the Documents↔Conversations pair. They do not detect module dependencies exposed through fields, properties, method parameters/returns, base types, attributes, or static members; nor do they check project-reference direction or the Tenants module boundary. As a result, the current suite can pass while non-constructor coupling is introduced.

**Evidence:** [ModularMonolithArchitectureTests.cs](../server/Normora.Architecture.Tests/ModularMonolithArchitectureTests.cs), especially lines 63–100.

**Resolution:** Architecture tests now assert that no business module references another module, the worker does not reference the API host, and infrastructure references neither executable host. Existing constructor-level rules remain in place.

### A-4 — Architecture documentation and implementation have drifted

**Severity: Low**  
**Confidence: High**

**Status: Resolved**

The architecture overview calls the frontend a standard SPA, while Angular is configured with server output and a server-rendered root route; the wildcard route uses client rendering. Also, module composition-root comments say migrations run during host startup, but the API invokes the migration runner only when started with `--migrate`. The overview itself correctly describes the one-off migration mode, so these descriptions conflict.

**Evidence:** [architecture.md](./architecture.md) lines 3 and 20; [angular.json](../client/angular.json) `outputMode`; [app.routes.server.ts](../client/src/app/app.routes.server.ts); [Program.cs](../server/Normora.Api/Program.cs) lines 63–68; migration comments in [DocumentsModuleExtensions.cs](../server/Modules/Documents/DocumentsModuleExtensions.cs) and [TenantsModuleExtensions.cs](../server/Modules/Tenants/TenantsModuleExtensions.cs).

**Resolution:** The architecture overview now describes the hybrid server/client rendering setup, documents the infrastructure project and worker boundary, and states that migrations run only in the explicit one-off process. Module migration comments now match that lifecycle.

## Positive architectural characteristics

- Business modules are separate projects and currently reference `Normora.Shared`, not one another directly.
- Each module owns its DbContext and migration implementation; the API migration runner invokes module-owned migrators.
- Controllers are placed in their owning modules, and the reviewed upload flow uses a shared contract for its cross-module quota check.
- The architecture document describes explicit tenancy, BFF, and deployment boundaries that provide a useful basis for stronger fitness tests.

## Validation

- `dotnet test server\Normora.Architecture.Tests\Normora.Architecture.Tests.csproj --no-restore` — passed, 10 tests.
- `dotnet build server\Normora.Api\Normora.Api.csproj --no-restore` — passed.
- `dotnet build server\Normora.Worker\Normora.Worker.csproj --no-restore` — passed.
