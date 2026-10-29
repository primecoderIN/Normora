# Database Schema & Entity Relationships

> **Conventions:**
> - All primary keys are `uniqueidentifier` (GUID), client-generated unless noted.
> - Soft deletes are handled via EF Core global query filters where appropriate.
> - The application uses a Modular Monolith architecture. Each module (`Tenants`, `Documents`, `Conversations`, `Users`) encapsulates its own `DbContext` and schema.
> - Cross-module foreign keys are mapped by ID only to allow EF Core navigation without duplicating table definitions across DbContexts.

---

## Module: Tenants (`tenants` schema)
Manages multi-tenancy, white-label branding, invitations, and hierarchical Role-Based Access Control (Departments and User Groups).

### `Tenants`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `Name` | `varchar(160)` | ❌ | |
| `Slug` | `varchar(180)` | ❌ | **Unique** (`UX_Tenants_Slug`). Used for workspace path routing (URL). |
| `Status` | `int` | ❌ | Enum: `Active`, `Suspended` |
| `CreatedAt` | `datetime2` | ❌ | UTC timestamp |

**Indexes:** `UX_Tenants_Slug` (Unique)

---

### `Users`
Shadow user identity records synchronized from the external OpenID Connect Identity Provider during login / JIT auto-provisioning. Decoupled from any vendor-specific schema.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `ExternalUserId` | `nvarchar(450)` | ❌ | **Unique** (`IX_Users_ExternalUserId`). The external OIDC `sub` subject identifier. |
| `Email` | `nvarchar(256)` | ✅ | Synchronized from OIDC `email` claim |
| `DisplayName` | `nvarchar(256)` | ✅ | Synchronized from OIDC `name` / `preferred_username` claim |
| `CreatedAt` | `datetime2` | ❌ | UTC timestamp |
| `UpdatedAt` | `datetime2` | ✅ | UTC timestamp of last profile sync |

**Indexes:** `IX_Users_ExternalUserId` (Unique)

---

### `TenantMemberships`
The junction table linking physical `Users` to `Tenants` with a context-specific role.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`) |
| `UserId` | `uniqueidentifier` | ❌ | FK → `tenants.Users.Id` (`Cascade`) |
| `Role` | `int` | ❌ | Enum: `Employer`, `Employee` |
| `CreatedAt` | `datetime2` | ❌ | |
| `RemovedAt` | `datetime2` | ✅ | Soft-delete timestamp (Phase 21) |
| `RemovedByUserId` | `uniqueidentifier` | ✅ | Audit trail for the admin who performed the removal |

**Indexes:** 
| Name | Columns | Unique | Notes |
|---|---|---|---|
| `UX_TenantMemberships_TenantId_UserId` | `(TenantId, UserId)` | ✅ (Partial) | Filtered where `"RemovedAt" IS NULL` to permit clean member re-invitation |
| `IX_TenantMemberships_UserId` | `UserId` | ❌ | Fast membership lookups by User |

---

### `TenantBranding`
1-to-1 with Tenant for white-labeling configurations.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`) |
| `PrimaryColor` | `varchar(20)` | ✅ | Hex color code |
| `SecondaryColor` | `varchar(20)` | ✅ | Hex color code |
| `LogoUrl` | `nvarchar(max)` | ✅ | |
| `FaviconUrl` | `nvarchar(max)` | ✅ | |

**Indexes:** `UX_TenantBranding_TenantId` (Unique)

---

### `Departments`
Logical subdivisions within a tenant used to restrict document access.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`) |
| `Name` | `nvarchar(200)` | ❌ | |

---

### `UserGroups`
Logical groupings of users that inherit access to specific departments.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`) |
| `Name` | `nvarchar(200)` | ❌ | |

---

### Group & Department Junctions
- `UserGroupMemberships`: Links `TenantMembership.Id` ↔ `UserGroup.Id`
- `UserGroupDepartments`: Links `UserGroup.Id` ↔ `Department.Id`
- `MembershipDepartments`: Links `TenantMembership.Id` ↔ `Department.Id` (Direct assignments)

---

