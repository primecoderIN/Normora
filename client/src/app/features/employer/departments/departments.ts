import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DepartmentService, Department } from '../../../core/services/department.service';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-wrap">
      <!-- Header -->
      <header class="page-header">
        <div>
          <p class="page-eyebrow">Organisation</p>
          <h1 class="page-title">Departments</h1>
          <p class="page-subtitle">Create departments to scope document access for your employees.</p>
        </div>
        <button type="button" class="btn-primary" (click)="openCreate()">
          <i class="pi pi-plus"></i> New department
        </button>
      </header>

      <!-- Stats -->
      <div class="stats-row">
        <div class="stat-card">
          <p class="stat-label">Total departments</p>
          <strong class="stat-value">{{ departments().length }}</strong>
        </div>
      </div>

      <!-- List -->
      <section class="card">
        <div class="card-header">
          <h2 class="card-title">All departments</h2>
          <input
            type="search"
            class="search-input"
            placeholder="Search departments…"
            [value]="searchTerm()"
            (input)="searchTerm.set($any($event.target).value)"
          />
        </div>

        @if (isLoading()) {
          <div class="loading-state">
            <i class="pi pi-spin pi-spinner"></i>
            <span>Loading departments…</span>
          </div>
        } @else if (filtered().length === 0) {
          <div class="empty-state">
            <i class="pi pi-sitemap empty-icon"></i>
            <p class="empty-title">No departments yet</p>
            <p class="empty-sub">Create your first department to start scoping document access.</p>
            <button type="button" class="btn-primary" (click)="openCreate()">
              <i class="pi pi-plus"></i> New department
            </button>
          </div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (dept of filtered(); track dept.id) {
                <tr>
                  <td>
                    <div class="dept-name">
                      <span class="dept-avatar">{{ dept.name.charAt(0) }}</span>
                      {{ dept.name }}
                    </div>
                  </td>
                  <td class="text-muted">{{ dept.description || '—' }}</td>
                  <td class="text-muted">{{ dept.createdAt | date:'mediumDate' }}</td>
                  <td class="action-cell">
                    <button type="button" class="icon-btn" title="Edit" (click)="openEdit(dept)">
                      <i class="pi pi-pencil"></i>
                    </button>
                    <button type="button" class="icon-btn danger" title="Delete" (click)="confirmDelete(dept)">
                      <i class="pi pi-trash"></i>
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      </section>

      <!-- Create / Edit Modal -->
      @if (showModal()) {
        <div class="modal-backdrop" (click)="closeModal()">
          <div class="modal" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>{{ editTarget() ? 'Edit department' : 'New department' }}</h3>
              <button type="button" class="icon-btn" (click)="closeModal()">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <form [formGroup]="form" (ngSubmit)="onSubmit()" class="modal-body">
              <label class="field">
                <span class="field-label">Name <span class="required">*</span></span>
                <input
                  type="text"
                  formControlName="name"
                  class="field-input"
                  placeholder="e.g. Engineering"
                  id="dept-name"
                />
                @if (form.get('name')?.invalid && form.get('name')?.touched) {
                  <span class="field-error">Department name is required.</span>
                }
              </label>
              <label class="field">
                <span class="field-label">Description</span>
                <textarea
                  formControlName="description"
                  class="field-input"
                  rows="3"
                  placeholder="Optional description…"
                  id="dept-description"
                ></textarea>
              </label>
              @if (formError()) {
                <div class="alert-error">{{ formError() }}</div>
              }
              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn-primary" [disabled]="form.invalid || isSaving()">
                  @if (isSaving()) { <i class="pi pi-spin pi-spinner"></i> }
                  {{ editTarget() ? 'Save changes' : 'Create department' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Delete Confirm Modal -->
      @if (deleteTarget()) {
        <div class="modal-backdrop" (click)="deleteTarget.set(null)">
          <div class="modal modal-sm" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Delete department</h3>
              <button type="button" class="icon-btn" (click)="deleteTarget.set(null)">
                <i class="pi pi-times"></i>
              </button>
            </div>
            <div class="modal-body">
              <p class="confirm-text">
                Are you sure you want to delete <strong>{{ deleteTarget()?.name }}</strong>?
                This will remove the department from all associated documents and users.
              </p>
              @if (formError()) {
                <div class="alert-error">{{ formError() }}</div>
              }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn-secondary" (click)="deleteTarget.set(null)">Cancel</button>
              <button type="button" class="btn-danger" (click)="onDelete()" [disabled]="isSaving()">
                @if (isSaving()) { <i class="pi pi-spin pi-spinner"></i> }
                Delete
              </button>
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
