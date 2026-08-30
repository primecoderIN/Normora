import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Invitation
        </h2>
      </div>

      <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div class="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          
          <div *ngIf="isLoading" class="flex justify-center">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>

          <div *ngIf="!isLoading && error" class="bg-red-50 p-4 rounded-md text-red-700 text-sm text-center">
            {{ error }}
          </div>

          <div *ngIf="!isLoading && invitation" class="text-center">
            <p class="text-gray-700 mb-4">
              You have been invited to join <strong>{{ invitation.tenantName }}</strong>!
            </p>

            <button *ngIf="isAuthenticated" (click)="acceptInvite()" [disabled]="isAccepting"
                    class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50">
              <span *ngIf="!isAccepting">Accept Invitation</span>
              <span *ngIf="isAccepting">Accepting...</span>
            </button>

            <button *ngIf="!isAuthenticated" (click)="loginAndAccept()"
                    class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
              Log In to Accept
            </button>
          </div>

          <div *ngIf="!isLoading && success" class="text-center">
            <div class="bg-green-50 p-4 rounded-md text-green-700 text-sm mb-4">
              Invitation accepted successfully!
            </div>
            <button (click)="goToApp()" class="text-blue-600 hover:text-blue-500 text-sm font-medium">
              Continue to App &rarr;
            </button>
          </div>

        </div>
      </div>
    </div>
  `
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);
  private oidcSecurityService = inject(OidcSecurityService);

  token: string | null = null;
  invitation: any = null;
  
  isLoading = true;
  isAccepting = false;
  error = '';
  success = false;
  isAuthenticated = false;

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token');

    if (!this.token) {
      this.error = 'Invalid invitation link.';
      this.isLoading = false;
      return;
    }

    // Check if user is logged in
    this.oidcSecurityService.isAuthenticated$.subscribe(({ isAuthenticated }) => {
      this.isAuthenticated = isAuthenticated;
      this.loadInvitation();
    });
  }

  loadInvitation() {
    this.http.get<{success: boolean, message: string, data: any}>(`${environment.apiUrl}/api/invitations/${this.token}`)
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.invitation = res.data;
            if (this.invitation.status !== 'Pending') {
              this.error = 'This invitation has already been accepted or is no longer valid.';
              this.invitation = null;
            }
          } else {
            this.error = res.message;
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.error = err.error?.message || 'Failed to load invitation.';
        }
      });
  }

  loginAndAccept() {
    // Save the token to local storage so we can accept it after the redirect flow
    localStorage.setItem('pending_invitation', this.token!);
    this.oidcSecurityService.authorize();
  }

  acceptInvite() {
    if (!this.token) return;

    this.isAccepting = true;
    this.error = '';

    this.http.post<{success: boolean, message: string}>(`${environment.apiUrl}/api/invitations/${this.token}/accept`, {})
      .subscribe({
        next: (res) => {
          this.isAccepting = false;
          if (res.success) {
            this.success = true;
            this.invitation = null;
            localStorage.removeItem('pending_invitation');
          } else {
            this.error = res.message;
          }
        },
        error: (err) => {
          this.isAccepting = false;
          this.error = err.error?.message || 'Failed to accept invitation.';
        }
      });
  }

  goToApp() {
    window.location.href = '/employee/ask';
  }
}
