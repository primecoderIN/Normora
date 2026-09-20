import { Component, inject, signal } from '@angular/core';

import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TenantService } from '@core/services/tenant.service';
import { AuthService } from '@core/services/auth.service';
import { UserService } from '@core/services/user.service';
import { InvitationService } from '@core/services/invitation.service';

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="min-h-screen bg-[#fafcff] flex relative overflow-hidden">
      <!-- Background decorators -->
      <div class="absolute top-0 right-0 w-[900px] h-[900px] bg-blue-50 rounded-full blur-[120px] opacity-50 -translate-y-1/2 translate-x-1/3 pointer-events-none"></div>
      <div class="absolute bottom-0 left-0 w-[700px] h-[700px] bg-indigo-50 rounded-full blur-[120px] opacity-60 translate-y-1/3 -translate-x-1/4 pointer-events-none"></div>

      <!-- Left branding panel -->
      <div class="hidden lg:flex flex-col justify-between w-[420px] flex-none bg-slate-950 text-white p-10 relative overflow-hidden">
        <!-- Panel decorators -->
        <div class="absolute top-0 right-0 w-64 h-64 bg-indigo-600/20 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2 pointer-events-none"></div>
        <div class="absolute bottom-0 left-0 w-48 h-48 bg-blue-600/10 rounded-full blur-3xl translate-y-1/2 -translate-x-1/2 pointer-events-none"></div>

        <!-- Logo -->
        <div class="relative z-10 flex items-center gap-3">
          <div class="flex items-center justify-center w-10 h-10 bg-indigo-600 rounded-xl font-extrabold text-xl shadow-lg shadow-indigo-900/50">N</div>
          <div>
            <div class="font-extrabold text-lg leading-tight">Normora</div>
            <div class="text-xs text-slate-400">Your Knowledge Workspace</div>
          </div>
        </div>

        <!-- Hero text -->
        <div class="relative z-10 space-y-6">
          <div>
            <h2 class="text-3xl font-black leading-tight tracking-tight mb-3">
              Your team's knowledge,<br/>always within reach.
            </h2>
            <p class="text-slate-400 text-sm leading-relaxed">
              Create a workspace to instantly surface company policies, documents, and answers — powered by AI.
            </p>
          </div>

          <!-- Feature list -->
          <ul class="space-y-3.5">
            @for (feature of features; track feature.label) {
              <li class="flex items-start gap-3">
                <div class="flex items-center justify-center flex-none w-7 h-7 bg-indigo-600/20 border border-indigo-500/30 rounded-lg mt-0.5">
                  <i [class]="'pi ' + feature.icon + ' text-indigo-400 text-xs'"></i>
                </div>
                <div>
                  <div class="text-sm font-semibold text-white">{{ feature.label }}</div>
                  <div class="text-xs text-slate-400 mt-0.5">{{ feature.description }}</div>
                </div>
              </li>
            }
          </ul>
        </div>

        <!-- Footer -->
        <div class="relative z-10 text-xs text-slate-500">© 2026 Normora. All rights reserved.</div>
      </div>

      <!-- Right content panel -->
      <div class="flex-1 flex flex-col items-center justify-center p-6 relative z-10">

        <!-- Mobile logo -->
        <div class="lg:hidden flex items-center gap-2 mb-8">
          <div class="flex items-center justify-center w-8 h-8 bg-indigo-600 rounded-lg font-extrabold text-white">N</div>
          <span class="font-extrabold text-slate-900">Normora</span>
        </div>

        <div class="w-full max-w-md space-y-5 page-enter">

          <!-- Pending invitations card -->
          @if (pendingInvitations().length > 0) {
            <div class="bg-white rounded-2xl border border-slate-100 shadow-xl shadow-slate-200/50 overflow-hidden">
              <div class="px-8 pt-8 pb-6">
                <div class="flex items-center gap-3 mb-5">
                  <div class="flex items-center justify-center w-11 h-11 bg-indigo-50 rounded-xl">
                    <i class="pi pi-envelope text-indigo-600 text-lg"></i>
                  </div>
                  <div>
                    <h2 class="text-lg font-bold text-slate-900 m-0">You've been invited!</h2>
                    <p class="text-sm text-slate-500 m-0">Accept to join your team's workspace</p>
                  </div>
                </div>

                <div class="space-y-2.5">
                  @for (invite of pendingInvitations(); track invite.token) {
                    <div class="flex items-center justify-between gap-3 p-3.5 bg-slate-50 border border-slate-200 rounded-xl hover:border-indigo-200 hover:bg-indigo-50/30 transition-colors group">
                      <div class="flex items-center gap-3 min-w-0">
                        <div class="flex items-center justify-center flex-none w-9 h-9 bg-white border border-slate-200 rounded-lg shadow-sm">
                          <i class="pi pi-building text-slate-500 text-sm"></i>
                        </div>
                        <span class="font-semibold text-slate-900 text-sm truncate">{{ invite.tenantName }}</span>
                      </div>
                      <button
                        type="button"
                        (click)="acceptInvite(invite.token)"
                        [disabled]="isAccepting()"
                        class="flex-none inline-flex items-center gap-1.5 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-xs font-bold rounded-lg transition-colors shadow-sm shadow-indigo-500/20 border-none cursor-pointer whitespace-nowrap"
                      >
                        @if (isAccepting() && acceptingToken() === invite.token) {
                          <i class="pi pi-spin pi-spinner text-[0.65rem]"></i> Joining...
                        } @else {
                          Accept & Join
                        }
                      </button>
                    </div>
                  }
                </div>

                @if (acceptError()) {
                  <div class="flex items-start gap-2 mt-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
                    <i class="pi pi-exclamation-circle mt-0.5 flex-none"></i>
                    <span>{{ acceptError() }}</span>
                  </div>
                }
              </div>

              <div class="px-8 py-4 bg-slate-50 border-t border-slate-100 text-center">
                <button type="button" class="text-sm font-semibold text-indigo-600 hover:text-indigo-700 transition-colors border-none bg-transparent cursor-pointer" (click)="showCreateForm.set(!showCreateForm())">
                  {{ showCreateForm() ? '← Hide workspace form' : 'Or create a new workspace →' }}
                </button>
              </div>
            </div>
          }

          <!-- Create workspace card -->
          @if (pendingInvitations().length === 0 || showCreateForm()) {
            <div class="bg-white rounded-2xl border border-slate-100 shadow-xl shadow-slate-200/50 overflow-hidden">
              <div class="px-8 pt-8 pb-8">
                <div class="flex items-center gap-3 mb-6">
                  <div class="flex items-center justify-center w-11 h-11 bg-indigo-50 rounded-xl">
                    <i class="pi pi-building text-indigo-600 text-lg"></i>
                  </div>
                  <div>
                    <h2 class="text-lg font-bold text-slate-900 m-0">Create a workspace</h2>
                    <p class="text-sm text-slate-500 m-0">Set up your organization in seconds</p>
                  </div>
                </div>

                <form [formGroup]="onboardingForm" (ngSubmit)="onSubmit()" class="space-y-5">
                  @if (error()) {
                    <div class="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700">
                      <i class="pi pi-exclamation-circle mt-0.5 flex-none"></i>
                      <span>{{ error() }}</span>
                    </div>
                  }

                  <div class="space-y-1.5">
                    <label for="name" class="block text-sm font-semibold text-slate-700">Organization name</label>
                    <input
                      id="name"
                      type="text"
                      formControlName="name"
                      (input)="autoSlug()"
                      placeholder="Acme Corp"
                      class="w-full h-11 px-4 bg-slate-50 border border-slate-200 rounded-xl text-sm text-slate-900 placeholder:text-slate-400 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100 transition-all"
                    />
                  </div>

                  <div class="space-y-1.5">
                    <label for="slug" class="block text-sm font-semibold text-slate-700">Workspace URL</label>
                    <div class="flex items-center h-11 bg-slate-50 border border-slate-200 rounded-xl overflow-hidden focus-within:border-indigo-500 focus-within:ring-2 focus-within:ring-indigo-100 transition-all">
                      <span class="flex items-center h-full px-3 text-slate-400 text-sm border-r border-slate-200 bg-slate-100 whitespace-nowrap select-none">normora.com/</span>
                      <input
                        id="slug"
                        type="text"
                        formControlName="slug"
                        placeholder="acme-corp"
                        class="flex-1 h-full px-3 bg-transparent text-sm text-slate-900 placeholder:text-slate-400 outline-none border-none"
                      />
                    </div>
                    <p class="text-xs text-slate-400">Use lowercase letters, numbers, and hyphens only.</p>
                  </div>

                  <button
                    type="submit"
                    [disabled]="onboardingForm.invalid || isLoading()"
                    class="w-full flex items-center justify-center gap-2 h-11 bg-indigo-600 hover:bg-indigo-700 disabled:bg-slate-200 disabled:text-slate-400 text-white font-bold text-sm rounded-xl border-none cursor-pointer transition-colors shadow-sm shadow-indigo-500/20 disabled:cursor-not-allowed"
                  >
                    @if (isLoading()) {
                      <i class="pi pi-spin pi-spinner text-sm"></i> Creating workspace…
                    } @else {
                      <i class="pi pi-arrow-right text-sm"></i> Create workspace
                    }
                  </button>
                </form>
              </div>
            </div>
          }

          <!-- Sign out -->
          <div class="text-center">
            <button (click)="logout()" class="text-sm text-slate-400 hover:text-slate-700 transition-colors border-none bg-transparent cursor-pointer">
              Sign out
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class OnboardingComponent {
  private fb = inject(FormBuilder);
  private tenantService = inject(TenantService);
  private router = inject(Router);
  private authService = inject(AuthService);
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

  /** Feature list shown in the left branding panel */
  features = [
    { icon: 'pi-sparkles', label: 'AI-powered Q&A', description: 'Get instant, cited answers from company documents.' },
    { icon: 'pi-file', label: 'Smart knowledge base', description: 'Upload policies, handbooks, and SOPs in any format.' },
    { icon: 'pi-users', label: 'Role-based access', description: 'Employers manage. Employees query. Everyone stays in sync.' },
    { icon: 'pi-shield', label: 'Secure by design', description: 'Scoped document access with department-level permissions.' },
  ];

  /** Auto-generate the slug from the organization name */
  autoSlug() {
    const name = this.onboardingForm.get('name')?.value ?? '';
    const slug = name
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-');
    this.onboardingForm.get('slug')?.setValue(slug, { emitEvent: false });
  }

  logout() {
    this.authService.logout();
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
