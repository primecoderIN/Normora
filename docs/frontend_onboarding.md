# Frontend Onboarding Guide

This guide is for developers working on the **Normora Angular SPA** (`client/`). It covers the tech stack, core architectural patterns, and how multi-tenant routing is handled on the frontend.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | Angular 18 |
| Styling | Tailwind CSS (v3) + CSS Custom Properties |
| HTTP Client | `HttpClient` |
| State Management | Signals + RxJS (for async streams) |
| Identity & Auth | Keycloak + Duende.BFF |
| Component Library | Material Design (Angular Material) |

---

## 1. Application Structure

The application follows a strictly modular structure, separating core logic from features.

```
client/src/app/
├── core/             # Singleton services (Auth, Tenant Context, API interceptors)
├── shared/           # Reusable UI components, pipes, and directives
├── features/         # Domain-specific modules
│   ├── auth/         # Login callbacks, Keycloak integration
│   ├── employer/     # Employer portal (Document management, Users, Departments)
│   └── employee/     # Employee portal (Ask Normora / Conversational RAG)
└── layout/           # Main application shell, Sidebar, Header
```

---

## 2. Multi-Tenant Routing & Subdomains

Normora uses **Subdomain-based Routing** to visually isolate tenant workspaces.

When a user logs in, the `AuthService` determines their memberships:
- If they belong to `Acme Corp`, they are redirected to `https://acme.localhost:4200`.
- The `TenantContextInterceptor` intercepts all outbound API requests to `/api/*` and automatically appends the `X-Tenant-Id` header based on the current active subdomain.
- The `TenantBrandingService` automatically fetches the branding configuration for `acme` and applies CSS Custom Properties (`--brand-primary`, etc.) to the `document.documentElement` to reskin the application.

### Route Guards
- `AuthGuard`: Ensures the user is authenticated via Keycloak.
- `EmployerGuard`: Ensures the user holds the `Employer` role in the current tenant context.
- `EmployeeGuard`: Ensures the user holds the `Employee` (or Employer) role.

---

## 3. State Management (Signals & RxJS)

We use **Angular Signals** for synchronous UI state and **RxJS** for complex asynchronous data flows (like WebSocket connections and HTTP requests).

### Signal Example (Local State)
```typescript
@Component({...})
export class DepartmentListComponent {
  departments = signal<Department[]>([]);
  isLoading = signal(false);

  constructor(private api: DepartmentService) {}

  load() {
    this.isLoading.set(true);
    this.api.getDepartments().subscribe({
      next: (res) => this.departments.set(res.data),
      complete: () => this.isLoading.set(false)
    });
  }
}
```

---

## 4. Realtime Events (SignalR)

Document processing states (`Uploaded`, `Processing`, `Ready`, `Failed`) are broadcasted in real-time to employers via SignalR.

- The `DocumentHubService` in `core/services` manages the WebSocket connection.
- It authenticates automatically using the secure `__Host-spa` cookie managed by the BFF.
- Components subscribe to `documentStatusChanged$` streams to update their local signals without polling the server.

---

## 5. Adding a New Feature

1. **Create the Models**: Add your TypeScript interfaces in `shared/models/`.
2. **Create the Service**: Add a service in `core/services/` that extends the base API logic.
3. **Build the Components**: Generate components inside the appropriate `features/` directory (e.g., `ng g c features/employer/my-new-feature`).
4. **Update Routing**: Register the component in `employer.routes.ts` or `employee.routes.ts`.
5. **Enforce Security**: Apply the correct Route Guard (`canActivate: [EmployerGuard]`).
