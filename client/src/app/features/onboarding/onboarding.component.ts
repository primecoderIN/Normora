import { Component, inject, signal } from '@angular/core';

import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TenantService } from '@core/services/tenant.service';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { UserService } from '@core/services/user.service';
import { InvitationService } from '@core/services/invitation.service';

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8 relative">
      <div class="absolute top-4 right-4">
        <button
          (click)="logout()"
          class="text-sm font-medium text-gray-500 hover:text-gray-900 transition-colors"
        >
          Sign Out
        </button>
      </div>

      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Welcome to Normora
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600">Join a workspace or create your own</p>
      </div>

      <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md space-y-6">
        @if (pendingInvitations().length > 0) {
          <div class="bg-white py-6 px-4 shadow sm:rounded-lg sm:px-10 border-t-4 border-indigo-500">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">You've been invited!</h3>
            <p class="text-sm text-gray-500 mb-4">You have pending invitations to join the following workspaces:</p>
            
            <div class="space-y-3">
              @for (invite of pendingInvitations(); track invite.token) {
                <div class="flex items-center justify-between p-3 border border-gray-200 rounded-lg bg-gray-50">
                  <span class="font-medium text-gray-900">{{ invite.tenantName }}</span>
                  <button
                    type="button"
                    (click)="acceptInvite(invite.token)"
                    [disabled]="isAccepting()"
                    class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50"
                  >
                    @if (isAccepting() && acceptingToken() === invite.token) {
                      Accepting...
                    } @else {
                      Accept & Join
                    }
                  </button>
                </div>
              }
            </div>

            @if (acceptError()) {
              <div class="mt-4 bg-red-50 p-3 rounded-md text-red-700 text-sm">
                {{ acceptError() }}
              </div>
            }
            
            <div class="mt-6 border-t border-gray-200 pt-4 text-center">
              <button type="button" class="text-sm font-medium text-indigo-600 hover:text-indigo-500" (click)="showCreateForm.set(!showCreateForm())">
                {{ showCreateForm() ? 'Hide create workspace form' : 'Or create a new workspace instead' }}
              </button>
            </div>
          </div>
        }

        @if (pendingInvitations().length === 0 || showCreateForm()) {
          <div class="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
            <h3 class="text-lg font-semibold text-gray-900 mb-4">Create a workspace</h3>
            <form [formGroup]="onboardingForm" (ngSubmit)="onSubmit()" class="space-y-6">
            @if (error()) {
              <div class="bg-red-50 p-4 rounded-md text-red-700 text-sm">
                {{ error() }}
              </div>
            }

            <div>
              <label for="name" class="block text-sm font-medium text-gray-700"
                >Organization Name</label
              >
              <div class="mt-1">
                <input
                  id="name"
                  type="text"
                  formControlName="name"
                  class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                />
              </div>
            </div>

            <div>
              <label for="slug" class="block text-sm font-medium text-gray-700"
                >Workspace URL Slug</label
              >
              <div class="mt-1 flex rounded-md shadow-sm">
                <span
                  class="inline-flex items-center px-3 rounded-l-md border border-r-0 border-gray-300 bg-gray-50 text-gray-500 sm:text-sm"
                >
                  normora.com/
                </span>
                <input
                  type="text"
                  id="slug"
                  formControlName="slug"
                  class="flex-1 min-w-0 block w-full px-3 py-2 rounded-none rounded-r-md border border-gray-300 focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                />
              </div>
            </div>

            <div>
              <button
                type="submit"
                [disabled]="onboardingForm.invalid || isLoading()"
                class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
              >
                @if (!isLoading()) {
                  <span>Create Workspace</span>
                }
                @if (isLoading()) {
                  <span>Creating...</span>
                }
              </button>
            </div>
          </form>
          </div>
        }
      </div>
    </div>
  `,
})
export class OnboardingComponent {
  private fb = inject(FormBuilder);
  private tenantService = inject(TenantService);
  private router = inject(Router);
  private oidcSecurityService = inject(OidcSecurityService);
  private userService = inject(UserService);
  private invitationService = inject(InvitationService);

  onboardingForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
  });

  isLoading = signal(false);
  error = signal('');
  
  pendingInvitations = signal(this.userService.currentUser()?.pendingInvitations || []);
  showCreateForm = signal(false);
  isAccepting = signal(false);
  acceptingToken = signal<string | null>(null);
  acceptError = signal('');

  logout() {
    this.oidcSecurityService.getIdToken().subscribe((idToken) => {
      this.oidcSecurityService.logoff('', { customParams: { id_token_hint: idToken } }).subscribe();
    });
  }

  // Process the acceptance of a workspace invitation and redirect the user to the employee portal on success
  acceptInvite(token: string) {
    this.isAccepting.set(true);
    this.acceptingToken.set(token);
    this.acceptError.set('');

    this.invitationService.acceptInvitation(token).subscribe({
      next: () => {
        window.location.href = '/employee/ask';
      },
      error: (err: any) => {
        this.acceptError.set(err.error?.message || 'Failed to accept invitation');
        this.isAccepting.set(false);
        this.acceptingToken.set(null);
      }
    });
  }

  // Submit the form to create a new workspace and route the newly minted admin to their employer dashboard
  onSubmit() {
    if (this.onboardingForm.invalid) return;

    this.isLoading.set(true);
    this.error.set('');

    const payload = this.onboardingForm.value;

    this.tenantService.createTenant(payload).subscribe({
      next: () => {
        // Need to reload to re-fetch the user profile with the new membership
        // The rootGuard will then route them to the employer dashboard
        window.location.href = '/employer/dashboard';
      },
      error: (err: any) => {
        this.error.set(err.error?.message || 'Failed to create organization');
        this.isLoading.set(false);
      },
    });
  }
}
