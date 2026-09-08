import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserGroupService, UserGroup, UserGroupDetail } from '../../../core/services/user-group.service';
import { DepartmentService, Department } from '../../../core/services/department.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { UserGroupListComponent } from './components/user-group-list.component';
import { UserGroupFormComponent } from './components/user-group-form.component';
import { UserGroupAssignmentsComponent } from './components/user-group-assignments.component';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-user-groups',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, ConfirmDialogComponent, UserGroupListComponent, UserGroupFormComponent, UserGroupAssignmentsComponent, SkeletonModule],
  templateUrl: './user-groups.html',
})
export class UserGroups implements OnInit {
  private groupService = inject(UserGroupService);
  private deptService = inject(DepartmentService);

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

  searchTerm = signal('');
  formError = signal('');

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
    this.formError.set('');
    this.showModal.set(true);
  }

  openEdit(group: UserGroup) {
    this.editTarget.set(group);
    this.formError.set('');
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editTarget.set(null);
    this.formError.set('');
  }

  onSaveForm(payload: { name: string; description: string }) {
    this.isSaving.set(true);
    this.formError.set('');
    const target = this.editTarget();

    const obs = target
      ? this.groupService.update(target.id, payload)
      : this.groupService.create(payload);

    obs.subscribe({
      next: () => { this.isSaving.set(false); this.closeModal(); this.loadGroups(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Something went wrong.');
      },
    });
  }

  // --- Assignments ---

  async openAssignments(group: UserGroup) {
    this.assignmentTarget.set(group);
    this.formError.set('');
    this.loadingDetail.set(true);
    this.showAssignmentsModal.set(true);

    this.groupService.getById(group.id).subscribe({
      next: detail => {
        this.assignmentDetail.set(detail);
        this.loadingDetail.set(false);
      },
      error: () => this.loadingDetail.set(false),
    });
  }

  closeAssignmentsModal() {
    this.showAssignmentsModal.set(false);
    this.assignmentTarget.set(null);
    this.assignmentDetail.set(null);
    this.formError.set('');
  }

  onSaveAssignments(payload: { memberUserIds: string[]; departmentIds: string[] }) {
    const target = this.assignmentTarget();
    if (!target) return;
    this.isSaving.set(true);
    this.formError.set('');

    this.groupService.updateAssignments(target.id, payload).subscribe({
      next: () => { this.isSaving.set(false); this.closeAssignmentsModal(); this.loadGroups(); },
      error: (err: any) => {
        this.isSaving.set(false);
        this.formError.set(err?.error?.message ?? 'Failed to save assignments.');
      },
    });
  }

  // --- Delete ---

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