### `AuditLogs`
Records all critical administrative and domain events executed within the tenant workspace.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`) |
| `UserId` | `uniqueidentifier` | ❌ | The user ID who performed the action |
| `Action` | `varchar(100)` | ❌ | Action name (e.g., `Document.Upload`, `Employee.Invite`) |
| `TargetType` | `varchar(100)` | ✅ | Type of entity affected (e.g., `Document`, `Employee`) |
| `TargetId` | `varchar(100)` | ✅ | Entity ID affected |
| `Metadata` | `text` | ✅ | JSON payload containing event context |
| `CreatedAtUtc` | `datetime2` | ❌ | UTC timestamp |

**Indexes:**
- `IX_AuditLogs_TenantId_CreatedAtUtc`
- `IX_AuditLogs_TenantId_Action`

---

### `TenantPlans`
Defines subscription tiers, quota boundaries, workspace billing currency, and PayU payment integration metadata for multi-tenant resource governance.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`), **Unique** |
| `PlanTier` | `varchar(20)` | ❌ | Enum string: `Free`, `Pro`, `Enterprise` |
| `Currency` | `varchar(10)` | ❌ | Workspace billing currency (e.g., `inr`, `usd`). Default: `inr` |
| `MaxDocuments` | `int` | ❌ | Limit on active documents (Free: 10, Pro: 100, Enterprise: 10,000) |
| `MaxEmployees` | `int` | ❌ | Limit on team member seats (Free: 5, Pro: 50, Enterprise: 5,000) |
| `MaxStorageBytes` | `bigint` | ❌ | Storage cap in bytes (Free: 50MB, Pro: 5GB, Enterprise: 500GB) |
| `ValidUntil` | `datetime2` | ✅ | Optional expiration timestamp |
| `CreatedAt` | `datetime2` | ❌ | UTC creation timestamp |
| `UpdatedAt` | `datetime2` | ✅ | UTC last update timestamp |
| `PaymentTransactionId` | `varchar(100)` | ✅ | Payment gateway transaction ID |
| `PaymentReferenceId` | `varchar(100)` | ✅ | Payment gateway reference / payment ID |
| `SubscriptionStatus` | `varchar(50)` | ✅ | Subscription status (`active`, `past_due`, `canceled`) |
| `CurrentPeriodEndUtc` | `datetime2` | ✅ | Billing cycle renewal date |
| `CancelAtPeriodEnd` | `boolean` | ❌ | True if cancellation is scheduled at end of current billing period |

**Indexes:** `UX_TenantPlans_TenantId` (Unique)

---

## Module: Documents (`documents` schema)
Handles the document processing lifecycle, chunking, and vector embeddings.

### `Documents`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `FileName` | `nvarchar(255)` | ❌ | |
| `ContentType` | `varchar(100)` | ❌ | |
| `Size` | `bigint` | ❌ | File size in bytes |
| `UploadedAt` | `datetime2` | ❌ | |
| `DeletedAt` | `datetime2` | ✅ | Soft-delete timestamp (Phase 20) |
| `DeletedByUserId` | `uniqueidentifier` | ✅ | Audit trail for the admin who performed the deletion |

**Indexes:** 
- `IX_Documents_TenantId`
- `IX_Documents_TenantId_DeletedAt` (Composite index on `(TenantId, DeletedAt)` for tenant query filters and background purge jobs)

---

### `DocumentVersions`
A single document can have multiple versions. RAG uses the one with `IsActive = true`.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `DocumentId` | `uniqueidentifier` | ❌ | FK → `Documents.Id` (`Cascade`) |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `VersionNumber` | `int` | ❌ | Sequential version number |
| `StorageObjectName` | `varchar(500)` | ❌ | S3/Object storage reference |
| `ExtractedText` | `text` | ✅ | |
| `Status` | `int` | ❌ | Enum: `Uploaded`, `Processing`, `Ready`, `Failed` |
| `IsActive` | `boolean` | ❌ | Only active version is queried by RAG |
| `CreatedAt` | `datetime2` | ❌ | |

**Indexes:** `IX_DocumentVersions_TenantId`

