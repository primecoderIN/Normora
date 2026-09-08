import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { UserGroup } from '../../../../core/services/user-group.service';

@Component({
  selector: 'app-user-group-list',
  standalone: true,
  imports: [CommonModule, DatePipe, ButtonModule],
  template: `
    <div class="overflow-x-auto">
      <table class="w-full text-left text-sm whitespace-nowrap">
        <thead class="bg-surface-50/50 border-b border-surface-200">
          <tr>
            <th class="px-5 py-3 font-bold text-surface-700">Name</th>
            <th class="px-5 py-3 font-bold text-surface-700">Description</th>
            <th class="px-5 py-3 font-bold text-surface-700">Members</th>
            <th class="px-5 py-3 font-bold text-surface-700">Departments</th>
            <th class="px-5 py-3 font-bold text-surface-700">Created</th>
            <th class="px-5 py-3 font-bold text-surface-700 w-32"></th>
          </tr>
        </thead>
        <tbody class="divide-y divide-surface-100">
          @for (group of groups(); track group.id) {
            <tr class="hover:bg-surface-50/50 transition-colors group/row">
              <td class="px-5 py-3.5">
                <div class="flex items-center gap-3 font-semibold text-surface-900">
                  <span class="flex items-center justify-center w-8 h-8 rounded-lg bg-linear-to-br from-indigo-500 to-purple-500 text-white font-bold text-xs uppercase">{{ group.name.charAt(0) }}</span>
                  {{ group.name }}
                </div>
              </td>
              <td class="px-5 py-3.5 text-surface-500 truncate max-w-xs">{{ group.description || '—' }}</td>
              <td class="px-5 py-3.5">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-50 text-blue-600">{{ group.memberCount }} members</span>
              </td>
              <td class="px-5 py-3.5">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-purple-50 text-purple-600">{{ group.departmentCount }} depts</span>
              </td>
              <td class="px-5 py-3.5 text-surface-500">{{ group.createdAt | date:'mediumDate' }}</td>
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-1 sm:opacity-0 sm:group-hover/row:opacity-100 focus-within:opacity-100 transition-opacity">
                  <p-button icon="pi pi-sliders-h" (onClick)="manage.emit(group)" title="Manage assignments" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-200 hover:!text-surface-700 transition-colors rounded-lg"></p-button>
                  <p-button icon="pi pi-pencil" (onClick)="edit.emit(group)" title="Edit" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-200 hover:!text-surface-700 transition-colors rounded-lg"></p-button>
                  <p-button icon="pi pi-trash" (onClick)="delete.emit(group)" title="Delete" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-red-50 hover:!text-red-600 transition-colors rounded-lg"></p-button>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class UserGroupListComponent {
  groups = input.required<UserGroup[]>();
  manage = output<UserGroup>();
  edit = output<UserGroup>();
  delete = output<UserGroup>();
}
