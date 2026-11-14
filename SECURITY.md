# Security Policy & Vulnerability Disclosure

Normora takes the security of its enterprise architecture, multi-tenant boundaries, and data integrity seriously.

---

## 1. Reporting a Vulnerability

If you believe you have found a security vulnerability in Normora (including BOLA, BFLA, authentication bypass, data leak, or dependency vulnerabilities), please **do NOT report it via public GitHub issues**.

Instead, please submit a responsible disclosure report directly to the project author:

* **Contact:** Sanjeev Kumar
* **Email:** `sanjeev2856@gmail.com`
* **Subject Line:** `[SECURITY] Normora Vulnerability Disclosure - <Brief Description>`

### Please Include:
1. Clear description of the vulnerability.
2. Steps to reproduce or proof-of-concept (PoC) code.
3. Affected components (e.g., API endpoint, controller, tenant boundary, background job).
4. Potential security impact.

We will acknowledge receipt within 48 hours and work on an immediate patch.

---

## 2. Responsible Research & Prohibited Activities

While security evaluations and responsible disclosures are welcome, you are **strictly prohibited** from:
- Testing against production systems or unauthorized hosted instances of Normora.
- Executing Denial of Service (DoS/DDoS) attacks.
- Accessing or modifying data belonging to another tenant or user.
- Using security scanning tools to scrape, duplicate, or clone the repository for commercial exploitation.

---

## 3. Supported Versions

| Version / Branch | Supported |
|---|---|
| `main` | ✅ Yes |
| Historical Commits / Tags | ❌ No |

