# Frontend Onboarding Guide

Welcome to the **Normora Angular SPA** (`client/`). This guide is tailored for frontend engineers to quickly understand our architecture, state management patterns, security model, and styling rules.

---

## Tech Stack & Architecture

| Concern | Technology / Approach |
|---|---|
| **Framework** | Angular 22 (100% Standalone Components) |
| **Language** | Strict TypeScript (No `any`, typed DTOs) |
| **Styling** | Tailwind CSS v4 + PrimeNG Aura Theme (Synchronized Dark Mode) |
| **State Management** | Angular Signals (`signal`, `computed`) + RxJS for async streams |
| **Identity & Session** | Duende BFF (`HttpOnly`, `SameSite=Strict` Cookie Session) + Keycloak OIDC |
| **Realtime Streams** | SignalR (Document Processing WebSockets) & Server-Sent Events (Conversational RAG) |
| **Unit Testing** | Vitest (Blazing-fast native ESM test runner) |

---

## 1. Dark Mode & Synchronized Theming

Normora implements a dual-system theme engine that synchronizes PrimeNG UI components and custom Tailwind styling through a single class on the root `<html>` element:

- **PrimeNG Configuration**: Set in `app.config.ts` via `providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.app-dark' } } })`.
- **Tailwind v4 Configuration**: Configured in `styles.css` using:
  ```css
  @custom-variant dark (&:where(.app-dark, .app-dark *));
  ```
- **Toggle Mechanism**: `ThemeService` toggles the `.app-dark` class on `document.documentElement`. Both PrimeNG components and Tailwind `dark:` utility classes (e.g., `dark:bg-slate-900`) respond simultaneously.
- **Tailwind v4 Syntax Rule**: The `!important` modifier is a **suffix**, not a prefix. Always write `class!` (e.g. `border-none!`, `shadow-none!`), and never legacy `!border-none`.

---

## 2. Directory Architecture

The frontend follows a domain-driven, vertical slice structure:

```
client/src/app/
├── core/                         # Singletons, security, and cross-cutting infrastructure
│   ├── constants/                # Centralized string constants (ApiMessages, RoutePaths)
│   ├── guards/                   # Route guards (AuthGuard, WorkspaceGuard, RoleGuard)
│   ├── interceptors/             # HTTP interceptors (ApiInterceptor, TenantContextInterceptor)
│   ├── models/                   # Strict domain contracts and DTO interfaces
│   └── services/                 # Global services (AuthService, TenantService, ThemeService)
├── features/                     # Domain feature modules (lazy-loaded routes)
│   ├── auth/                     # Login redirection and session state views
│   ├── employer/                 # Employer administration portal
│   │   ├── dashboard/            # Telemetry, storage metrics, active user stats
│   │   ├── documents/            # Ingestion table, upload modal, version history
│   │   ├── employees/            # Team roster, invitation drawer, pagination
│   │   └── settings/             # Branding, departments, user groups, audit logs, billing
│   └── employee/                 # Employee knowledge workspace
│       ├── ask/                  # Multi-turn Conversational RAG, citations drawer, suggestions
│       ├── profile/              # User profile and linked workspaces
│       └── saved/                # Bookmarked answers with multi-format export (PDF/DOCX/MD)
├── layout/                       # Application shell components (Sidebar, TopNav, Breadcrumbs)
└── shared/                       # Reusable UI widgets, empty-states, pipes, and directives
```

---

## 3. Path-Based Workspace Multi-Tenancy

Normora visually isolates workspaces through URL paths:
```
/app/workspaces/:slug/{employer|employee}/*
```

### Lifecycle & Interceptors
1. **Login & Redirection**: Upon authentication via Duende BFF, `AuthService` queries user memberships. If the user belongs to workspace `acme`, they navigate to `/app/workspaces/acme/employer/dashboard`.
2. **`WorkspaceGuard`**: Extracts `:slug` from the active route, resolves tenant metadata, and binds it to `TenantService`.
3. **`TenantContextInterceptor`**: Intercepts outbound HTTP requests matching `/api/` or `environment.apiUrl` and automatically attaches:
   ```http
   X-Tenant-Id: <active-tenant-guid>
   ```
