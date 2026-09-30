import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InvitationService } from '@core/services/invitation.service';
import { TenantService } from '@core/services/tenant.service';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonModule, DatePipe],
  template: `
    <div class="grid gap-8 text-slate-900 dark:text-white page-enter">
      <header class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
        <div>
          <p class="text-[0.7rem] font-extrabold uppercase tracking-widest text-primary-600 m-0 mb-1.5">Workspace access</p>
          <h1 class="text-2xl sm:text-[1.75rem] font-bold text-slate-900 dark:text-white leading-[1.15] m-0">Employees</h1>
          <p class="text-sm text-slate-500 dark:text-slate-400 m-0 mt-1.5">Invite employees and manage who can use company knowledge.</p>
        </div>
        <p-button label="Export" icon="pi pi-download" styleClass="!bg-white dark:bg-slate-900 !border-slate-200 dark:border-slate-700 !text-slate-700 dark:text-slate-200 hover:!bg-slate-50 dark:bg-slate-950 !font-semibold !px-4 !py-2 !h-10 !text-sm transition-colors self-start"></p-button>
      </header>

      <!-- Stat cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl p-5 shadow-sm flex items-center gap-4">
          <div class="flex items-center justify-center flex-none w-11 h-11 bg-primary-50 text-primary-600 rounded-xl">
            <i class="pi pi-users text-lg"></i>
          </div>
          <div>
            <p class="text-xs font-medium text-slate-500 dark:text-slate-400 m-0 mb-0.5">Active employees</p>
            <strong class="text-2xl font-bold text-slate-900 dark:text-white">{{ employees().length }}</strong>
          </div>
        </div>
        <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl p-5 shadow-sm flex items-center gap-4">
          <div class="flex items-center justify-center flex-none w-11 h-11 bg-amber-50 text-amber-600 rounded-xl">
            <i class="pi pi-envelope text-lg"></i>
          </div>
          <div>
            <p class="text-xs font-medium text-slate-500 dark:text-slate-400 m-0 mb-0.5">Pending invites</p>
            <strong class="text-2xl font-bold text-slate-900 dark:text-white">{{ pendingInvites() }}</strong>
          </div>
        </div>
        <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl p-5 shadow-sm flex items-center gap-4">
          <div class="flex items-center justify-center flex-none w-11 h-11 bg-purple-50 text-purple-600 rounded-xl">
            <i class="pi pi-shield text-lg"></i>
          </div>
          <div>
            <p class="text-xs font-medium text-slate-500 dark:text-slate-400 m-0 mb-0.5">Admin seats</p>
            <strong class="text-2xl font-bold text-slate-900 dark:text-white">{{ adminSeats() }}</strong>
          </div>
        </div>
      </div>

      <!-- Invite section -->
      <section class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl shadow-sm overflow-hidden">
        <div class="border-b border-slate-100 dark:border-slate-800 px-6 py-5">
          <h2 class="text-base font-bold text-slate-900 dark:text-white m-0">Invite employee</h2>
          <p class="text-sm text-slate-500 dark:text-slate-400 m-0 mt-0.5">Send an invitation link to join this workspace.</p>
        </div>
        <div class="px-6 py-5">
          @if (successMessage()) {
            <div class="flex items-center gap-2 p-4 mb-5 bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-800 rounded-xl text-emerald-800 dark:text-emerald-300">
              <div class="flex items-center justify-center flex-none w-8 h-8 bg-emerald-100 dark:bg-emerald-900/40 rounded-lg">
                <i class="pi pi-check-circle text-emerald-600 dark:text-emerald-400"></i>
              </div>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-bold m-0">Invitation sent!</p>
                <p class="text-xs text-emerald-700 dark:text-emerald-400 m-0 mt-0.5 truncate">{{ successMessage() }}</p>
              </div>
              <button type="button" class="flex-none ml-auto flex items-center justify-center w-6 h-6 rounded-md hover:bg-emerald-100 dark:hover:bg-emerald-900/40 text-emerald-500 border-none cursor-pointer transition-colors" (click)="successMessage.set('')">
                <i class="pi pi-times text-base"></i>
              </button>
            </div>
          }
          @if (errorMessage()) {
            <div class="flex items-start gap-2.5 p-3 mb-5 bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-800 rounded-xl text-red-700 dark:text-red-400 text-sm" role="alert">
              <i class="pi pi-exclamation-circle mt-0.5 flex-none"></i>
              <span>{{ errorMessage() }}</span>
            </div>
          }
          <form
            [formGroup]="inviteForm"
            (ngSubmit)="onSubmit()"
            class="flex flex-col sm:flex-row gap-3 w-full"
          >
            <div class="flex-1 relative">
              <i class="pi pi-envelope absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-base pointer-events-none"></i>
              <input
                type="email"
                formControlName="email"
                class="w-full h-10 pl-9 pr-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-700 rounded-lg text-base outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-100 transition-all placeholder:text-slate-400"
                placeholder="employee@company.com"
              />
            </div>
            <p-button
              [label]="isInviting() ? 'Sending...' : 'Send invite'"
              type="submit"
              [disabled]="inviteForm.invalid || isInviting()"
              [icon]="isInviting() ? 'pi pi-spin pi-spinner' : 'pi pi-send'"
              styleClass="!bg-primary-600 !border-primary-600 !text-white hover:!bg-primary-700 disabled:!bg-slate-200 disabled:!border-slate-200 dark:border-slate-700 disabled:!text-slate-400 font-bold !px-4 !h-10 transition-colors whitespace-nowrap">
            </p-button>
          </form>
        </div>
      </section>

      <!-- Employees table -->
      <section class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl shadow-sm overflow-hidden">
        <div class="flex items-center justify-between gap-4 border-b border-slate-100 dark:border-slate-800 px-6 py-4">
          <h2 class="text-base font-bold text-slate-900 dark:text-white m-0">Members</h2>
          <label class="flex items-center gap-2 h-9 px-3 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-700 rounded-lg w-52 focus-within:border-primary-400 transition-colors">
            <i class="pi pi-search text-slate-400 text-base"></i>
            <input type="text" placeholder="Search members" class="flex-1 min-w-0 bg-transparent border-none! outline-none! focus:outline-none! focus:border-transparent! focus:ring-0! text-base text-slate-900 dark:text-white placeholder:text-slate-400 p-0" />
          </label>
        </div>
        
        @if (isLoadingEmployees()) {
          <div class="divide-y divide-slate-100">
            @for (row of skeletonRows; track row) {
              <div class="flex items-center gap-4 px-6 py-4 animate-pulse">
                <div class="flex-none w-9 h-9 bg-slate-200 rounded-full"></div>
                <div class="flex-1 space-y-2">
                  <div class="h-3 bg-slate-200 rounded-full w-1/4"></div>
                  <div class="h-2.5 bg-slate-100 dark:bg-slate-800 rounded-full w-1/3"></div>
                </div>
                <div class="h-6 w-16 bg-slate-100 dark:bg-slate-800 rounded-full"></div>
                <div class="h-6 w-14 bg-slate-100 dark:bg-slate-800 rounded-full"></div>
              </div>
            }
          </div>
        } @else if (employees().length > 0) {
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm border-collapse">
              <thead>
                <tr class="border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-950/50">
                  <th class="px-4 sm:px-6 py-3 font-semibold text-slate-600 dark:text-slate-300 text-xs uppercase tracking-wider">User</th>
                  <th class="px-4 sm:px-6 py-3 font-semibold text-slate-600 dark:text-slate-300 text-xs uppercase tracking-wider">Role</th>
                  <th class="px-4 sm:px-6 py-3 font-semibold text-slate-600 dark:text-slate-300 text-xs uppercase tracking-wider hidden sm:table-cell">Joined</th>
                  <th class="px-4 sm:px-6 py-3 font-semibold text-slate-600 dark:text-slate-300 text-xs uppercase tracking-wider w-16">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100">
                @for (emp of employees(); track emp.userId) {
                  <tr class="hover:bg-slate-50 dark:hover:bg-slate-950/80 transition-colors group">
                    <td class="px-6 py-3 whitespace-nowrap">
                      <div class="flex items-center gap-3">
                        <div class="flex-none w-9 h-9 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center font-bold text-sm">
                          {{ emp.displayName ? emp.displayName.charAt(0).toUpperCase() : (emp.email ? emp.email.charAt(0).toUpperCase() : '?') }}
                        </div>
                        <div>
                          <p class="font-semibold text-slate-900 dark:text-white m-0">{{ emp.displayName || '—' }}</p>
                          <p class="text-slate-500 dark:text-slate-400 text-xs m-0 mt-0.5">{{ emp.email }}</p>
                        </div>
                      </div>
                    </td>
                    <td class="px-6 py-3 whitespace-nowrap">
                      <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium" 
                        [ngClass]="emp.role === 'Admin' ? 'bg-purple-50 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 border border-purple-200 dark:border-purple-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700'">
                        {{ emp.role }}
                      </span>
                    </td>
                    <td class="px-4 sm:px-6 py-3 whitespace-nowrap text-slate-500 dark:text-slate-400 hidden sm:table-cell">
                      {{ emp.joinedAt | date:'mediumDate' }}
                    </td>
                    <td class="px-6 py-3 whitespace-nowrap">
                      <button type="button" class="w-8 h-8 rounded-md flex items-center justify-center text-slate-400 hover:bg-slate-200 hover:text-slate-700 dark:hover:text-slate-200 transition-colors opacity-0 group-hover:opacity-100 focus:opacity-100">
                        <i class="pi pi-ellipsis-v text-base"></i>
                      </button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        } @else {
          <div class="flex flex-col items-center justify-center gap-3 py-12 text-center">
            <div class="flex items-center justify-center w-14 h-14 rounded-2xl bg-primary-50 text-primary-400">
              <i class="pi pi-users text-2xl"></i>
            </div>
            <p class="text-sm font-medium text-slate-500 dark:text-slate-400 m-0">No members yet — invite your first employee above.</p>
          </div>
        }
      </section>
    </div>
  `,
})
export class Employees implements OnInit {
  private fb = inject(FormBuilder);
  private invitationService = inject(InvitationService);
  private tenantService = inject(TenantService);

  inviteForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
  });

  isInviting = signal(false);
  isLoadingEmployees = signal(true);
  employees = signal<any[]>([]);
  pendingInvites = signal<number>(0);
  adminSeats = signal<number>(0);
  successMessage = signal('');
  errorMessage = signal('');

  readonly skeletonRows = [1, 2, 3, 4, 5];

  ngOnInit() {
    this.loadEmployees();
  }

  loadEmployees() {
    this.isLoadingEmployees.set(true);
    this.tenantService.getEmployees().subscribe({
      next: (res) => {
        this.employees.set(res.data || []);
        this.isLoadingEmployees.set(false);
      },
      error: (err) => {
        console.error('Failed to load employees', err);
        this.isLoadingEmployees.set(false);
      }
    });

    this.tenantService.getEmployeeStats().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.pendingInvites.set(res.data.pendingInvites);
          this.adminSeats.set(res.data.adminSeats);
        }
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  onSubmit() {
    if (this.inviteForm.invalid) return;

    this.isInviting.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    const payload = this.inviteForm.value;

    this.invitationService.inviteEmployee(payload.email).subscribe({
      next: (res: any) => {
        this.isInviting.set(false);
        if (res.success) {
          this.successMessage.set(res.message);
          this.inviteForm.reset();
          // Optionally reload employees list
          this.loadEmployees();
        } else {
          this.errorMessage.set(res.message);
        }
      },
      error: (err: any) => {
        this.isInviting.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to send invitation');
      },
    });
  }
}
