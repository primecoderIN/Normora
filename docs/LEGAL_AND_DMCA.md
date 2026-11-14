# ⚖️ Legal Protection, Copyright & DMCA Takedown Guide

This document outlines the intellectual property rights, legal protections, and enforcement procedures for the **Normora** codebase.

---

## 1. Ownership & Copyright Status

* **Copyright Holder:** Sanjeev Kumar
* **Project Name:** Normora (Workplace Intelligence & Knowledge Management Platform)
* **Primary Repository:** `https://github.com/primecoderIN/Normora`
* **Contact:** `sanjeev2856@gmail.com`
* **Governing License:** [PROPRIETARY & SOURCE-AVAILABLE LICENSE](../LICENSE)

### Public Repository ≠ Open Source
Hosting this repository publicly on GitHub allows users to view, inspect, and evaluate the architecture under GitHub's Standard Terms of Service. **It does not grant any right to:**
1. Copy, redistribute, mirror, or publish duplicates of the codebase.
2. Rebrand or white-label the application.
3. Deploy or run the software for commercial, enterprise, or revenue-generating purposes.
4. Offer the software (or derivatives) as a hosted cloud service (SaaS/PaaS).

---

## 2. When Can You File a DMCA Takedown Notice?

You can legally issue an official **DMCA Takedown Notice** through GitHub if you discover someone who has:
1. Created a public fork and used it to launch a public duplicate, competitor, or mirror.
2. Cloned your repository and re-uploaded it under their own GitHub account or another platform (GitLab, Bitbucket, etc.) without explicit written permission.
3. Removed your copyright notices, branding, or author attributions.
4. Deployed or hosted your code as a commercial product or service.

---

## 3. Ready-to-Use GitHub DMCA Takedown Notice Template

If you encounter an unauthorized copy on GitHub, navigate to [GitHub DMCA Takedown Submission](https://support.github.com/contact/dmca-takedown) or email `copyright@github.com` with the following filled template:

```text
Subject: DMCA Takedown Notice - Unauthorized Copy of Normora Repository

Dear GitHub Copyright Agent,

I am writing to notify GitHub under the Digital Millennium Copyright Act (17 U.S.C. § 512(c)) of copyright infringement occurring on GitHub.

1. IDENTIFICATION OF THE COPYRIGHTED WORK:
I am the author and sole copyright owner of the proprietary software project known as "Normora", an enterprise workplace intelligence and multi-tenant knowledge management platform.
Original Repository URL: https://github.com/primecoderIN/Normora
Governing License: Proprietary & Source-Available (All Rights Reserved) - https://github.com/primecoderIN/Normora/blob/main/LICENSE

2. IDENTIFICATION OF THE INFRINGING MATERIAL:
The following repository/file(s) contain an unauthorized reproduction and distribution of my proprietary source code without my consent:
Infringing Repository URL: [INSERT INFRINGING REPOSITORY URL HERE, e.g., https://github.com/infringer/repo]
Specific Infringing Files / Commits: [INSERT DETAILS OR STATE: Entire repository is an unauthorized clone]

3. INFRINGEMENT DETAILS:
The original work is licensed under a strict proprietary, source-available license with All Rights Reserved. The owner of the infringing repository has cloned/re-uploaded this copyrighted material in violation of my exclusive copyright rights and GitHub's Terms of Service. No commercial license, redistribution permission, or derivative work authorization was granted to this party.

4. GOOD FAITH STATEMENT:
I have a good faith belief that the use of the copyrighted material described above is not authorized by the copyright owner, its agent, or the law.

5. ACCURACY STATEMENT:
The information in this notification is accurate. Under penalty of perjury, I declare that I am the copyright owner or am authorized to act on behalf of the owner of an exclusive right that is allegedly infringed.

6. CONTACT INFORMATION:
Name: Sanjeev Kumar
Email: sanjeev2856@gmail.com
GitHub Username: primecoderIN
Address/Country: India

7. PHYSICAL / ELECTRONIC SIGNATURE:
/Sanjeev Kumar/
Date: [Insert Date]
```

---

## 4. Recommended GitHub Platform Settings for Protection

To further tighten security on the GitHub repository:

1. **Protect Main Branch:**
   * Go to **Settings** → **Branches** → Add branch protection rule for `main`.
   * Enable *Require pull request before merging*.
   * Enable *Require signed commits* if using GPG keys.
2. **Disable Unnecessary Public Collaboration Vectors:**
   * In **Settings** → **General** → **Features**:
     * Uncheck **Discussions** (if you do not want public community forums).
     * Uncheck **Wikis** (all authoritative docs reside in `/docs`).
     * Uncheck **Projects** (if tracking issues privately).
3. **Limit Pull Requests from Forks (Spam & Clones):**
   * In **Settings** → **Moderation options** → **Interaction limits**, you can limit interactions to existing contributors if you experience copycat spam.

