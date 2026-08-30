import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Set up your Organization
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600">
          Create a workspace for your company
        </p>
      </div>

      <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div class="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          <form [formGroup]="onboardingForm" (ngSubmit)="onSubmit()" class="space-y-6">
            
            <div *ngIf="error" class="bg-red-50 p-4 rounded-md text-red-700 text-sm">
              {{ error }}
            </div>

            <div>
              <label for="name" class="block text-sm font-medium text-gray-700">Organization Name</label>
              <div class="mt-1">
                <input id="name" type="text" formControlName="name" class="appearance-none block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm">
              </div>
            </div>

            <div>
              <label for="slug" class="block text-sm font-medium text-gray-700">Workspace URL Slug</label>
              <div class="mt-1 flex rounded-md shadow-sm">
                <span class="inline-flex items-center px-3 rounded-l-md border border-r-0 border-gray-300 bg-gray-50 text-gray-500 sm:text-sm">
                  normora.com/
                </span>
                <input type="text" id="slug" formControlName="slug" class="flex-1 min-w-0 block w-full px-3 py-2 rounded-none rounded-r-md border border-gray-300 focus:ring-blue-500 focus:border-blue-500 sm:text-sm">
              </div>
            </div>

            <div>
              <button type="submit" [disabled]="onboardingForm.invalid || isLoading" 
                      class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50">
                <span *ngIf="!isLoading">Create Workspace</span>
                <span *ngIf="isLoading">Creating...</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `
})
export class OnboardingComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  onboardingForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]]
  });

  isLoading = false;
  error = '';

  onSubmit() {
    if (this.onboardingForm.invalid) return;

    this.isLoading = true;
    this.error = '';

    const payload = this.onboardingForm.value;
    
    this.http.post<{success: boolean, message: string}>(`${environment.apiUrl}/api/tenants`, payload)
      .subscribe({
        next: (res) => {
          if (res.success) {
            // Need to reload to re-fetch the user profile with the new membership
            // The rootGuard will then route them to the employer dashboard
            window.location.href = '/employer/dashboard';
          } else {
            this.error = res.message;
            this.isLoading = false;
          }
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to create organization';
          this.isLoading = false;
        }
      });
  }
}
