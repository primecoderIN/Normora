import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DepartmentService, Department } from '../../../core/services/department.service';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonModule],
  template: `
    <div class="flex flex-col gap-8">
      <!-- Header -->
      <header class="flex items-center justify-between gap-4">
        <div>
          <p class="text-[0.72rem] font-extrabold uppercase tracking-wider text-indigo-600 m-0 mb-1">Organisation</p>
          <h1 class="text-[1.75rem] font-bold text-surface-900 leading-[1.15] m-0">Departments</h1>
          <p class="text-[0.9rem] text-surface-500 m-0 mt-1.5">Create departments to scope document access for your employees.</p>
        </div>
        <p-button label="New department" icon="pi pi-plus" (onClick)="openCreate()" styleClass="!bg-indigo-600 !border-indigo-600 !text-white hover:!bg-indigo-700 font-bold !px-4 !py-2 !h-10 whitespace-nowrap transition-colors"></p-button>
      </header>

      <!-- Stats -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="bg-white border border-surface-200 rounded-lg p-5 shadow-[0_1px_2px_rgba(15,23,42,0.04)]">
          <p class="text-sm font-medium text-surface-500 m-0">Total departments</p>
          <strong class="block text-2xl font-bold text-surface-900 mt-2">{{ departments().length }}</strong>
        </div>
      </div>

      <!-- List -->
      <section class="bg-white border border-surface-200 rounded-lg shadow-[0_1px_2px_rgba(15,23,42,0.04)] overflow-hidden">
        <div class="flex items-center justify-between gap-4 p-5 border-b border-surface-100">
          <h2 class="text-lg font-bold text-surface-900 m-0">All departments</h2>
          <div class="relative w-full max-w-xs">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-surface-400"></i>
            <input
              type="search"
              class="w-full h-9 pl-9 pr-3 bg-surface-50 border border-surface-200 rounded-md text-sm outline-none focus:border-indigo-400 focus:ring-1 focus:ring-indigo-400 transition-all"
              placeholder="Search departments…"
              [value]="searchTerm()"
              (input)="searchTerm.set($any($event.target).value)"
            />
          </div>
        </div>

        @if (isLoading()) {
          <div class="flex flex-col items-center justify-center py-16 gap-3">
            <i class="pi pi-spin pi-spinner text-2xl text-indigo-600"></i>
            <span class="text-sm font-medium text-surface-500">Loading departments…</span>
          </div>
        } @else if (filtered().length === 0) {
          <div class="flex flex-col items-center justify-center py-16 px-6 text-center">
            <div class="flex items-center justify-center w-12 h-12 bg-indigo-50 border border-indigo-100 rounded-full mb-4">
              <i class="pi pi-sitemap text-xl text-indigo-600"></i>
            </div>
            <p class="text-lg font-bold text-surface-900 m-0 mb-1">No departments yet</p>
            <p class="text-sm text-surface-500 m-0 mb-6 max-w-sm">Create your first department to start scoping document access.</p>
            <p-button label="New department" icon="pi pi-plus" (onClick)="openCreate()" styleClass="!bg-indigo-600 !border-indigo-600 !text-white hover:!bg-indigo-700 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm whitespace-nowrap">
              <thead class="bg-surface-50/50 border-b border-surface-200">
                <tr>
                  <th class="px-5 py-3 font-bold text-surface-700">Name</th>
                  <th class="px-5 py-3 font-bold text-surface-700">Description</th>
                  <th class="px-5 py-3 font-bold text-surface-700">Created</th>
                  <th class="px-5 py-3 font-bold text-surface-700 w-24"></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-surface-100">
                @for (dept of filtered(); track dept.id) {
                  <tr class="hover:bg-surface-50/50 transition-colors">
                    <td class="px-5 py-3.5">
                      <div class="flex items-center gap-3">
                        <span class="flex items-center justify-center w-8 h-8 rounded-full bg-indigo-100 text-indigo-700 font-bold text-xs">{{ dept.name.charAt(0) }}</span>
                        <span class="font-semibold text-surface-900">{{ dept.name }}</span>
                      </div>
                    </td>
                    <td class="px-5 py-3.5 text-surface-500 truncate max-w-xs">{{ dept.description || '—' }}</td>
                    <td class="px-5 py-3.5 text-surface-500">{{ dept.createdAt | date:'mediumDate' }}</td>
                    <td class="px-5 py-3.5">
                      <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 focus-within:opacity-100 transition-opacity [&:hover]:opacity-100" style="opacity: 1;">
                        <p-button icon="pi pi-pencil" (onClick)="openEdit(dept)" title="Edit" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-200 hover:!text-surface-700 transition-colors"></p-button>
                        <p-button icon="pi pi-trash" (onClick)="confirmDelete(dept)" title="Delete" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-red-50 hover:!text-red-600 transition-colors"></p-button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>

      <!-- Create / Edit Modal -->
      @if (showModal()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-surface-900/40 backdrop-blur-sm" (click)="closeModal()">
          <div class="w-full max-w-md bg-white rounded-xl shadow-2xl overflow-hidden flex flex-col" (click)="$event.stopPropagation()">
            <div class="flex items-center justify-between p-5 border-b border-surface-100">
              <h3 class="text-lg font-bold text-surface-900 m-0">{{ editTarget() ? 'Edit department' : 'New department' }}</h3>
              <p-button icon="pi pi-times" (onClick)="closeModal()" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-100 hover:!text-surface-700 rounded-full transition-colors"></p-button>
            </div>
            <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4 p-5">
              <label class="flex flex-col gap-1.5">
                <span class="text-sm font-semibold text-surface-700">Name <span class="text-red-500">*</span></span>
                <input
                  type="text"
                  formControlName="name"
                  class="w-full h-10 px-3 bg-white border border-surface-300 rounded-md text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all"
                  placeholder="e.g. Engineering"
                  id="dept-name"
                />
                @if (form.get('name')?.invalid && form.get('name')?.touched) {
                  <span class="text-xs font-medium text-red-500 mt-1">Department name is required.</span>
                }
              </label>
              <label class="flex flex-col gap-1.5">
                <span class="text-sm font-semibold text-surface-700">Description</span>
                <textarea
                  formControlName="description"
                  class="w-full p-3 bg-white border border-surface-300 rounded-md text-sm outline-none resize-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-all custom-scrollbar"
                  rows="3"
                  placeholder="Optional description…"
                  id="dept-description"
                ></textarea>
              </label>
              @if (formError()) {
                <div class="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700 mt-2">
                  <i class="pi pi-exclamation-circle mt-0.5"></i>
                  <span>{{ formError() }}</span>
                </div>
              }
              <div class="flex items-center justify-end gap-3 mt-4 pt-4 border-t border-surface-100">
                <p-button label="Cancel" (onClick)="closeModal()" styleClass="!bg-white !border-surface-300 !text-surface-700 hover:!bg-surface-50 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
                <p-button [label]="editTarget() ? 'Save changes' : 'Create department'" type="submit" [disabled]="form.invalid || isSaving()" [icon]="isSaving() ? 'pi pi-spin pi-spinner' : ''" styleClass="!bg-indigo-600 !border-indigo-600 !text-white hover:!bg-indigo-700 disabled:!bg-surface-300 disabled:!border-surface-300 disabled:!text-surface-500 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Delete Confirm Modal -->
      @if (deleteTarget()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-surface-900/40 backdrop-blur-sm" (click)="deleteTarget.set(null)">
          <div class="w-full max-w-sm bg-white rounded-xl shadow-2xl overflow-hidden flex flex-col" (click)="$event.stopPropagation()">
            <div class="flex items-center justify-between p-5 border-b border-surface-100">
              <h3 class="text-lg font-bold text-surface-900 m-0">Delete department</h3>
              <p-button icon="pi pi-times" (onClick)="deleteTarget.set(null)" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-100 hover:!text-surface-700 rounded-full transition-colors"></p-button>
            </div>
            <div class="p-5">
              <p class="text-[0.95rem] text-surface-700 leading-relaxed m-0 mb-4">
                Are you sure you want to delete <strong class="text-surface-900">{{ deleteTarget()?.name }}</strong>?
                This will remove the department from all associated documents and users.
              </p>
              @if (formError()) {
                <div class="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700 mb-4">
                  <i class="pi pi-exclamation-circle mt-0.5"></i>
                  <span>{{ formError() }}</span>
                </div>
              }
              <div class="flex items-center justify-end gap-3 pt-2">
                <p-button label="Cancel" (onClick)="deleteTarget.set(null)" styleClass="!bg-white !border-surface-300 !text-surface-700 hover:!bg-surface-50 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
                <p-button label="Delete" (onClick)="onDelete()" [disabled]="isSaving()" [icon]="isSaving() ? 'pi pi-spin pi-spinner' : ''" styleClass="!bg-red-600 !border-red-600 !text-white hover:!bg-red-700 disabled:!bg-surface-300 disabled:!border-surface-300 disabled:!text-surface-500 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
              </div>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styleUrl: './departments.css',
})
export class Departments implements OnInit {
  private deptService = inject(DepartmentService);
  private fb = inject(FormBuilder);

  departments = signal<Department[]>([]);
  isLoading = signal(true);
  isSaving = signal(false);
  showModal = signal(false);
  editTarget = signal<Department | null>(null);
  deleteTarget = signal<Department | null>(null);
  searchTerm = signal('');
  formError = signal('');

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
  });

  filtered = computed(() => {
    const q = this.searchTerm().toLowerCase().trim();
    if (!q) return this.departments();
    return this.departments().filter(d => d.name.toLowerCase().includes(q) || (d.description ?? '').toLowerCase().includes(q));
  });

  ngOnInit() {
    this.loadDepartments();
  }

  loadDepartments() {
    this.isLoading.set(true);
    this.deptService.getAll().subscribe({
      next: data => { this.departments.set(data ?? []); this.isLoading.set(false); },
      error: () => this.isLoading.set(false),
    });
  }

  openCreate() {
    this.editTarget.set(null);
    this.form.reset();
    this.formError.set('');
    this.showModal.set(true);
  }

  openEdit(dept: Department) {
    this.editTarget.set(dept);
    this.form.patchValue({ name: dept.name, description: dept.description ?? '' });
    this.formError.set('');
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editTarget.set(null);
    this.form.reset();
  }

  onSubmit() {
    if (this.form.invalid) return;
    this.isSaving.set(true);
    this.formError.set('');
    const { name, description } = this.form.value;
    const target = this.editTarget();

    const obs = target
      ? this.deptService.update(target.id, { name, description })
      : this.deptService.create({ name, description });

    obs.subscribe({
      next: () => { this.isSaving.set(false); this.closeModal(); this.loadDepartments(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Something went wrong. Please try again.');
      },
    });
  }

  confirmDelete(dept: Department) {
    this.deleteTarget.set(dept);
    this.formError.set('');
  }

  onDelete() {
    const target = this.deleteTarget();
    if (!target) return;
    this.isSaving.set(true);
    this.deptService.delete(target.id).subscribe({
      next: () => { this.isSaving.set(false); this.deleteTarget.set(null); this.loadDepartments(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Failed to delete department.');
      },
    });
  }
}
