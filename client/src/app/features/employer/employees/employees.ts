import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InvitationService } from '../../../core/services/invitation.service';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule],
  template: `
    <div class="grid gap-8 text-surface-900">
      <header class="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
        <div>
          <p class="text-[0.72rem] font-extrabold uppercase tracking-wider text-indigo-600 m-0 mb-1">Workspace access</p>
          <h1 class="text-[1.75rem] font-bold text-surface-900 leading-[1.15] m-0">Employees</h1>
          <p class="text-[0.9rem] text-surface-500 m-0 mt-1.5">Invite employees and manage who can use company knowledge.</p>
        </div>
        <p-button label="Export" icon="pi pi-download" styleClass="!bg-white !border-surface-200 !text-surface-700 hover:!bg-surface-50 font-bold !px-4 !py-2 !h-10 transition-colors"></p-button>
      </header>

      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="bg-white border border-surface-200 rounded-lg p-5 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-sm font-medium text-surface-500 m-0">Active employees</p>
          <strong class="block text-2xl font-bold text-surface-900 mt-2">342</strong>
        </div>
        <div class="bg-white border border-surface-200 rounded-lg p-5 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-sm font-medium text-surface-500 m-0">Pending invites</p>
          <strong class="block text-2xl font-bold text-surface-900 mt-2">4</strong>
        </div>
        <div class="bg-white border border-surface-200 rounded-lg p-5 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-sm font-medium text-surface-500 m-0">Admin seats</p>
          <strong class="block text-2xl font-bold text-surface-900 mt-2">3</strong>
        </div>
      </div>

      <section class="bg-white border border-surface-200 rounded-lg shadow-[0_1px_2px_rgba(15,23,42,0.04)] overflow-hidden">
        <div class="border-b border-surface-100 p-5">
          <h2 class="text-lg font-bold text-surface-900 m-0">Invite employee</h2>
          <p class="text-sm text-surface-500 m-0 mt-1">Send an invitation to join this workspace.</p>
        </div>
        <div class="p-5">
          <form
            [formGroup]="inviteForm"
            (ngSubmit)="onSubmit()"
            class="grid sm:grid-cols-[minmax(0,1fr)_auto] gap-4 max-w-xl"
          >
            <label class="flex flex-col gap-1.5">
              <span class="text-sm font-semibold text-surface-700">Email address</span>
              <input
                type="email"
                formControlName="email"
                class="w-full h-10 px-3 bg-white border border-surface-300 rounded-md text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all"
                placeholder="employee@company.com"
              />
            </label>
            <div class="self-end pb-0.5">
              <p-button [label]="isLoading() ? 'Sending...' : 'Send invite'" type="submit" [disabled]="inviteForm.invalid || isLoading()" [icon]="isLoading() ? 'pi pi-spin pi-spinner' : 'pi pi-send'" styleClass="!bg-indigo-600 !border-indigo-600 !text-white hover:!bg-indigo-700 disabled:!bg-surface-300 disabled:!border-surface-300 disabled:!text-surface-500 font-bold !px-4 !py-2 !h-10 transition-colors"></p-button>
            </div>
          </form>

          @if (successMessage()) {
            <div class="flex items-start gap-2 p-3 mt-4 bg-emerald-50 border border-emerald-200 rounded-md text-sm text-emerald-700">
              <i class="pi pi-check-circle mt-0.5"></i>
              <span>{{ successMessage() }}</span>
            </div>
          }
          @if (errorMessage()) {
            <div class="flex items-start gap-2 p-3 mt-4 bg-red-50 border border-red-200 rounded-md text-sm text-red-700">
              <i class="pi pi-exclamation-circle mt-0.5"></i>
              <span>{{ errorMessage() }}</span>
            </div>
          }
        </div>
      </section>
    </div>
  `,
})
export class Employees {
  private fb = inject(FormBuilder);
  private invitationService = inject(InvitationService);

  inviteForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  isLoading = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  onSubmit() {
    if (this.inviteForm.invalid) return;

    this.isLoading.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    const payload = this.inviteForm.value;

    this.invitationService.inviteEmployee(payload.email).subscribe({
      next: (res: any) => {
        this.isLoading.set(false);
        if (res.success) {
          this.successMessage.set(res.message);
          this.inviteForm.reset();
        } else {
          this.errorMessage.set(res.message);
        }
      },
      error: (err: any) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to send invitation');
      },
    });
  }
}
