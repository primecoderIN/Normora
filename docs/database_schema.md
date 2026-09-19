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
| `Slug` | `varchar(180)` | ❌ | **Unique** (`UX_Tenants_Slug`). Used for subdomain routing. |
| `Status` | `int` | ❌ | Enum: `Active`, `Suspended` |
| `CreatedAt` | `datetime2` | ❌ | UTC timestamp |

**Indexes:** `UX_Tenants_Slug` (Unique)

---

### `TenantMemberships`
The junction table linking physical `Users` to `Tenants` with a context-specific role.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `Tenants.Id` (`Cascade`) |
| `UserId` | `uniqueidentifier` | ❌ | FK → `users.Users.Id` (By convention) |
| `Role` | `int` | ❌ | Enum: `Employer`, `Employee` |
| `CreatedAt` | `datetime2` | ❌ | |

**Indexes:** 
| Name | Columns | Unique |
|---|---|---|
| `UX_TenantMemberships_TenantId_UserId` | `(TenantId, UserId)` | ✅ |
| `IX_TenantMemberships_UserId` | `UserId` | ❌ |

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
| `Status` | `int` | ❌ | Enum: `Uploaded`, `Processing`, `Ready`, `Failed` |
| `MinioObjectKey` | `varchar(500)` | ❌ | S3 Object reference |
| `CreatedAt` | `datetime2` | ❌ | |

**Indexes:** `IX_Documents_TenantId_Status`

---

### `DocumentChunks`
Extracted text and 768-dimensional Gemini embeddings for RAG retrieval.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | ❌ | PK |
| `DocumentId` | `uniqueidentifier` | ❌ | FK → `Documents.Id` (`Cascade`) |
| `TenantId` | `uniqueidentifier` | ❌ | FK → `tenants.Tenants.Id` |
| `ChunkIndex` | `int` | ❌ | Ordering within the document |
| `Text` | `nvarchar(max)` | ❌ | Extracted chunk content (~4k chars) |
| `Embedding` | `vector(768)` | ✅ | `pgvector` embedding from Gemini API |

**Indexes:** 
- `IX_DocumentChunks_DocumentId`
- HNSW/IVFFlat index on `Embedding` for cosine distance search (if vector extension active).

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
