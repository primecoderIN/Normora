# Web Security Headers in Normora

This document outlines the essential HTTP security headers and server configurations implemented within the Normora frontend architecture to mitigate common web vulnerabilities as identified by OWASP Dynamic Application Security Testing (DAST) scans.

These headers are configured at the edge (via Nginx in `client/nginx.conf`) and the SSR application layer (via Express in `client/src/server.ts`).

---

## 1. Anti-clickjacking (X-Frame-Options)
**Header:** `X-Frame-Options: SAMEORIGIN`

### What it is
Clickjacking (UI redressing) is a malicious technique where an attacker tricks a user into clicking on something different from what the user perceives. They typically achieve this by embedding the target application (Normora) inside an invisible `<iframe>` layered over a malicious site.

### How this header mitigates it
The `X-Frame-Options: SAMEORIGIN` header strictly instructs the browser that the Normora application is *only* allowed to be rendered in a frame or iframe if the parent page is exactly the same origin (domain). If a malicious external website attempts to embed Normora in an iframe, modern browsers will block the rendering, entirely mitigating clickjacking attacks.

---

## 2. MIME-Type Sniffing (X-Content-Type-Options)
**Header:** `X-Content-Type-Options: nosniff`

### What it is
Historically, browsers would attempt to "guess" (or "sniff") the MIME type of a file based on its content, ignoring the `Content-Type` header sent by the server. An attacker could exploit this by uploading a malicious executable JavaScript file disguised as an innocent image (`.jpg`). The browser would sniff it, realize it contains script tags, and execute it, leading to Cross-Site Scripting (XSS).

### How this header mitigates it
By explicitly passing `nosniff`, we force the browser to strictly honor the `Content-Type` header provided by Nginx. If the server says a file is an image, the browser will refuse to execute it as a script under any circumstances.

---

## 3. Content Security Policy (CSP)
**Header:** `Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https: blob:; connect-src 'self' ws: wss:; font-src 'self' data:;`

### What it is
The CSP is the ultimate defense-in-depth security layer against XSS and data injection attacks. Without a CSP, a browser trusts any script or asset that the HTML page requests, regardless of where it came from. 

### How this header mitigates it
Our CSP establishes an explicit "allowlist" of trusted origins. 
- `default-src 'self'`: By default, assets are only allowed to load from our own domain. 
- `connect-src 'self' ws: wss:`: Restricts API calls (fetch/XHR) and WebSocket connections (SignalR) to trusted endpoints.
- If an attacker somehow manages to inject a malicious `<script src="http://evil.com/malware.js"></script>` into a Normora page, the browser will actively block the script from loading because `evil.com` is not explicitly permitted in our CSP.

---

## 4. Information Leaks (X-Powered-By & Server Tokens)
**Configurations:** 
- Nginx: `server_tokens off;`
- Express: `app.disable('x-powered-by');`

### What it is
By default, web servers and frameworks are highly verbose. Nginx will append a `Server: nginx/1.25.3` header to every response, and Express will append `X-Powered-By: Express`. 

### How disabling them mitigates risk
While exposing version numbers is not a direct vulnerability, it provides free reconnaissance to automated scanners and attackers. If an attacker knows you are running a specific version of Express or Nginx, they can instantly look up a database of known CVEs (Common Vulnerabilities and Exposures) for that exact version and launch a targeted exploit. By disabling these headers, we force attackers to operate blindly, significantly raising the difficulty of an attack.

---

## 5. Backend Server Header Hardening (Kestrel & ASP.NET Core)
**Configurations:**
- Kestrel: `options.AddServerHeader = false;` (in `server/Normora.Api/Program.cs`)
- Pipeline: `app.Use(...)` appending `X-Content-Type-Options: nosniff` to all HTTP API responses.

### What it is
By default, ASP.NET Core Kestrel broadcasts `Server: Kestrel` on all HTTP responses, aiding automated fingerprinting. Furthermore, missing `X-Content-Type-Options` on JSON API endpoints can occasionally allow legacy clients or proxies to misinterpret error payloads.

### How this mitigates risk
- Suppressing `Server: Kestrel` strips banner identification from API calls.
- Enforcing `nosniff` prevents MIME-type sniffing on API responses across all environments.

---

## 6. API Rate Limiting & Resource Exhaustion Defense (OWASP API4)
**Policies Configured (`server/Normora.Api/Extensions/ApiServiceExtensions.cs`):**
- **AI Policy (`"ai"`):** 20 requests/minute per authenticated user partition (`sub` claim). Protects `/api/conversations`, `/api/documents/search`, `/api/documents/upload`, `/api/suggestions`, and `/api/mcp`.
- **Anonymous Policy (`"anon"`):** 5 requests/minute per client IP address. Protects public endpoints including `/api/invitations/{token}` and `/api/subscription/callback` / `/payu-callback`.

### What it is
Without rate limiting, malicious callers or misconfigured client-side loops can trigger high-cost LLM completions, run CPU-intensive SHA-512 cryptographic calculations, or flood vector databases.

### How this mitigates risk
- Guarantees deterministic resource consumption per user and IP bucket.
- Exceeding quotas returns `HTTP 429 Too Many Requests` which is normalized by the frontend interceptor into a non-destructive user notification toast.

---

## 7. Production Exception Masking & Information Leakage Prevention
**Handler:** `GlobalExceptionHandler` (`server/Normora.Api/Middleware/GlobalExceptionHandler.cs`)

### What it is
Unhandled runtime exceptions can leak internal database schemas, SQL fragments, server file paths, and framework library versions.

### How this mitigates risk
- In production (`env.IsProduction()`), internal exceptions are strictly replaced with generic sanitized messages (`ApiMessages.BadRequest` or `ApiMessages.InternalServerError`).
- Deliberate business rule rejections declared in `Normora.Shared.Constants.ApiMessages` are cached dynamically via reflection (`SafeDomainMessages`) and safely returned to the client, providing clear UI feedback without exposing internal system internals.
