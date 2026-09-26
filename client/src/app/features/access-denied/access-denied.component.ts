import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  template: `
    <div class="access-denied-wrapper">
      <div class="access-denied-card">
        <div class="icon-ring">
          <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 24 24" fill="none"
               stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/>
          </svg>
        </div>
        <h1>Access Denied</h1>
        <p class="subtitle">You don't have permission to view this page.</p>
        <p class="hint">
          If you believe this is a mistake, contact your workspace administrator
          or try switching to a different workspace.
        </p>
        <div class="actions">
          <button class="btn-primary" (click)="goHome()">Go to Dashboard</button>
          <button class="btn-ghost" (click)="logout()">Sign Out</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; height: 100vh; }

    .access-denied-wrapper {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
      padding: 2rem;
    }

    .access-denied-card {
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 1.5rem;
      padding: 3rem 2.5rem;
      max-width: 440px;
      width: 100%;
      text-align: center;
      backdrop-filter: blur(12px);
      box-shadow: 0 25px 50px rgba(0, 0, 0, 0.4);
      animation: fadeSlideIn 0.4s ease;
    }

    @keyframes fadeSlideIn {
      from { opacity: 0; transform: translateY(20px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    .icon-ring {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: rgba(239, 68, 68, 0.15);
      border: 2px solid rgba(239, 68, 68, 0.3);
      color: #f87171;
      margin-bottom: 1.5rem;
    }

    h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #f1f5f9;
      margin: 0 0 0.5rem;
      font-family: 'Inter', sans-serif;
    }

    .subtitle {
      font-size: 1rem;
      color: #94a3b8;
      margin: 0 0 0.75rem;
    }

    .hint {
      font-size: 0.875rem;
      color: #64748b;
      line-height: 1.6;
      margin: 0 0 2rem;
    }

    .actions {
      display: flex;
      gap: 0.75rem;
      justify-content: center;
      flex-wrap: wrap;
    }

    .btn-primary {
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: #fff;
      border: none;
      padding: 0.625rem 1.5rem;
      border-radius: 0.625rem;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: opacity 0.2s, transform 0.1s;
    }
    .btn-primary:hover { opacity: 0.9; transform: translateY(-1px); }

    .btn-ghost {
      background: transparent;
      color: #94a3b8;
      border: 1px solid rgba(148, 163, 184, 0.3);
      padding: 0.625rem 1.5rem;
      border-radius: 0.625rem;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: border-color 0.2s, color 0.2s;
    }
    .btn-ghost:hover { border-color: #94a3b8; color: #f1f5f9; }
  `]
})
export class AccessDeniedComponent {
  private router = inject(Router);
  private authService = inject(AuthService);

  goHome() {
    this.router.navigate(['/']);
  }

  logout() {
    this.authService.logout();
  }
}
