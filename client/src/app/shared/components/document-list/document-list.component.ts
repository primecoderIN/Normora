import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Document } from '../../../core/services/document.service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <div class="bg-white rounded-2xl shadow-[0_2px_10px_-4px_rgba(0,0,0,0.05)] border border-surface-200 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-surface-50 border-b border-surface-200 text-xs font-semibold text-surface-500 uppercase tracking-wider">
              <th class="px-6 py-4">Name</th>
              <th class="px-6 py-4">Date</th>
              <th class="px-6 py-4">Status</th>
              <th class="px-6 py-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-surface-100">
            @for (doc of documents(); track doc.id) {
              <tr class="hover:bg-surface-50/50 transition-colors group">
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                    <div class="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center text-indigo-600">
                      <i class="pi pi-file text-sm"></i>
                    </div>
                    <span class="font-medium text-surface-900 text-sm">{{ doc.fileName }}</span>
                  </div>
                </td>
                <td class="px-6 py-4">
                  <span class="text-sm text-surface-500">{{ doc.uploadedAt | date:'MMM d, y' }}</span>
                </td>
                <td class="px-6 py-4">
                  @if (doc.status === 1) {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-emerald-50 text-emerald-700 text-xs font-medium border border-emerald-100">
                      <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                      Published
                    </span>
                  } @else if (doc.status === 0) {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-surface-100 text-surface-700 text-xs font-medium border border-surface-200">
                      Draft
                    </span>
                  } @else {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-amber-50 text-amber-700 text-xs font-medium border border-amber-100">
                      Archived
                    </span>
                  }
                </td>
                <td class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button class="w-8 h-8 rounded-lg hover:bg-surface-200 text-surface-400 hover:text-surface-700 transition-colors flex items-center justify-center">
                      <i class="pi pi-download"></i>
                    </button>
                    <button class="w-8 h-8 rounded-lg hover:bg-red-50 text-surface-400 hover:text-red-600 transition-colors flex items-center justify-center" (click)="onDelete.emit(doc.id)">
                      <i class="pi pi-trash"></i>
                    </button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class DocumentListComponent {
  documents = input.required<Document[]>();
  onDelete = output<string>();
}
