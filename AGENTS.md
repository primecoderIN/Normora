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
