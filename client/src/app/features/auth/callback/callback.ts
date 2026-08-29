import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

/**
 * Auth Callback Component
 *
 * This is the landing page after Keycloak/GitHub redirects the user back.
 * The URL will look like: /auth/callback?code=XXX&state=YYY
 *
 * The OidcSecurityService.checkAuth() in app.ts handles the actual token
 * exchange. This component just shows a loading state so the user does not
 * see the login form flash briefly during the OAuth callback round-trip.
 *
 * If checkAuth() has already resolved (it runs in app.ts ngOnInit), we
 * actively re-check here and navigate to the correct dashboard.
 */
@Component({
  selector: 'app-auth-callback',
  standalone: true,
  template: `
    <div style="display:flex;height:100vh;align-items:center;justify-content:center;background:#0f172a;flex-direction:column;gap:1rem;">
      <div style="width:48px;height:48px;border:4px solid #334155;border-top-color:#6366f1;border-radius:50%;animation:spin 0.8s linear infinite;"></div>
      <p style="color:#94a3b8;font-family:sans-serif;font-size:0.95rem;margin:0;">Completing sign-in…</p>
      <style>@keyframes spin{to{transform:rotate(360deg)}}</style>
    </div>
  `,
})
export class AuthCallback {
  // This component is intentionally empty.
  // The global app.ts component handles the checkAuth() token exchange
  // and routes the user to the correct dashboard automatically.
  // This component just prevents the login form from flashing during the redirect.
}
