import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserGroupService, UserGroup, UserGroupDetail } from '../../../core/services/user-group.service';
import { DepartmentService, Department } from '../../../core/services/department.service';

@Component({
  selector: 'app-user-groups',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="page-wrap">
      <!-- Header -->
      <header class="page-header">
        <div>
          <p class="page-eyebrow">Organisation</p>
          <h1 class="page-title">User Groups</h1>
          <p class="page-subtitle">Group employees together and assign them to departments in bulk.</p>
        </div>
        <button type="button" class="btn-primary" (click)="openCreate()">
          <i class="pi pi-plus"></i> New group
        </button>
      </header>

      <!-- Stats -->
      <div class="stats-row">
        <div class="stat-card">
          <p class="stat-label">Total groups</p>
          <strong class="stat-value">{{ groups().length }}</strong>
        </div>
      </div>

      <!-- List -->
      <section class="card">
        <div class="card-header">
          <h2 class="card-title">All user groups</h2>
          <input
            type="search"
            class="search-input"
            placeholder="Search groups…"
            [value]="searchTerm()"
            (input)="searchTerm.set($any($event.target).value)"
          />
        </div>

        @if (isLoading()) {
          <div class="loading-state">
            <i class="pi pi-spin pi-spinner"></i>
            <span>Loading groups…</span>
          </div>
        } @else if (filtered().length === 0) {
          <div class="empty-state">
            <i class="pi pi-users empty-icon"></i>
            <p class="empty-title">No user groups yet</p>
            <p class="empty-sub">Create groups to assign multiple employees to departments at once.</p>
            <button type="button" class="btn-primary" (click)="openCreate()">
              <i class="pi pi-plus"></i> New group
            </button>
          </div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Members</th>
                <th>Departments</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (group of filtered(); track group.id) {
                <tr>
                  <td>
                    <div class="group-name">
                      <span class="group-avatar">{{ group.name.charAt(0) }}</span>
                      {{ group.name }}
                    </div>
                  </td>
                  <td class="text-muted">{{ group.description || '—' }}</td>
                  <td>
                    <span class="badge badge-blue">{{ group.memberCount }} members</span>
                  </td>
                  <td>
                    <span class="badge badge-purple">{{ group.departmentCount }} depts</span>
                  </td>
                  <td class="text-muted">{{ group.createdAt | date:'mediumDate' }}</td>
                  <td class="action-cell">
                    <button type="button" class="icon-btn" title="Manage assignments" (click)="openAssignments(group)">
                      <i class="pi pi-sliders-h"></i>
                    </button>
                    <button type="button" class="icon-btn" title="Edit" (click)="openEdit(group)">
                      <i class="pi pi-pencil"></i>
                    </button>
                    <button type="button" class="icon-btn danger" title="Delete" (click)="confirmDelete(group)">
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
              <h3>{{ editTarget() ? 'Edit group' : 'New user group' }}</h3>
              <button type="button" class="icon-btn" (click)="closeModal()"><i class="pi pi-times"></i></button>
            </div>
            <form [formGroup]="form" (ngSubmit)="onSubmit()" class="modal-body">
              <label class="field">
                <span class="field-label">Name <span class="required">*</span></span>
                <input type="text" formControlName="name" class="field-input"
                  placeholder="e.g. Engineering Team" id="group-name" />
                @if (form.get('name')?.invalid && form.get('name')?.touched) {
                  <span class="field-error">Group name is required.</span>
                }
              </label>
              <label class="field">
                <span class="field-label">Description</span>
                <textarea formControlName="description" class="field-input" rows="3"
                  placeholder="Optional description…" id="group-description"></textarea>
              </label>
              @if (formError()) { <div class="alert-error">{{ formError() }}</div> }
              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="closeModal()">Cancel</button>
                <button type="submit" class="btn-primary" [disabled]="form.invalid || isSaving()">
                  @if (isSaving()) { <i class="pi pi-spin pi-spinner"></i> }
                  {{ editTarget() ? 'Save changes' : 'Create group' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }

      <!-- Assignments Modal -->
      @if (showAssignmentsModal()) {
        <div class="modal-backdrop" (click)="closeAssignmentsModal()">
          <div class="modal modal-lg" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Manage assignments — {{ assignmentTarget()?.name }}</h3>
              <button type="button" class="icon-btn" (click)="closeAssignmentsModal()"><i class="pi pi-times"></i></button>
            </div>
            <div class="modal-body">
              @if (loadingDetail()) {
                <div class="loading-state"><i class="pi pi-spin pi-spinner"></i> Loading…</div>
              } @else {
                <div class="assignments-grid">
                  <!-- Departments -->
                  <div class="assign-section">
                    <p class="assign-section-title"><i class="pi pi-sitemap"></i> Departments</p>
                    <p class="assign-section-sub">Members of this group will have access to documents in these departments.</p>
                    <div class="checklist">
                      @for (dept of allDepartments(); track dept.id) {
                        <label class="check-item">
                          <input type="checkbox"
                            [checked]="selectedDeptIds().includes(dept.id)"
                            (change)="toggleDept(dept.id)"
                          />
                          <span class="check-label">{{ dept.name }}</span>
                        </label>
                      }
                      @if (allDepartments().length === 0) {
                        <p class="text-muted-sm">No departments created yet.</p>
                      }
                    </div>
                  </div>
                </div>
              }
              @if (formError()) { <div class="alert-error">{{ formError() }}</div> }
            </div>
            <div class="modal-footer">
              <button type="button" class="btn-secondary" (click)="closeAssignmentsModal()">Cancel</button>
              <button type="button" class="btn-primary" (click)="onSaveAssignments()" [disabled]="isSaving() || loadingDetail()">
                @if (isSaving()) { <i class="pi pi-spin pi-spinner"></i> }
                Save assignments
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Delete Confirm Modal -->
      @if (deleteTarget()) {
        <div class="modal-backdrop" (click)="deleteTarget.set(null)">
          <div class="modal modal-sm" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Delete group</h3>
              <button type="button" class="icon-btn" (click)="deleteTarget.set(null)"><i class="pi pi-times"></i></button>
            </div>
            <div class="modal-body">
              <p class="confirm-text">
                Are you sure you want to delete <strong>{{ deleteTarget()?.name }}</strong>?
                Members will lose access granted through this group.
              </p>
              @if (formError()) { <div class="alert-error">{{ formError() }}</div> }
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
  styleUrl: './user-groups.css',
})
export class UserGroups implements OnInit {
  private groupService = inject(UserGroupService);
  private deptService = inject(DepartmentService);
  private fb = inject(FormBuilder);

  groups = signal<UserGroup[]>([]);
  allDepartments = signal<Department[]>([]);
  isLoading = signal(true);
  isSaving = signal(false);
  loadingDetail = signal(false);

  showModal = signal(false);
  editTarget = signal<UserGroup | null>(null);
  deleteTarget = signal<UserGroup | null>(null);

  showAssignmentsModal = signal(false);
  assignmentTarget = signal<UserGroup | null>(null);
  assignmentDetail = signal<UserGroupDetail | null>(null);
  selectedDeptIds = signal<string[]>([]);

  searchTerm = signal('');
  formError = signal('');

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
  });

  filtered = computed(() => {
    const q = this.searchTerm().toLowerCase().trim();
    if (!q) return this.groups();
    return this.groups().filter(g =>
      g.name.toLowerCase().includes(q) || (g.description ?? '').toLowerCase().includes(q)
    );
  });

  ngOnInit() {
    this.loadGroups();
    this.deptService.getAll().subscribe({ next: data => this.allDepartments.set(data ?? []) });
  }

  loadGroups() {
    this.isLoading.set(true);
    this.groupService.getAll().subscribe({
      next: data => { this.groups.set(data ?? []); this.isLoading.set(false); },
      error: () => this.isLoading.set(false),
    });
  }

  openCreate() {
    this.editTarget.set(null);
    this.form.reset();
    this.formError.set('');
    this.showModal.set(true);
  }

  openEdit(group: UserGroup) {
    this.editTarget.set(group);
    this.form.patchValue({ name: group.name, description: group.description ?? '' });
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
      ? this.groupService.update(target.id, { name, description })
      : this.groupService.create({ name, description });

    obs.subscribe({
      next: () => { this.isSaving.set(false); this.closeModal(); this.loadGroups(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Something went wrong.');
      },
    });
  }

  openAssignments(group: UserGroup) {
    this.assignmentTarget.set(group);
    this.formError.set('');
    this.loadingDetail.set(true);
    this.showAssignmentsModal.set(true);
    this.groupService.getById(group.id).subscribe({
      next: detail => {
        this.assignmentDetail.set(detail);
        this.selectedDeptIds.set([...(detail.departmentIds ?? [])]);
        this.loadingDetail.set(false);
      },
      error: () => this.loadingDetail.set(false),
    });
  }

  closeAssignmentsModal() {
    this.showAssignmentsModal.set(false);
    this.assignmentTarget.set(null);
    this.assignmentDetail.set(null);
    this.selectedDeptIds.set([]);
  }

  toggleDept(id: string) {
    const current = this.selectedDeptIds();
    if (current.includes(id)) {
      this.selectedDeptIds.set(current.filter(x => x !== id));
    } else {
      this.selectedDeptIds.set([...current, id]);
    }
  }

  onSaveAssignments() {
    const target = this.assignmentTarget();
    const detail = this.assignmentDetail();
    if (!target) return;
    this.isSaving.set(true);
    this.formError.set('');
    this.groupService.updateAssignments(target.id, {
      memberUserIds: detail?.memberUserIds ?? [],
      departmentIds: this.selectedDeptIds(),
    }).subscribe({
      next: () => { this.isSaving.set(false); this.closeAssignmentsModal(); this.loadGroups(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Failed to save assignments.');
      },
    });
  }

  confirmDelete(group: UserGroup) {
    this.deleteTarget.set(group);
    this.formError.set('');
  }

  onDelete() {
    const target = this.deleteTarget();
    if (!target) return;
    this.isSaving.set(true);
    this.groupService.delete(target.id).subscribe({
      next: () => { this.isSaving.set(false); this.deleteTarget.set(null); this.loadGroups(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Failed to delete group.');
      },
    });
  }
}
