import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { FormErrorComponent } from '@shared/components/form-error/form-error.component';
import { UserGroup, UserGroupDetail } from '@core/services/user-group.service';
import { Department } from '@core/services/department.service';

@Component({
  selector: 'app-user-group-assignments',
  standalone: true,
  imports: [CommonModule, ButtonModule, FormErrorComponent],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/40 backdrop-blur-sm" (click)="onCancel()">
      <div class="w-full max-w-4xl bg-white rounded-xl shadow-2xl overflow-hidden flex flex-col" (click)="$event.stopPropagation()">
        <div class="flex items-center justify-between p-5 border-b border-slate-100">
          <h3 class="text-lg font-bold text-slate-900 m-0">Manage assignments — {{ userGroup()?.name }}</h3>
          <p-button icon="pi pi-times" (onClick)="onCancel()" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-slate-400 hover:!bg-slate-100 hover:!text-slate-700 rounded-full transition-colors"></p-button>
        </div>
        <div class="p-5 overflow-y-auto max-h-[70vh] custom-scrollbar">
          @if (isLoadingDetail()) {
            <div class="flex items-center justify-center py-8 gap-3">
              <div class="w-8 h-8 border-3 border-primary-100 border-t-indigo-600 rounded-full animate-spin"></div>
              <span class="text-sm font-medium text-slate-500">Loading assignments…</span>
            </div>
          } @else {
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              
              <!-- Members Section -->
              <div class="bg-slate-50 rounded-lg p-4 border border-slate-200">
                <h4 class="text-sm font-bold text-slate-900 mb-1 flex items-center gap-2">
                  <i class="pi pi-users text-primary-600"></i> Members
                </h4>
                <p class="text-sm text-slate-500 mb-4">Select employees to add to this group.</p>
                
                <div class="grid grid-cols-1 gap-2 max-h-64 overflow-y-auto custom-scrollbar pr-1">
                  @for (emp of allEmployees(); track emp.userId) {
                    <label class="flex items-center gap-3 p-3 bg-white border border-slate-200 rounded-lg cursor-pointer hover:bg-primary-50/50 hover:border-primary-200 transition-colors">
                      <input
                        type="checkbox"
                        [checked]="selectedUserIds().includes(emp.userId)"
                        (change)="toggleUser(emp.userId)"
                        class="w-4 h-4 text-primary-600 border-slate-300 rounded focus:ring-primary-500 accent-indigo-600 cursor-pointer flex-none"
                      />
                      <div class="flex items-center gap-3 min-w-0">
                        <div class="flex-none w-8 h-8 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center font-bold text-xs">
                          {{ emp.displayName ? emp.displayName.charAt(0).toUpperCase() : (emp.email ? emp.email.charAt(0).toUpperCase() : '?') }}
                        </div>
                        <div class="truncate">
                          <p class="font-semibold text-slate-900 text-sm m-0 truncate">{{ emp.displayName || '—' }}</p>
                          <p class="text-slate-500 text-xs m-0 mt-0.5 truncate">{{ emp.email }}</p>
                        </div>
                      </div>
                    </label>
                  }
                  @if (allEmployees().length === 0) {
                    <p class="text-sm text-slate-500 italic col-span-full">No employees available.</p>
                  }
                </div>
              </div>

              <!-- Departments Section -->
              <div class="bg-slate-50 rounded-lg p-4 border border-slate-200">
                <h4 class="text-sm font-bold text-slate-900 mb-1 flex items-center gap-2">
                  <i class="pi pi-sitemap text-primary-600"></i> Departments
                </h4>
                <p class="text-sm text-slate-500 mb-4">Members of this group will have access to documents in these departments.</p>
                
                <div class="grid grid-cols-1 gap-2 max-h-64 overflow-y-auto custom-scrollbar pr-1">
                  @for (dept of allDepartments(); track dept.id) {
                    <label class="flex items-center gap-3 p-3 bg-white border border-slate-200 rounded-lg cursor-pointer hover:bg-primary-50/50 hover:border-primary-200 transition-colors">
                      <input
                        type="checkbox"
                        [checked]="selectedDeptIds().includes(dept.id)"
                        (change)="toggleDept(dept.id)"
                        class="w-4 h-4 text-primary-600 border-slate-300 rounded focus:ring-primary-500 accent-indigo-600 cursor-pointer"
                      />
                      <span class="text-sm font-medium text-slate-700">{{ dept.name }}</span>
                    </label>
                  }
                  @if (allDepartments().length === 0) {
                    <p class="text-sm text-slate-500 italic col-span-full">No departments created yet.</p>
                  }
                </div>
              </div>

            </div>
          }
          
          <app-form-error [error]="error()"></app-form-error>
        </div>
        
        <div class="flex items-center justify-end gap-3 p-5 border-t border-slate-100 bg-slate-50">
          <p-button label="Cancel" (onClick)="onCancel()" styleClass="!bg-white !border-slate-300 !text-slate-700 hover:!bg-slate-100 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
          <p-button label="Save assignments" (onClick)="onSave()" [disabled]="isSaving() || isLoadingDetail()" [icon]="isSaving() ? 'pi pi-spin pi-spinner' : ''" styleClass="!bg-primary-600 !border-primary-600 !text-white hover:!bg-primary-700 disabled:!bg-slate-300 disabled:!border-slate-300 disabled:!text-slate-500 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
        </div>
      </div>
    </div>
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

