import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { Department } from '../../../../core/services/department.service';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, DatePipe, ButtonModule],
  template: `
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
          @for (dept of departments(); track dept.id) {
            <tr class="hover:bg-surface-50/50 transition-colors group">
              <td class="px-5 py-3.5">
                <div class="flex items-center gap-3">
                  <span class="flex items-center justify-center w-8 h-8 rounded-lg bg-indigo-50 text-indigo-700 font-bold text-xs">{{ dept.name.charAt(0) }}</span>
                  <span class="font-semibold text-surface-900">{{ dept.name }}</span>
                </div>
              </td>
              <td class="px-5 py-3.5 text-surface-500 truncate max-w-xs">{{ dept.description || '—' }}</td>
              <td class="px-5 py-3.5 text-surface-500">{{ dept.createdAt | date:'mediumDate' }}</td>
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-1 sm:opacity-0 sm:group-hover:opacity-100 focus-within:opacity-100 transition-opacity">
                  <p-button icon="pi pi-pencil" (onClick)="edit.emit(dept)" title="Edit" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-200 hover:!text-surface-700 transition-colors rounded-lg"></p-button>
                  <p-button icon="pi pi-trash" (onClick)="delete.emit(dept)" title="Delete" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-red-50 hover:!text-red-600 transition-colors rounded-lg"></p-button>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class DepartmentListComponent {
  departments = input.required<Department[]>();
  edit = output<Department>();
  delete = output<Department>();
}
