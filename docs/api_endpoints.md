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
| `GET` | `/tenants/members` | RequireTenant(Employer) | List all active members in the current tenant. |
| `DELETE` | `/tenants/members/{userId}` | RequireTenant(Employer) | Remove a user from the current tenant. |

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
| `POST` | `/documents/upload` | RequireTenant(Employer) | Upload a document (PDF, TXT) and assign it to departments or Company Wide. |
| `DELETE` | `/documents/{id}` | RequireTenant(Employer) | Delete a document and its associated vector chunks. |
| `GET` | `/documents/search` | RequireTenant(Employee) | Perform a hybrid vector/keyword search across tenant documents. Filtered by the user's assigned departments. |

---

## 💬 Conversations (RAG)

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/conversations` | RequireTenant(Employee) | List the user's past conversations within the tenant. |
| `GET` | `/conversations/{id}` | RequireTenant(Employee) | Get full message history for a specific conversation. |
| `POST` | `/conversations` | RequireTenant(Employee) | Start a new conversation with an initial prompt. Returns grounded answer and citations. |
| `POST` | `/conversations/{id}/messages` | RequireTenant(Employee) | Send a follow-up message in an existing conversation (Multi-turn RAG). |
| `POST` | `/conversations/{id}/messages/{messageId}/feedback` | RequireTenant(Employee) | Submit a Positive (1) or Negative (2) rating for an AI message. |
| `DELETE` | `/conversations/{id}` | RequireTenant(Employee) | Delete a conversation history. |

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
