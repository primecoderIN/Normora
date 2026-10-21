import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DsButtonComponent, DsDialogComponent } from '@design-system';
import { FormErrorComponent } from '@shared/components/form-error/form-error.component';
import { UserGroup, UserGroupDetail } from '@core/services/user-group.service';
import { Department } from '@core/services/department.service';

/**
 * Modal dialog component for assigning members and departments to a user group.
 */
@Component({
  selector: 'app-user-group-assignments',
  standalone: true,
  imports: [CommonModule, DsButtonComponent, DsDialogComponent, FormErrorComponent],
  template: `
    <ds-dialog
      [visible]="true"
      (visibleChange)="onCancel()"
      [title]="'Manage assignments — ' + (userGroup()?.name || '')"
      maxWidthClass="max-w-4xl"
    >
      <div class="p-1">
          @if (isLoadingDetail()) {
            <div class="flex items-center justify-center py-8 gap-3">
              <div class="w-8 h-8 border-3 border-primary-100 border-t-indigo-600 rounded-full animate-spin"></div>
              <span class="text-sm font-medium text-slate-500 dark:text-slate-400">Loading assignments…</span>
            </div>
          } @else {
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              
              <!-- Members Section -->
              <div class="bg-slate-50 dark:bg-slate-950 rounded-lg p-4 border border-slate-200 dark:border-slate-800">
                <h4 class="text-sm font-bold text-slate-900 dark:text-white mb-1 flex items-center gap-2">
                  <i class="pi pi-users text-primary-600 dark:text-primary-400"></i> Members
                </h4>
                <p class="text-sm text-slate-500 dark:text-slate-400 mb-4">Select employees to add to this group.</p>
                
                <div class="grid grid-cols-1 gap-2 max-h-64 overflow-y-auto custom-scrollbar pr-1">
                  @for (emp of allEmployees(); track emp.userId) {
                    <label class="flex items-center gap-3 p-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg cursor-pointer hover:bg-primary-50/50 dark:hover:bg-slate-800 hover:border-primary-200 dark:hover:border-slate-700 transition-colors">
                      <input
                        type="checkbox"
                        [checked]="selectedUserIds().includes(emp.userId)"
                        (change)="toggleUser(emp.userId)"
                        class="w-4 h-4 text-primary-600 border-slate-300 dark:border-slate-700 rounded focus:ring-primary-500 accent-indigo-600 cursor-pointer flex-none"
                      />
                      <div class="flex items-center gap-3 min-w-0">
                        <div class="flex-none w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-950/70 text-primary-700 dark:text-primary-300 border border-primary-200 dark:border-primary-800/50 flex items-center justify-center font-bold text-xs">
                          {{ emp.displayName ? emp.displayName.charAt(0).toUpperCase() : (emp.email ? emp.email.charAt(0).toUpperCase() : '?') }}
                        </div>
                        <div class="truncate">
                          <p class="font-semibold text-slate-900 dark:text-white text-sm m-0 truncate">{{ emp.displayName || '—' }}</p>
                          <p class="text-slate-500 dark:text-slate-400 text-xs m-0 mt-0.5 truncate">{{ emp.email }}</p>
                        </div>
                      </div>
                    </label>
                  }
                  @if (allEmployees().length === 0) {
                    <p class="text-sm text-slate-500 dark:text-slate-400 italic col-span-full">No employees available.</p>
                  }
                </div>
              </div>

              <!-- Departments Section -->
              <div class="bg-slate-50 dark:bg-slate-950 rounded-lg p-4 border border-slate-200 dark:border-slate-800">
                <h4 class="text-sm font-bold text-slate-900 dark:text-white mb-1 flex items-center gap-2">
                  <i class="pi pi-sitemap text-primary-600 dark:text-primary-400"></i> Departments
                </h4>
                <p class="text-sm text-slate-500 dark:text-slate-400 mb-4">Members of this group will have access to documents in these departments.</p>
                
                <div class="grid grid-cols-1 gap-2 max-h-64 overflow-y-auto custom-scrollbar pr-1">
                  @for (dept of allDepartments(); track dept.id) {
                    <label class="flex items-center gap-3 p-3 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg cursor-pointer hover:bg-primary-50/50 dark:hover:bg-slate-800 hover:border-primary-200 dark:hover:border-slate-700 transition-colors">
                      <input
                        type="checkbox"
                        [checked]="selectedDeptIds().includes(dept.id)"
                        (change)="toggleDept(dept.id)"
                        class="w-4 h-4 text-primary-600 border-slate-300 dark:border-slate-700 rounded focus:ring-primary-500 accent-indigo-600 cursor-pointer"
                      />
                      <span class="text-sm font-medium text-slate-700 dark:text-slate-200">{{ dept.name }}</span>
                    </label>
                  }
                  @if (allDepartments().length === 0) {
                    <p class="text-sm text-slate-500 dark:text-slate-400 italic col-span-full">No departments created yet.</p>
                  }
                </div>
              </div>

            </div>
          }
          
          <app-form-error [error]="error()"></app-form-error>
        </div>
        
        <div class="flex items-center justify-end gap-3 mt-4 pt-4 border-t border-slate-100 dark:border-slate-800">
          <ds-button variant="outline" size="sm" (clicked)="onCancel()">Cancel</ds-button>
          <ds-button
            variant="primary"
            size="sm"
            (clicked)="onSave()"
            [disabled]="isSaving() || isLoadingDetail()"
            [loading]="isSaving()"
          >
            Save assignments
          </ds-button>
        </div>
    </ds-dialog>
  `
})
export class UserGroupAssignmentsComponent {
  userGroup = input.required<UserGroup>();
  detail = input<UserGroupDetail | null>(null);
  allDepartments = input.required<Department[]>();
  allEmployees = input<any[]>([]);
  
  isLoadingDetail = input<boolean>(true);
  isSaving = input<boolean>(false);
  error = input<string | null | undefined>('');
  
  save = output<{ memberUserIds: string[]; departmentIds: string[] }>();
  cancel = output<void>();

  selectedDeptIds = signal<string[]>([]);
  selectedUserIds = signal<string[]>([]);

  constructor() {
    effect(() => {
      const d = this.detail();
      if (d) {
        this.selectedDeptIds.set([...(d.departmentIds ?? [])]);
        this.selectedUserIds.set([...(d.memberUserIds ?? [])]);
      } else {
        this.selectedDeptIds.set([]);
        this.selectedUserIds.set([]);
      }
    }, { allowSignalWrites: true });
  }

  toggleDept(id: string) {
    const current = this.selectedDeptIds();
    if (current.includes(id)) {
      this.selectedDeptIds.set(current.filter(x => x !== id));
    } else {
      this.selectedDeptIds.set([...current, id]);
    }
  }

  toggleUser(id: string) {
    const current = this.selectedUserIds();
    if (current.includes(id)) {
      this.selectedUserIds.set(current.filter(x => x !== id));
    } else {
      this.selectedUserIds.set([...current, id]);
    }
  }

  onSave() {
    this.save.emit({
      memberUserIds: this.selectedUserIds(),
      departmentIds: this.selectedDeptIds()
    });
  }

  onCancel() {
    this.cancel.emit();
  }
}

