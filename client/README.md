# 🎨 Normora Client (Angular SPA)

<div align="center">
  <img src="https://img.shields.io/badge/Angular-22.1-DD0031?logo=angular&logoColor=white" alt="Angular 22" />
  <img src="https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/Tailwind_CSS-v4.0-38B2AC?logo=tailwind-css&logoColor=white" alt="Tailwind CSS v4" />
  <img src="https://img.shields.io/badge/PrimeNG-Aura_Theme-007ACC?logo=primeng&logoColor=white" alt="PrimeNG" />
  <img src="https://img.shields.io/badge/Vitest-Unit_Tests-6E9F18?logo=vitest&logoColor=white" alt="Vitest" />
  <img src="https://img.shields.io/badge/Duende_BFF-Cookie_Auth-000000?logo=csharp&logoColor=white" alt="Duende BFF" />
</div>
<br>

Welcome to the frontend application for **Normora**. The client is an enterprise-grade Single Page Application (SPA) architected with **Angular 22**, strictly typed in TypeScript, styled using **Tailwind CSS v4** and **PrimeNG**, and secured via the **Backend-For-Frontend (BFF)** pattern.

---

## 🌟 Architectural Highlights

1. **Angular Standalone Components & Signals**:
   - 100% standalone components—no legacy `NgModule` declarations.
   - Synchronous, predictable reactive state representation powered by **Angular Signals** (`signal`, `computed`, `effect`), eliminating unnecessary RxJS subscription management and memory leaks.
2. **Backend-For-Frontend (BFF) Security**:
   - Zero access or refresh tokens are ever stored in `localStorage`, `sessionStorage`, or JavaScript memory.
   - All session state is anchored by encrypted, `HttpOnly`, `SameSite=Strict`, `Secure` cookies managed through Duende BFF.
   - Automatic anti-CSRF header propagation (`X-CSRF: 1`) on state-mutating requests.
3. **Path-Based Workspace Multi-Tenancy**:
   - Visual workspace isolation via route structure: `/app/workspaces/:slug/{employer|employee}/*`.
   - Dynamic workspace branding (custom hex colors, logos, favicons) loaded per tenant and bound to CSS custom properties at runtime.
4. **Tailwind CSS v4 & PrimeNG Aura Hybrid Theming**:
   - Dark mode synchronized across both PrimeNG components (`darkModeSelector: '.app-dark'`) and Tailwind utility classes (`@custom-variant dark (&:where(.app-dark, .app-dark *));`).
   - Tailwind v4 important syntax compliance (`class!` instead of legacy `!class`).
5. **Centralized Interceptor Pipeline**:
   - `TenantContextInterceptor`: Attaches `X-Tenant-Id` headers to scoped API requests while bypassing external third-party endpoints.
   - `ApiInterceptor`: Translates HTTP 401 (BFF login redirect), 402 (plan upgrade modal), 403 (BFLA access denied toast), 404 (BOLA generic not-found toast), and 429 (exponential backoff) into structured UI feedback without magic error strings.
6. **Real-Time Streaming & WebSockets**:
   - Server-Sent Events (SSE) streaming consumer for Conversational RAG with real-time text buffering and markdown parsing.
   - SignalR WebSocket client for document ingestion lifecycle tracking (`Uploaded` ➔ `Processing` ➔ `Ready` / `Failed`).

---

## 📂 Project Structure

```
client/src/app/
├── core/                         # Singleton services, guards, interceptors, and models
│   ├── constants/                # Centralized string constants (ApiMessages, RoutePaths)
│   ├── guards/                   # Angular route guards (WorkspaceGuard, RoleGuard, AuthGuard)
│   ├── interceptors/             # HTTP pipeline interceptors (ApiInterceptor, TenantInterceptor)
│   ├── models/                   # Strict TypeScript domain interfaces and DTOs
│   └── services/                 # Cross-cutting core services (AuthService, TenantService, ThemeService)
├── features/                     # Domain feature slices (lazy-loaded routes)
│   ├── auth/                     # BFF authentication redirection and login status views
│   ├── employee/                 # Employee knowledge portal
│   │   ├── ask/                  # Multi-turn Conversational RAG, citations drawer, suggestions
│   │   ├── profile/              # User profile, account preferences, joined workspaces
│   │   └── saved/                # Bookmarked answers with multi-format export (PDF, Word, Markdown)
│   └── employer/                 # Employer management portal
│       ├── dashboard/            # Workspace usage telemetry, storage metrics, active users
│       ├── documents/            # Document uploads, version history, processing status
│       ├── employees/            # Team roster, invitations, role assignments, department mappings
│       └── settings/             # White-label branding, departments, user groups, audit logs, billing
├── layout/                       # Application shell components (Sidebar, TopNav, WorkspaceSwitcher)
└── shared/                       # Reusable UI widgets, pipes, directives, and empty-state components
```

---

## 🚀 Getting Started

### Prerequisites
- **Node.js**: v20.x or v22.x LTS
- **npm**: v10.x+
- **Backend API**: Running at `http://localhost:5000` (via Docker Compose or local .NET host)

### Installation
From the repository root or the `client/` directory:
```bash
npm install --prefix client
```

### Local Development Server
To launch the Angular development server with live reload:
```bash
npm start --prefix client
```
Navigate your browser to `http://localhost:4200/`.

### Production Build
To compile the production-optimized build into `client/dist/client`:
```bash
npm run build --prefix client
```

### Running Unit Tests
Tests are executed using **Vitest** for ultra-fast, native ESM execution:
```bash
npm test --prefix client
```

---

## 🛡️ Coding Guidelines & Quality Standards

When contributing to the client codebase, adhere strictly to the following standards:
- **No Magic Strings**: All error toasts, confirmation prompts, and user-facing notifications must use `ApiMessages` from `client/src/app/core/constants/api-messages.ts`.
- **Strict Typing**: Never use `any` or `unknown` without explicit type narrowing. Define typed DTOs matching backend MediatR contracts.
- **Mobile-First Responsiveness**: Ensure all views are responsive across mobile, tablet, and desktop (`sm:`, `md:`, `lg:` modifiers).
- **Tailwind v4 Important Modifier**: Always write `class!` (e.g., `border-none!`, `shadow-none!`), never `!class`.
- **Accessible & Semantic HTML**: Use semantic tags (`<header>`, `<main>`, `<article>`, `<section>`, `<nav>`) and include descriptive `alt` tags on all visual assets.
