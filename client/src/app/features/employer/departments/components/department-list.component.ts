import { Component, ViewChild, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Department } from '@core/services/department.service';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, DatePipe, MenuModule],
  template: `
    <p-menu #menu [popup]="true" [model]="menuItems" styleClass="!text-sm !min-w-[150px]"></p-menu>

    <div class="overflow-x-auto">
      <table class="w-full text-left text-sm">
        <thead class="border-b border-slate-200">
          <tr>
            <th class="px-6 py-3 font-semibold text-slate-500 text-xs uppercase tracking-wider">Department</th>
            <th class="px-6 py-3 font-semibold text-slate-500 text-xs uppercase tracking-wider">Description</th>
            <th class="px-6 py-3 font-semibold text-slate-500 text-xs uppercase tracking-wider">Employees</th>
            <th class="px-6 py-3 font-semibold text-slate-500 text-xs uppercase tracking-wider">Documents</th>
            <th class="px-6 py-3 font-semibold text-slate-500 text-xs uppercase tracking-wider">Created</th>
            <th class="px-6 py-3 font-semibold text-slate-500 text-xs uppercase tracking-wider w-16">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          @for (dept of departments(); track dept.id) {
            <tr class="hover:bg-slate-50 transition-colors group">
              <td class="px-6 py-4 whitespace-nowrap">
                <div class="flex items-center gap-3">
                  <div class="flex items-center justify-center flex-none w-9 h-9 rounded-lg font-bold text-base text-white"
                       [style.background-color]="getAvatarColor(dept.name)">
                    {{ dept.name.charAt(0).toUpperCase() }}
                  </div>
                  <span class="font-semibold text-slate-900">{{ dept.name }}</span>
                </div>
              </td>
              <td class="px-6 py-4 text-slate-500">
                <span class="truncate block max-w-[220px]">{{ dept.description || '—' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-blue-50 text-blue-600">
                  0 employees
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-purple-50 text-purple-600">
                  0 documents
                </span>
              </td>
              <td class="px-6 py-4 text-slate-500 whitespace-nowrap text-sm">
                {{ dept.createdAt | date:'MMM dd, yyyy' }}
              </td>
              <td class="px-6 py-4">
                <button
                  type="button"
                  class="w-8 h-8 flex items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-slate-700 transition-colors opacity-0 group-hover:opacity-100 focus:opacity-100"
                  (click)="openMenu($event, dept)"
                >
                  <i class="pi pi-ellipsis-v text-sm"></i>
                </button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class DepartmentListComponent {
  @ViewChild('menu') menu!: Menu;

  departments = input.required<Department[]>();
  edit = output<Department>();
  delete = output<Department>();

  menuItems: MenuItem[] = [];

  private readonly avatarColors = [
    '#e11d48', '#db2777', '#9333ea', '#7c3aed',
    '#2563eb', '#0891b2', '#059669', '#d97706'
  ];

  getAvatarColor(name: string): string {
    const idx = name.charCodeAt(0) % this.avatarColors.length;
    return this.avatarColors[idx];
  }

  openMenu(event: MouseEvent, dept: Department): void {
    this.menuItems = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => this.edit.emit(dept)
      },
      {
        separator: true
      },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        styleClass: '!text-red-600',
        command: () => this.delete.emit(dept)
      }
    ];
    this.menu.toggle(event);
  }
}
