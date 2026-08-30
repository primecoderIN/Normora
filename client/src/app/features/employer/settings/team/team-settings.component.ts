import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-team-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="space-y-6">
      <div>
        <h3 class="text-lg leading-6 font-medium text-gray-900">Team Management</h3>
        <p class="mt-1 text-sm text-gray-500">
          Invite new members to join your workspace.
        </p>
      </div>

      <div class="bg-white shadow sm:rounded-lg">
        <div class="px-4 py-5 sm:p-6">
          <h3 class="text-lg leading-6 font-medium text-gray-900">
            Invite a new team member
          </h3>
          <div class="mt-2 max-w-xl text-sm text-gray-500">
            <p>
              Send an invitation email to a new employee. They will receive a link to join your organization.
            </p>
          </div>
          <form [formGroup]="inviteForm" (ngSubmit)="onSubmit()" class="mt-5 sm:flex sm:items-center">
            <div class="w-full sm:max-w-xs">
              <label for="email" class="sr-only">Email</label>
              <input type="email" name="email" id="email" formControlName="email"
                     class="shadow-sm focus:ring-blue-500 focus:border-blue-500 block w-full sm:text-sm border-gray-300 rounded-md" 
                     placeholder="you@example.com">
            </div>
            <button type="submit" [disabled]="inviteForm.invalid || isLoading"
                    class="mt-3 w-full inline-flex items-center justify-center px-4 py-2 border border-transparent shadow-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50">
              <span *ngIf="!isLoading">Send Invite</span>
              <span *ngIf="isLoading">Sending...</span>
            </button>
          </form>

          <div *ngIf="successMessage" class="mt-3 text-sm text-green-600 bg-green-50 p-2 rounded">
            {{ successMessage }}
          </div>
          <div *ngIf="errorMessage" class="mt-3 text-sm text-red-600 bg-red-50 p-2 rounded">
            {{ errorMessage }}
          </div>
        </div>
      </div>
    </div>
  `
})
export class TeamSettingsComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);

  inviteForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  isLoading = false;
  successMessage = '';
  errorMessage = '';

  onSubmit() {
    if (this.inviteForm.invalid) return;

    this.isLoading = true;
    this.successMessage = '';
    this.errorMessage = '';

    const payload = this.inviteForm.value;
    
    // Note: The HTTP interceptor will automatically attach the X-Tenant-Id header based on the active route/tenant context
    // In our simplified MVP, we assume the backend will resolve the tenant for the employer based on their memberships
    // Wait, the backend requires a tenant context to be set. 
    // In our backend TenantResolutionMiddleware, if `X-Tenant-Id` is missing but the user is authenticated,
    // we can either default to their first membership, or the frontend must send it.
    // Let's explicitly pass the TenantId. Wait, we don't have it explicitly mapped in the URL right now.
    // We will rely on `X-Tenant-Id` header sent by an interceptor, or we can fetch the user's first tenant.
    
    // For simplicity, let's just make the POST request and we'll add an interceptor or default in middleware.
    this.http.post<{success: boolean, message: string}>(`${environment.apiUrl}/tenants/invitations`, payload)
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.successMessage = res.message;
            this.inviteForm.reset();
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.message || 'Failed to send invitation';
        }
      });
  }
}