---

### `DocumentChunks`
Extracted text and 768-dimensional Gemini embeddings for RAG retrieval.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `DocumentVersionId` | `uniqueidentifier` | ❌ | FK → `DocumentVersions.Id` (`Cascade`) |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `ChunkIndex` | `int` | ❌ | Ordering within the document |
| `Text` | `nvarchar(max)` | ❌ | Extracted chunk content (~4k chars) |
| `Embedding` | `vector(768)` | ✅ | `pgvector` embedding from Gemini API |

**Indexes:** 
- `IX_DocumentChunks_TenantId` (Tenant-scoped chunk isolation)
- `IX_DocumentChunks_DocumentVersionId`
- `IX_DocumentChunks_Embedding_Hnsw` (HNSW index on `Embedding` with `vector_cosine_ops`, `m = 16`, `ef_construction = 64` for high-throughput sub-millisecond semantic search)

---

## Module: Conversations (`conversations` schema)
Stores multi-turn conversational histories and AI citations.

### `Conversations`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `UserId` | `uniqueidentifier` | ❌ | FK → `users.Users.Id` |
| `Title` | `nvarchar(200)` | ❌ | Auto-generated from first message |
| `CreatedAt` | `datetime2` | ❌ | |
| `UpdatedAt` | `datetime2` | ❌ | |

**Indexes:** `IX_Conversations_TenantId_UserId`

---

### `Messages`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `ConversationId` | `uniqueidentifier` | ❌ | FK → `Conversations.Id` (`Cascade`) |
| `Role` | `varchar(20)` | ❌ | Enum: `User`, `Assistant` |
| `Content` | `nvarchar(max)` | ❌ | The markdown/text content |
| `CreatedAt` | `datetime2` | ❌ | |

---

### `MessageCitations`
Links AI Assistant messages to the `DocumentChunks` used to generate the answer.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `MessageId` | `uniqueidentifier` | ❌ | FK → `Messages.Id` (`Cascade`) |
| `DocumentChunkId` | `uniqueidentifier` | ❌ | FK → `documents.DocumentChunks.Id` |
| `RelevanceScore` | `float` | ❌ | Cosine similarity score |

---

### `SavedAnswers`
Allows users to bookmark/save assistant messages for later retrieval.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `UserId` | `nvarchar(max)` | ❌ | External OIDC User Identifier (`sub`) |
| `MessageId` | `uniqueidentifier` | ❌ | FK → `Messages.Id` (`Cascade`) |
| `ConversationId` | `uniqueidentifier` | ❌ | FK → `Conversations.Id` |

**Indexes:** `UX_SavedAnswers_UserId_MessageId` (Unique)

---

### `MessageFeedbacks`
Stores thumbs-up/thumbs-down ratings from users on assistant messages.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `UserId` | `nvarchar(max)` | ❌ | External OIDC User Identifier (`sub`) |
| `MessageId` | `uniqueidentifier` | ❌ | FK → `Messages.Id` (`Cascade`) |
| `Rating` | `int` | ❌ | Enum: `Positive=1`, `Negative=2` |
| `Comment` | `nvarchar(500)` | ✅ | Optional feedback text |

**Indexes:** `UX_MessageFeedbacks_UserId_MessageId` (Unique)

---

### `ConversationShares`
Enables secure snapshot sharing of assistant conversations via unique cryptographic URL tokens.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `ConversationId` | `uniqueidentifier` | ❌ | FK → `Conversations.Id` (`Cascade`) |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `ShareToken` | `varchar(64)` | ❌ | Base62 unique cryptographic token |
| `CreatedByUserId` | `nvarchar(max)` | ❌ | External OIDC User Identifier (`sub`) of the owner |
| `CreatedAtUtc` | `datetime2` | ❌ | UTC timestamp |
| `ExpiresAtUtc` | `datetime2` | ✅ | Optional expiration timestamp |
| `RevokedAtUtc` | `datetime2` | ✅ | Soft revocation timestamp |

**Indexes:** `UX_ConversationShares_ShareToken` (Unique)
