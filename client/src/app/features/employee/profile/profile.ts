import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { UserService } from '@core/services/user.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <div class="flex flex-col gap-8 max-w-3xl page-enter">
      <!-- Header -->
      <header class="flex flex-col gap-1">
        <p class="text-[0.72rem] font-extrabold uppercase tracking-wider text-primary-600 m-0 mb-1">Account</p>
        <h1 class="text-[1.75rem] font-bold text-slate-900 dark:text-white leading-[1.15] m-0">My Profile</h1>
        <p class="text-[0.9rem] text-slate-500 dark:text-slate-400 m-0 mt-1.5">
          View your personal details and workspace memberships.
        </p>
      </header>

      @if (userService.currentUser()) {
        <div class="flex flex-col gap-6">
          
          <!-- Profile Card -->
          <section class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden shadow-sm">
            <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/30">
              <h2 class="text-base font-semibold text-slate-800 dark:text-slate-100 m-0">Personal Details</h2>
            </div>
            <div class="p-6">
              <div class="flex items-center gap-5">
                <div class="flex items-center justify-center w-16 h-16 rounded-full bg-primary-100 dark:bg-primary-900/50 text-primary-600 dark:text-primary-400 text-2xl font-bold uppercase shrink-0">
                  {{ userService.currentUser()?.displayName?.charAt(0) || userService.currentUser()?.email?.charAt(0) }}
                </div>
                <div class="flex flex-col gap-1 min-w-0">
                  <h3 class="text-lg font-bold text-slate-900 dark:text-white m-0 truncate">
                    {{ userService.currentUser()?.displayName || 'User' }}
                  </h3>
                  <div class="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400 truncate">
                    <i class="pi pi-envelope text-xs"></i>
                    <span>{{ userService.currentUser()?.email }}</span>
                  </div>
                  <div class="flex items-center gap-2 text-[0.8rem] text-slate-400 dark:text-slate-500 mt-1">
                    <i class="pi pi-calendar text-xs"></i>
                    <span>Member since {{ userService.currentUser()?.createdAt | date:'longDate' }}</span>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <!-- Workspaces Card -->
          <section class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden shadow-sm">
            <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-950/30">
              <h2 class="text-base font-semibold text-slate-800 dark:text-slate-100 m-0">My Workspaces</h2>
            </div>
            <div class="flex flex-col">
              @for (workspace of userService.currentUser()?.memberships; track workspace.tenantId) {
                <div class="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800 last:border-b-0 hover:bg-slate-50 dark:hover:bg-slate-950/50 transition-colors">
                  <div class="flex items-center gap-3">
                    <div class="flex items-center justify-center w-10 h-10 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 shrink-0">
                      <i class="pi" [ngClass]="workspace.isPersonal ? 'pi-user' : 'pi-building'"></i>
                    </div>
                    <div class="flex flex-col">
                      <span class="text-[0.95rem] font-semibold text-slate-900 dark:text-white">{{ workspace.tenantName }}</span>
                      <span class="text-xs text-slate-500 capitalize">{{ workspace.role }}</span>
                    </div>
                  </div>
                  @if (workspace.tenantId === userService.currentTenant()?.tenantId) {
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-[0.7rem] font-bold bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-500 tracking-wide uppercase">Active</span>
                  }
                </div>
              }
            </div>
          </section>
          
          <!-- Actions -->
          <section class="flex flex-col gap-2 pt-2">
            <button (click)="manageAccount()" class="inline-flex items-center justify-center gap-2 px-5 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600 text-slate-700 dark:text-slate-200 text-sm font-semibold rounded-lg shadow-sm cursor-pointer transition-all self-start">
              <i class="pi pi-external-link text-xs opacity-70"></i>
              Manage Account settings
            </button>
            <p class="text-[0.8rem] text-slate-500 mt-1 ml-1 max-w-lg">
              Update your password or configure multi-factor authentication securely through the identity provider.
            </p>
          </section>

        </div>
      } @else {
        <!-- Loading -->
        <div class="flex flex-col gap-6 animate-pulse">
          <div class="h-32 bg-slate-100 dark:bg-slate-800 rounded-xl"></div>
          <div class="h-48 bg-slate-100 dark:bg-slate-800 rounded-xl"></div>
        </div>
      }
    </div>
  `
})
export class ProfileComponent {
  userService = inject(UserService);
  authService = inject(AuthService);

  manageAccount() {
    // Open the Keycloak account management page
    this.authService.manageAccount();
  }
}
