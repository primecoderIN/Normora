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
