# API Endpoints Catalog

Base URL: `http://localhost:5000/api`

All URLs are **lowercase**. All responses use **camelCase** JSON and the standard envelope pattern.

> **Auth Legend**
> - `Anonymous` — No token required.
> - `Authenticated` — Any valid authenticated session (via __Host-spa cookie).
> - `RequireTenant(Employer)` — Requires a valid `X-Tenant-Id` header and the user must be an `Employer` in that tenant.
> - `RequireTenant(Employee)` — Requires a valid `X-Tenant-Id` header and the user must be an `Employee` (or Employer) in that tenant.

---

## 🏢 Tenants

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/tenants/my` | Authenticated | List all tenants the current user is a member of. |
| `POST` | `/tenants` | Authenticated | Create a new tenant (workspace). User becomes the owner (Employer). |
| `GET` | `/tenants/branding/{slug}` | Anonymous | Get white-label branding configurations by tenant slug. |
| `PUT` | `/tenants/{id}/branding` | RequireTenant(Employer) | Update white-label branding (colors, logos) for the tenant. |

---

## 📩 Invitations & Membership

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/invitations` | RequireTenant(Employer) | Invite a user to the current tenant by email. |
| `GET` | `/invitations/my` | Authenticated | List all pending invitations for the current user. |
| `POST` | `/invitations/{id}/accept` | Authenticated | Accept an invitation to join a tenant. |
| `GET` | `/tenants/employees` | RequireTenant(Admin, Employee) | List employees with pagination, search, and department/group filters. |
| `DELETE` | `/tenants/employees/{membershipId}` | RequireTenant(Admin) | Remove an employee's membership from the current workspace (Soft delete). |

---

## 📁 Departments

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/departments` | RequireTenant(Employer) | List all departments in the current tenant. |
| `POST` | `/departments` | RequireTenant(Employer) | Create a new department. |
| `DELETE` | `/departments/{id}` | RequireTenant(Employer) | Delete a department. |

---

## 👥 User Groups

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/usergroups` | RequireTenant(Employer) | List all user groups in the current tenant. |
| `POST` | `/usergroups` | RequireTenant(Employer) | Create a new user group and optionally assign departments. |
| `PUT` | `/usergroups/{id}` | RequireTenant(Employer) | Update a user group (name, department assignments). |
| `POST` | `/usergroups/{id}/members` | RequireTenant(Employer) | Add users to a user group. |
| `DELETE` | `/usergroups/{id}` | RequireTenant(Employer) | Delete a user group. |

---

## 📄 Documents

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/documents` | RequireTenant(Employer) | List all uploaded documents and their processing status (`Uploaded`, `Processing`, `Ready`, `Failed`). |
| `POST` | `/documents/upload` | RequireTenant(Employer) | Upload a document (PDF, TXT) and assign it to departments or Company Wide. Quota-checked against subscription plan limits. |
| `DELETE` | `/documents/{id}` | RequireTenant(Employer) | Soft-delete a document and broadcast a real-time event. Physical deletion is handled by a Hangfire job after 30 days. |
| `GET` | `/documents/search` | RequireTenant(Employee) | Perform a hybrid vector/keyword search across tenant documents. Filtered by the user's assigned departments. |
| `GET` | `/documents/chunks/{chunkId}` | RequireTenant(Employee) | Retrieve full text and metadata of a specific chunk for citation preview. Enforces department authorization. |

---

## 💬 Conversations (RAG)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/conversations` | RequireTenant(Employee) | List the user's past conversations within the tenant. |
| `GET` | `/conversations/{id}` | RequireTenant(Employee) | Get full message history for a specific conversation. |
| `POST` | `/conversations` | RequireTenant(Employee) | Start a new conversation with an initial prompt. Returns grounded answer and citations. |
| `POST` | `/conversations/{id}/messages` | RequireTenant(Employee) | Send a follow-up message in an existing conversation (Multi-turn RAG). |
| `POST` | `/conversations/{id}/messages/stream` | RequireTenant(Employee) | Stream a grounded AI response using Server-Sent Events (SSE). |
| `POST` | `/conversations/{id}/messages/{messageId}/feedback` | RequireTenant(Employee) | Submit a Positive (1) or Negative (2) rating for an AI message. |
| `DELETE` | `/conversations/{id}` | RequireTenant(Employee) | Delete a conversation history. |
| `POST` | `/conversations/{id}/share` | RequireTenant(Employee) | Create or retrieve a shareable read-only link token for a conversation. |
| `GET` | `/conversations/shared/{token}` | Authenticated | Fetch a read-only snapshot of a shared conversation by token. |

