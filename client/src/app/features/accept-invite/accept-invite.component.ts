import { Component, inject, OnInit, signal } from '@angular/core';

import { ActivatedRoute, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { InvitationService, InvitationDto } from '../../core/services/invitation.service';
import { take } from 'rxjs';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [],
  template: `
    <div class="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">Invitation</h2>
      </div>

      <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div class="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          @if (isLoading()) {
            <div class="flex justify-center">
              <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          }

          @if (!isLoading() && error()) {
            <div class="bg-red-50 p-4 rounded-md text-red-700 text-sm text-center">
              {{ error() }}
            </div>
          }

          @if (!isLoading() && invitation()) {
            <div class="text-center">
              <p class="text-gray-700 mb-4">
                You have been invited to join <strong>{{ invitation()?.tenantName }}</strong
                >!
              </p>
              @if (isAuthenticated()) {
                <button
                  (click)="acceptInvite()"
                  [disabled]="isAccepting()"
                  class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
                >
                  @if (!isAccepting()) {
                    <span>Accept Invitation</span>
                  }
                  @if (isAccepting()) {
                    <span>Accepting...</span>
                  }
                </button>
              }
              @if (!isAuthenticated()) {
                <button
                  (click)="loginAndAccept()"
                  class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                  Log In to Accept
                </button>
              }
            </div>
          }

          @if (!isLoading() && success()) {
            <div class="text-center">
              <div class="bg-green-50 p-4 rounded-md text-green-700 text-sm mb-4">
                Invitation accepted successfully!
              </div>
              <button
                (click)="goToApp()"
                class="text-blue-600 hover:text-blue-500 text-sm font-medium"
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
  private oidcSecurityService = inject(OidcSecurityService);
  private invitationService = inject(InvitationService);

  token: string | null = null;
  
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
      this.error.set('Invalid invitation link.');
      this.isLoading.set(false);
      return;
    }

    // Check if user is logged in
    this.oidcSecurityService.isAuthenticated$.pipe(take(1)).subscribe(({ isAuthenticated }) => {
      this.isAuthenticated.set(isAuthenticated);
      this.loadInvitation();
    });
  }

  loadInvitation() {
    // Invitation details are safe to inspect anonymously; the API remains authoritative
    // when accepting and requires the authenticated email to match the recipient.
    this.invitationService.getInvitation(this.token!).subscribe({
      next: (res: any) => {
        this.isLoading.set(false);
        if (res.success && res.data) {
          this.invitation.set(res.data);
          if (this.invitation()!.status !== 'Pending') {
            this.error.set('This invitation has already been accepted or is no longer valid.');
            this.invitation.set(null);
          } else if (new Date(this.invitation()!.expiresAt) < new Date()) {
            this.error.set('This invitation link has expired.');
            this.invitation.set(null);
          }
        } else {
          this.error.set(res.message);
        }
      },
      error: (err: any) => {
        this.isLoading.set(false);
        this.error.set(err.error?.message || 'Failed to load invitation.');
      },
    });
  }

  loginAndAccept() {
    // Preserve the token across Keycloak's full-page redirect; app.ts consumes it after
    // authentication and routes back here before the normal workspace destination.
    localStorage.setItem('pending_invitation', this.token!);
    this.oidcSecurityService.authorize();
  }

  acceptInvite() {
    if (!this.token) return;

    this.isAccepting.set(true);
    this.error.set('');

    this.invitationService.acceptInvitation(this.token).subscribe({
      next: (res: any) => {
        this.isAccepting.set(false);
        if (res.success) {
          this.success.set(true);
          this.invitation.set(null);
          localStorage.removeItem('pending_invitation');
        } else {
          this.error.set(res.message);
        }
      },
      error: (err: any) => {
        this.isAccepting.set(false);
        this.error.set(err.error?.message || 'Failed to accept invitation.');
      },
    });
  }

  goToApp() {
    this.router.navigate(['/employee/ask']);
  }
}
