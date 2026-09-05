import { Component, inject, signal } from '@angular/core';

import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InvitationService } from '../../../core/services/invitation.service';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="grid gap-5 max-w-300 mx-auto text-surface-900">
      <header class="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
        <div>
          <p class="text-indigo-600 text-xs font-extrabold uppercase mb-1">Workspace access</p>
          <h1 class="text-3xl font-bold m-0 leading-tight">Employees</h1>
          <p class="mt-2 text-sm text-surface-500">Invite employees and manage who can use company knowledge.</p>
        </div>
        <button
          type="button"
          class="h-10 px-4 rounded-lg border border-surface-200 bg-white text-surface-700 font-semibold text-sm hover:bg-surface-50 transition-colors flex items-center gap-2"
        >
          <i class="pi pi-download text-xs"></i>
          Export
        </button>
      </header>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-white border border-surface-200 rounded-lg p-4 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-xs font-semibold text-surface-500 m-0">Active employees</p>
          <strong class="block text-2xl mt-1">342</strong>
        </div>
        <div class="bg-white border border-surface-200 rounded-lg p-4 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-xs font-semibold text-surface-500 m-0">Pending invites</p>
          <strong class="block text-2xl mt-1">4</strong>
        </div>
        <div class="bg-white border border-surface-200 rounded-lg p-4 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-xs font-semibold text-surface-500 m-0">Admin seats</p>
          <strong class="block text-2xl mt-1">3</strong>
        </div>
      </div>

      <section class="bg-white border border-surface-200 rounded-lg shadow-[0_1px_2px_rgba(15,23,42,0.04)] overflow-hidden">
        <div class="border-b border-surface-100 px-5 py-4">
          <h2 class="text-base font-bold m-0">Invite employee</h2>
          <p class="mt-1 text-sm text-surface-500">Send an invitation to join this workspace.</p>
        </div>
        <div class="p-5">
          <form
            [formGroup]="inviteForm"
            (ngSubmit)="onSubmit()"
            class="grid sm:grid-cols-[minmax(0,1fr)_auto] gap-3 max-w-220"
          >
            <label for="email" class="grid gap-1">
              <span class="text-xs font-semibold text-surface-600">Email address</span>
              <input
                type="email"
                name="email"
                id="email"
                formControlName="email"
                class="h-10 px-3 border border-surface-200 rounded-lg text-sm outline-none focus:border-indigo-500"
                placeholder="employee@company.com"
              />
            </label>
            <button
              type="submit"
              [disabled]="inviteForm.invalid || isLoading()"
              class="self-end h-10 px-4 rounded-lg border border-indigo-600 bg-indigo-600 text-white font-semibold text-sm hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed inline-flex items-center justify-center gap-2"
            >
              @if (!isLoading()) {
                <i class="pi pi-send text-xs"></i>
                <span>Send invite</span>
              }
              @if (isLoading()) {
                <span>Sending...</span>
              }
            </button>
          </form>

          @if (successMessage()) {
            <div class="mt-4 text-sm text-emerald-700 bg-emerald-50 border border-emerald-100 p-3 rounded-lg">
              {{ successMessage() }}
            </div>
          }
          @if (errorMessage()) {
            <div class="mt-4 text-sm text-red-700 bg-red-50 border border-red-100 p-3 rounded-lg">
              {{ errorMessage() }}
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