---

## 📊 Dashboard

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/dashboard/summary` | RequireTenant(Employer) | Fetch aggregated workspace dashboard metrics (total documents, members, storage utilization, and conversation volume). MediatR aggregator querying decoupled modules. |

---

## 🤖 Model Context Protocol (MCP)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/mcp` | RequireTenant(Employee) | Discovery manifest returning MCP server capabilities, protocol version, and tool inventory. |
| `POST` | `/mcp` | RequireTenant(Employee) | JSON-RPC 2.0 endpoint handling `initialize`, `tools/list`, `tools/call`, `resources/list`, and `resources/read`. |

---

## 💡 Suggestions

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/suggestions` | RequireTenant(Employee) | Generate dynamic AI suggested prompt questions based on accessible department documents. |

---

## 🛡️ Audit Logs

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/audit-logs` | RequireTenant(Employer) | List paginated audit trail events with optional filters (`action`, `userId`, `fromDate`, `toDate`, `search`). |

---

## 💳 Subscription & Plans

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/subscription/usage` | RequireTenant | Fetch tenant resource usage (documents, member seats, storage bytes, subscription status, currency) compared against current plan limits. |
| `POST` | `/subscription/checkout` | RequireTenant(Admin) | Generate PayU hosted checkout action URL and parameters signed with a SHA-512 cryptographic hash for upgrading to a paid tier (`Pro`, `Enterprise`) in specified currency. |
| `POST` | `/subscription/portal` | RequireTenant(Admin) | Direct user to workspace subscription and billing management overview. |
| `POST` | `/subscription/payu-callback` | AllowAnonymous | Securely handle PayU hosted checkout return (`surl`/`furl`) with reverse SHA-512 hash verification, quota provisioning, and browser redirection. |
| `POST` | `/subscription/tier` | RequireTenant(Admin) | Upgrade or downgrade tenant subscription tier directly (free tier fallback). |

---

## 🔖 Saved Answers

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/saved-answers` | RequireTenant(Employee) | List the user's bookmarked answers, ordered newest-first. Supports `limit` and `offset` query params for pagination. |
| `POST` | `/saved-answers` | RequireTenant(Employee) | Bookmark an assistant message by its `messageId`. Idempotent — safe to call multiple times. |
| `DELETE` | `/saved-answers/{messageId}` | RequireTenant(Employee) | Remove a bookmark by the original message ID. Idempotent. |
| `GET` | `/saved-answers/{id}/export` | RequireTenant(Employee) | Export a saved answer as a downloadable file. See **Export Formats** below. |

### Export Formats

The `format` query parameter controls the output file type:

| `format` value | MIME Type | File | Description |
|---|---|---|---|
| `Markdown` (default) | `text/markdown` | `.md` | Plain text with Markdown formatting — lightweight, version-control friendly. |
| `Pdf` | `application/pdf` | `.pdf` | Print-ready PDF with branded header, question/answer blocks, and a citations table (generated by QuestPDF). |
| `Docx` | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` | `.docx` | Editable Word document using Open XML SDK — headings, quote block, citations table, footer. |

Example request:
```
GET /api/saved-answers/{id}/export?format=Pdf
```

The server streams the file as `Content-Disposition: attachment` with a date-stamped filename (e.g. `normora-answer-2026-09-29.pdf`). No polling or background job required — exports are generated synchronously in-process.

---

## 👤 Users

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/users/me` | Authenticated | Get the current authenticated user's profile information. |

---

## 📘 OpenAPI Documentation (Scalar UI)

Normora automatically generates full OpenAPI (Swagger) specifications for all endpoints. 
You can view the beautifully rendered interactive API documentation by navigating to:
`http://localhost:5000/scalar` (or `/scalar` in production).

The documentation includes:
- Request/Response JSON schemas mapped strictly to the `ApiResponse<T>` wrapper.
- Explicitly documented HTTP status codes (200, 400, 401, 403, 404, 500).
- Developer-friendly descriptions and XML comments.

---

## API Response Envelope

Every endpoint returns a standardized JSON shape:

```json
{
  "success": true,
  "message": "Operation completed successfully.",
  "data": { ... },
  "errors": {}
}
```

| Field | Type | Description |
|---|---|---|
| `success` | `bool` | `true` for 2xx responses, `false` for 4xx/5xx errors. |
| `message` | `string` | A human-readable summary message. |
| `data` | `T \| null` | The response payload (null if an error occurred). |
| `errors` | `Dictionary` | Validation errors keyed by the property name. |
