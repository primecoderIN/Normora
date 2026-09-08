import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DepartmentService, Department } from '../../../core/services/department.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { DepartmentListComponent } from './components/department-list.component';
import { DepartmentFormComponent } from './components/department-form.component';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, ConfirmDialogComponent, DepartmentListComponent, DepartmentFormComponent, SkeletonModule],
  templateUrl: './departments.html',
  styleUrl: './departments.css',
})
export class Departments implements OnInit {
  private deptService = inject(DepartmentService);

  departments = signal<Department[]>([]);
  isLoading = signal(true);
  isSaving = signal(false);

  showModal = signal(false);
  editTarget = signal<Department | null>(null);
  deleteTarget = signal<Department | null>(null);

  searchTerm = signal('');
  formError = signal('');

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
    this.formError.set('');
    this.showModal.set(true);
  }

  openEdit(dept: Department) {
    this.editTarget.set(dept);
    this.formError.set('');
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.editTarget.set(null);
    this.formError.set('');
  }

  onSave(payload: { name: string; description: string }) {
    this.isSaving.set(true);
    this.formError.set('');
    const target = this.editTarget();

    const obs = target
      ? this.deptService.update(target.id, payload)
      : this.deptService.create(payload);

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
