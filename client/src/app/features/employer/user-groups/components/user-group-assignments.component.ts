import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { FormErrorComponent } from '../../../../shared/components/form-error/form-error.component';
import { UserGroup, UserGroupDetail } from '../../../../core/services/user-group.service';
import { Department } from '../../../../core/services/department.service';

@Component({
  selector: 'app-user-group-assignments',
  standalone: true,
  imports: [CommonModule, ButtonModule, FormErrorComponent],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-surface-900/40 backdrop-blur-sm" (click)="onCancel()">
      <div class="w-full max-w-2xl bg-white rounded-xl shadow-2xl overflow-hidden flex flex-col" (click)="$event.stopPropagation()">
        <div class="flex items-center justify-between p-5 border-b border-surface-100">
          <h3 class="text-lg font-bold text-surface-900 m-0">Manage assignments — {{ userGroup()?.name }}</h3>
          <p-button icon="pi pi-times" (onClick)="onCancel()" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-100 hover:!text-surface-700 rounded-full transition-colors"></p-button>
        </div>
        <div class="p-5 overflow-y-auto max-h-[70vh] custom-scrollbar">
          @if (isLoadingDetail()) {
            <div class="flex items-center justify-center py-8 gap-3">
              <div class="w-8 h-8 border-3 border-indigo-100 border-t-indigo-600 rounded-full animate-spin"></div>
              <span class="text-sm font-medium text-surface-500">Loading assignments…</span>
            </div>
          } @else {
            <div class="grid gap-6">
              <div class="bg-surface-50 rounded-lg p-4 border border-surface-200">
                <h4 class="text-sm font-bold text-surface-900 mb-1 flex items-center gap-2">
                  <i class="pi pi-sitemap text-indigo-600"></i> Departments
                </h4>
                <p class="text-sm text-surface-500 mb-4">Members of this group will have access to documents in these departments.</p>
                
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 max-h-64 overflow-y-auto custom-scrollbar pr-1">
                  @for (dept of allDepartments(); track dept.id) {
                    <label class="flex items-center gap-3 p-3 bg-white border border-surface-200 rounded-lg cursor-pointer hover:bg-indigo-50/50 hover:border-indigo-200 transition-colors">
                      <input
                        type="checkbox"
                        [checked]="selectedDeptIds().includes(dept.id)"
                        (change)="toggleDept(dept.id)"
                        class="w-4 h-4 text-indigo-600 border-surface-300 rounded focus:ring-indigo-500 accent-indigo-600 cursor-pointer"
                      />
                      <span class="text-sm font-medium text-surface-700">{{ dept.name }}</span>
                    </label>
                  }
                  @if (allDepartments().length === 0) {
                    <p class="text-sm text-surface-500 italic col-span-full">No departments created yet.</p>
                  }
                </div>
              </div>
            </div>
          }
          
          <app-form-error [error]="error()"></app-form-error>
        </div>
        
        <div class="flex items-center justify-end gap-3 p-5 border-t border-surface-100 bg-surface-50">
          <p-button label="Cancel" (onClick)="onCancel()" styleClass="!bg-white !border-surface-300 !text-surface-700 hover:!bg-surface-100 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
          <p-button label="Save assignments" (onClick)="onSave()" [disabled]="isSaving() || isLoadingDetail()" [icon]="isSaving() ? 'pi pi-spin pi-spinner' : ''" styleClass="!bg-indigo-600 !border-indigo-600 !text-white hover:!bg-indigo-700 disabled:!bg-surface-300 disabled:!border-surface-300 disabled:!text-surface-500 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
        </div>
      </div>
    </div>
  `
})
export class UserGroupAssignmentsComponent {
  userGroup = input.required<UserGroup>();
  detail = input<UserGroupDetail | null>(null);
  allDepartments = input.required<Department[]>();
  
  isLoadingDetail = input<boolean>(true);
  isSaving = input<boolean>(false);
  error = input<string | null | undefined>('');
  
  save = output<{ memberUserIds: string[]; departmentIds: string[] }>();
  cancel = output<void>();

  selectedDeptIds = signal<string[]>([]);

  constructor() {
    effect(() => {
      const d = this.detail();
      if (d) {
        this.selectedDeptIds.set([...(d.departmentIds ?? [])]);
      } else {
        this.selectedDeptIds.set([]);
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

  onSave() {
    this.save.emit({
      memberUserIds: this.detail()?.memberUserIds ?? [],
      departmentIds: this.selectedDeptIds()
    });
  }

  onCancel() {
    this.cancel.emit();
  }
}
