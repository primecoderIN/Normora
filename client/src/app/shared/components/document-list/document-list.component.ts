import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Document } from '../../../core/services/document.service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <div class="block bg-white rounded-lg shadow-[0_1px_2px_rgba(15,23,42,0.04)] border border-surface-200 overflow-hidden">
      <div class="overflow-x-auto custom-scrollbar">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-surface-50 border-b border-surface-200 text-xs font-semibold text-surface-500 uppercase">
              <th class="px-5 py-3">Document</th>
              <th class="px-5 py-3">Uploaded</th>
              <th class="px-5 py-3">Status</th>
              <th class="px-5 py-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-surface-100">
            @for (doc of documents(); track doc.id) {
              <tr class="hover:bg-surface-50/50 transition-colors group">
                <td class="px-5 py-3">
                  <div class="flex items-center gap-3">
                    <div class="w-8 h-8 rounded-lg bg-indigo-50 flex items-center justify-center text-indigo-600 shrink-0">
                      <i class="pi pi-file text-sm"></i>
                    </div>
                    <div class="min-w-0">
                      <div class="font-medium text-surface-900 text-sm truncate max-w-90">{{ doc.fileName }}</div>
                      <div class="text-xs text-surface-500 mt-0.5">Company knowledge base</div>
                    </div>
                  </div>
                </td>
                <td class="px-5 py-3">
                  <span class="text-sm text-surface-500">{{ doc.uploadedAt | date:'MMM d, y' }}</span>
                </td>
                <td class="px-5 py-3">
                  @if (doc.status === 'Ready') {
                    <span class="inline-flex items-center gap-1.5 min-w-24 justify-center px-2.5 py-1 rounded-md bg-emerald-50 text-emerald-700 text-xs font-medium border border-emerald-100">
                      <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                      Ready
                    </span>
                  } @else if (doc.status === 'Failed') {
                    <span class="inline-flex items-center gap-1.5 min-w-24 justify-center px-2.5 py-1 rounded-md bg-red-50 text-red-700 text-xs font-medium border border-red-100">
                      <span class="w-1.5 h-1.5 rounded-full bg-red-500"></span>
                      Failed
                    </span>
                  } @else if (doc.status === 'Processing') {
                    <span class="inline-flex items-center gap-1.5 min-w-24 justify-center px-2.5 py-1 rounded-md bg-blue-50 text-blue-700 text-xs font-medium border border-blue-100">
                      <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse"></span>
                      Processing
                    </span>
                  } @else {
                    <span class="inline-flex items-center gap-1.5 min-w-24 justify-center px-2.5 py-1 rounded-md bg-surface-100 text-surface-700 text-xs font-medium border border-surface-200">
                      Uploaded
                    </span>
                  }
                </td>
                <td class="px-5 py-3 text-right">
                  <div class="flex items-center justify-end gap-2 sm:opacity-0 sm:group-hover:opacity-100 transition-opacity">
                    <button type="button" class="w-8 h-8 rounded-lg hover:bg-surface-200 text-surface-400 hover:text-surface-700 transition-colors flex items-center justify-center" aria-label="Download document">
                      <i class="pi pi-download"></i>
                    </button>
                    <button type="button" class="w-8 h-8 rounded-lg hover:bg-red-50 text-surface-400 hover:text-red-600 transition-colors flex items-center justify-center" (click)="onDelete.emit(doc.id)" aria-label="Delete document">
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