4. **Dynamic Branding**: `TenantBrandingService` fetches tenant branding colors and applies CSS custom variables (`--brand-primary`, `--brand-secondary`) to `document.documentElement`, instantly reskinning the workspace without reloading.

---

## 4. HTTP Interceptor Pipeline & Error Normalization

### `ApiInterceptor`
Centralizes HTTP error handling to eliminate redundant try/catch blocks across UI components:
- **`401 Unauthorized`**: Automatically redirects browser to the BFF login endpoint (`/bff/login?returnUrl=...`).
- **`402 Payment Required`**: Emits event to open the subscription upgrade modal (quota exceeded).
- **`403 Forbidden (BFLA)`**: Displays an access-denied toast notification (`ApiMessages.Forbidden`).
- **`404 Not Found (BOLA)`**: For non-GET mutations, displays a generic not-found toast (`ApiMessages.NotFound`), preventing resource enumeration.
- **`429 Too Many Requests`**: Triggers exponential backoff retry notification.
- **`5xx Server Error`**: Displays a generic system error toast (`ApiMessages.InternalServerError`).

> [!IMPORTANT]
> **No Magic Strings**: Never hardcode user-facing error messages in components or services. Always import and use `ApiMessages` from `client/src/app/core/constants/api-messages.ts`.

---

## 5. State Management: Angular Signals & Modern DI

We use **Angular Signals** for synchronous, performant component state and **`inject()`** for dependency injection.

### Standard Component Pattern
```typescript
import { Component, inject, signal, computed } from '@angular/core';
import { DepartmentService } from '@core/services/department.service';
import { Department } from '@core/models/department.model';

@Component({
  selector: 'app-department-list',
  standalone: true,
  templateUrl: './department-list.component.html'
})
export class DepartmentListComponent {
  private readonly deptService = inject(DepartmentService);

  // State Signals
  readonly departments = signal<Department[]>([]);
  readonly isLoading = signal(false);
  readonly searchQuery = signal('');

  // Computed Signal
  readonly filteredDepartments = computed(() => {
    const query = this.searchQuery().toLowerCase();
    return this.departments().filter(d => d.name.toLowerCase().includes(query));
  });

  loadDepartments(): void {
    this.isLoading.set(true);
    this.deptService.getDepartments().subscribe({
      next: (res) => this.departments.set(res.data),
      error: () => this.isLoading.set(false),
      complete: () => this.isLoading.set(false)
    });
  }
}
```

---

## 6. Real-Time Communication

### SignalR WebSockets (Document Processing)
- `DocumentRealtimeService` establishes a connection to `/hubs/documents` using credentials stored in secure BFF cookies (`withCredentials: true`).
- The backend joins the connection to a tenant-isolated SignalR group.
- Real-time status events (`Uploaded` ➔ `Processing` ➔ `Ready` / `Failed`) stream to components, updating UI progress bars instantly without polling.

### Server-Sent Events (SSE) (Conversational RAG)
- `AskService` consumes streaming responses from `POST /api/conversations/{id}/messages/stream`.
- Chunks of generated text are parsed and accumulated in real-time, streaming token-by-token directly into the chat interface alongside clickable citation references.

---

## 7. Adding a New Frontend Feature

Follow this checklist when adding a new feature:
1. **Define DTOs**: Create strict TypeScript interfaces in `core/models/` matching the backend MediatR response envelopes.
2. **Implement Service**: Create an Angular service using `inject(HttpClient)` with typed methods returning `Observable<ApiResponse<T>>`.
3. **Build Standalone Component**: Create components with `standalone: true`, using Angular Signals for local state and PrimeNG / Tailwind v4 for layout.
4. **Register Route**: Add the path to `employer.routes.ts` or `employee.routes.ts` with `canActivate: [WorkspaceGuard, RoleGuard]`.
5. **Enforce Dark Mode**: Use `dark:` variants on all backgrounds, borders, and text colors.
