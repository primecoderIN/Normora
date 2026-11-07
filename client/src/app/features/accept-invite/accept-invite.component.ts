import { Component, inject, OnInit, signal } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { ApiMessages } from '@core/constants/api-messages';
import { AuthService } from '@core/services/auth.service';
import { InvitationService, InvitationDto } from '@core/services/invitation.service';
import { take } from 'rxjs';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-slate-950 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">Invitation</h2>
      </div>

      <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div class="bg-white dark:bg-slate-900 py-8 px-4 shadow sm:rounded-lg sm:px-10">
          @if (isLoading()) {
            <div class="flex justify-center">
              <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          }

          @if (!isLoading() && error()) {
            <div class="bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/50 p-4 rounded-md text-red-700 dark:text-red-300 text-sm text-center">
              {{ error() }}
            </div>
          }

          @if (!isLoading() && invitation()) {
            <div class="text-center">
              <p class="text-gray-700 dark:text-slate-300 mb-4">
                You have been invited to join <strong>{{ invitation()?.tenantName }}</strong
                >!
              </p>
              @if (isAuthenticated()) {
                <div class="space-y-3">
                  <button
                    (click)="acceptInvite()"
                    [disabled]="isAccepting()"
                    class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-base font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
                  >
                    @if (!isAccepting()) {
                      <span>Accept Invitation</span>
                    }
                    @if (isAccepting()) {
                      <span>Accepting...</span>
                    }
                  </button>
                  <button
                    (click)="rejectInvite()"
                    [disabled]="isAccepting()"
                    class="w-full flex justify-center py-2 px-4 border border-gray-300 dark:border-slate-700 shadow-sm text-base font-medium rounded-md text-gray-700 dark:text-slate-200 bg-white dark:bg-slate-800 hover:bg-gray-50 dark:hover:bg-slate-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
                  >
                    Decline Invitation
                  </button>
                </div>
              }
              @if (!isAuthenticated()) {
                <button
                  (click)="loginAndAccept()"
                  class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-base font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                  Log In to Accept
                </button>
              }
            </div>
          }

          @if (!isLoading() && success()) {
            <div class="text-center">
              <div class="bg-green-50 dark:bg-green-950/40 border border-green-200 dark:border-green-900/50 p-4 rounded-md text-green-700 dark:text-green-300 text-sm mb-4">
                {{ acceptSuccessMessage }}
              </div>
              <button
                (click)="goToApp()"
                class="text-blue-600 hover:text-blue-500 text-base font-medium"
              >
                Continue to App &rarr;
              </button>
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private invitationService = inject(InvitationService);

  token: string | null = null;
  readonly acceptSuccessMessage = ApiMessages.InvitationAccepted.detail;
  
  // State Signals
  invitation = signal<InvitationDto | null>(null);
  isLoading = signal(true);
  isAccepting = signal(false);
  error = signal('');
  success = signal(false);
  isAuthenticated = signal(false);

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token');

    if (!this.token) {
      this.error.set(ApiMessages.InvalidInvitation);
      this.isLoading.set(false);
      return;
    }

    // Check if user is logged in via BFF
    this.authService.checkAuth().pipe(take(1)).subscribe((isAuthenticated) => {
      this.isAuthenticated.set(isAuthenticated);
      this.loadInvitation();
    });
  }

  loadInvitation() {
    // Invitation details are safe to inspect anonymously; the API remains authoritative
    // when accepting and requires the authenticated email to match the recipient.
    this.invitationService.getInvitation(this.token!).subscribe({
      next: (res: { success?: boolean; data?: InvitationDto; message?: string }) => {
        this.isLoading.set(false);
        if (res.success && res.data) {
          this.invitation.set(res.data);
          if (this.invitation()!.status !== 'Pending') {
            this.error.set(ApiMessages.InvitationAlreadyAccepted);
            this.invitation.set(null);
          } else if (new Date(this.invitation()!.expiresAt) < new Date()) {
            this.error.set(ApiMessages.InvitationExpired);
            this.invitation.set(null);
          }
        } else {
          this.error.set(res.message || ApiMessages.InvitationInvalidOrMissing);
        }
      },
      error: (err: { error?: { message?: string } }) => {
        this.isLoading.set(false);
        this.error.set(err.error?.message || ApiMessages.InvitationInvalidOrMissing);
      },
    });
  }

  loginAndAccept() {
    // Cross-Redirect State Preservation:
    // When an unauthenticated user arrives via an email invitation link, we save the invitation
    // token to localStorage ('pending_invitation') before initiating the OIDC full-page redirect to Keycloak.
    // Upon successful authentication, the BFF callback handler reads this key and redirects back
    // to this page to complete the acceptance workflow without losing context.
    localStorage.setItem('pending_invitation', this.token!);
    this.authService.login();
  }

  acceptInvite() {
    // Authoritative Invitation Acceptance:
    // Dispatches POST /api/invitations/accept with the invitation token.
    // Backend security invariants enforced:
    // 1. Authenticated email must match invitation email exactly (anti-account-hijacking defense).
    // 2. Token must not be expired or previously processed.
    // 3. Quota check: Tenant must have available employee seats under active TenantPlan.
    // 4. JIT provisioning: Adds user to TenantMembership with TenantRole.Employee.
    if (!this.token) return;

    this.isAccepting.set(true);
    this.error.set('');

    this.invitationService.acceptInvitation(this.token).subscribe({
      next: (res: { success?: boolean; message?: string }) => {
        this.isAccepting.set(false);
        if (res.success) {
          this.success.set(true);
          this.invitation.set(null);
          localStorage.removeItem('pending_invitation');
        } else {
          this.error.set(res.message || ApiMessages.InvitationAcceptFailed.detail);
        }
      },
      error: (err: { error?: { message?: string } }) => {
        this.isAccepting.set(false);
        this.error.set(err.error?.message || ApiMessages.InvitationAcceptFailed.detail);
      },
    });
  }

  rejectInvite() {
    if (!this.token) return;

    this.isAccepting.set(true);
    this.error.set('');

    this.invitationService.rejectInvitation(this.token).subscribe({
      next: (res: { success?: boolean; message?: string }) => {
        this.isAccepting.set(false);
        if (res.success) {
          this.router.navigate(['/']);
        } else {
          this.error.set(res.message || ApiMessages.InvitationRejectFailed.detail);
        }
      },
      error: (err: { error?: { message?: string } }) => {
        this.isAccepting.set(false);
        this.error.set(err.error?.message || ApiMessages.InvitationRejectFailed.detail);
      },
    });
  }

  goToApp() {
    this.router.navigate(['/']);
  }
}
