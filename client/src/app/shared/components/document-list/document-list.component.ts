import { Component, input, output, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';

import { Document, DocumentVersion } from '@core/services/document.service';
import { Department } from '@core/services/department.service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, DatePipe],
  template: `
    <div class="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden">
      <div class="overflow-x-auto custom-scrollbar">
        <table class="w-full text-left border-collapse" aria-label="Documents">
          <thead>
            <tr class="border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-950">
              <th class="px-5 py-3 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">Document</th>
              <th class="px-5 py-3 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">Uploaded</th>
              <th class="px-5 py-3 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">Status</th>
              <th class="px-5 py-3 text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400 text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            @for (doc of documents(); track doc.id) {
              <tr class="hover:bg-slate-50 dark:hover:bg-slate-950/60 transition-colors group bg-white dark:bg-slate-900 border-b border-slate-100 dark:border-slate-800"
                  [class.last:border-0]="!isExpanded(doc.id)">
                <td class="px-5 py-3.5">
                  <div class="flex items-center gap-3">
                    <div class="w-9 h-9 rounded-lg bg-primary-50 dark:bg-primary-900/30 flex items-center justify-center text-primary-600 dark:text-primary-400 shrink-0">
                      <i class="pi pi-file text-base"></i>
                    </div>
                    <div class="min-w-0">
                      <div class="font-medium text-slate-900 dark:text-white text-sm truncate max-w-xs">{{ doc.fileName }}</div>
                      <div class="mt-0.5 flex flex-wrap gap-1">
                        @if (!doc.departmentIds || doc.departmentIds.length === 0) {
                          <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">Everyone in {{ tenantName() }}</span>
                        } @else {
                          @for (deptId of doc.departmentIds; track deptId) {
                            <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300 border border-primary-100 dark:border-primary-800">
                              {{ getDepartmentName(deptId) }}
                            </span>
                          }
                        }
                      </div>
                    </div>
                  </div>
                </td>
                <td class="px-5 py-3.5">
                  <span class="text-sm text-slate-500 dark:text-slate-400">{{ doc.uploadedAt | date:'MMM d, y' }}</span>
                </td>
                <td class="px-5 py-3.5">
                  @if (doc.status === 'Ready') {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400 text-xs font-medium border border-emerald-200 dark:border-emerald-800">
                      <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 flex-none"></span>
                      Ready
                    </span>
                  } @else if (doc.status === 'Failed') {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-400 text-xs font-medium border border-red-200 dark:border-red-800">
                      <span class="w-1.5 h-1.5 rounded-full bg-red-500 flex-none"></span>
                      Failed
                    </span>
                  } @else if (doc.status === 'Processing') {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-400 text-xs font-medium border border-blue-200 dark:border-blue-800">
                      <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse flex-none"></span>
                      Processing
                    </span>
                  } @else {
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 text-xs font-medium border border-slate-200 dark:border-slate-700">
                      Uploaded
                    </span>
                  }
                </td>
                <td class="px-5 py-3.5 text-right">
                  <div class="flex items-center justify-end gap-1.5 sm:opacity-0 sm:group-hover:opacity-100 focus-within:opacity-100 transition-opacity">
                    <button
                      type="button"
                      class="w-8 h-8 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors flex items-center justify-center"
                      aria-label="Download document"
                    >
                      <i class="pi pi-download text-base"></i>
                    </button>
                    <!-- Document Versioning (Phase 19): Action button to upload a new file for the existing Document.
                         This bypasses department assignment and inherits the document's original permissions. -->
                    <button
                      type="button"
                      class="w-8 h-8 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-emerald-600 dark:hover:text-emerald-400 transition-colors flex items-center justify-center"
                      (click)="onUploadNewVersion.emit(doc.id)"
                      aria-label="Upload new version"
                      [title]="'Upload new version'"
                    >
                      <i class="pi pi-upload text-base"></i>
                    </button>
                    <button
                      type="button"
                      class="w-8 h-8 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors flex items-center justify-center"
                      (click)="toggleExpand(doc.id)"
                      [attr.aria-expanded]="isExpanded(doc.id)"
                      aria-label="View history"
                      [title]="'View history'"
                    >
                      <i class="pi pi-history text-base"></i>
                    </button>
                    <button
                      type="button"
                      class="w-8 h-8 rounded-lg hover:bg-red-50 dark:hover:bg-red-950/40 text-slate-400 hover:text-red-600 dark:hover:text-red-400 transition-colors flex items-center justify-center"
                      (click)="onConfirmDelete.emit(doc.id)"
                      aria-label="Delete document"
                      [title]="'Delete document'"
                    >
                      <i class="pi pi-trash text-base"></i>
                    </button>
                  </div>
                </td>
              </tr>
              <!-- Inline Delete Confirmation -->
              @if (documentToDeleteId() === doc.id) {
                <tr class="bg-red-50/50 dark:bg-red-900/10 border-b border-red-100 dark:border-red-900/30 animate-in fade-in slide-in-from-top-2 duration-200">
                  <td colspan="4" class="px-5 py-3">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-2 text-red-700 dark:text-red-400">
                        <i class="pi pi-exclamation-triangle"></i>
                        <span class="text-sm font-medium">Are you sure you want to delete this document?</span>
                      </div>
                      <div class="flex items-center gap-2">
                        <button
                          type="button"
                          class="px-3 py-1.5 text-xs font-semibold text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-md transition-colors disabled:opacity-50"
                          (click)="onCancelDelete.emit()"
                          [disabled]="isDeleting()"
                        >
                          Cancel
                        </button>
                        <button
                          type="button"
                          class="flex items-center gap-2 px-3 py-1.5 text-xs font-semibold text-white bg-red-600 hover:bg-red-700 rounded-md shadow-sm transition-colors disabled:opacity-50"
                          (click)="onExecuteDelete.emit()"
                          [disabled]="isDeleting()"
                        >
                          @if (isDeleting()) {
                            <i class="pi pi-spinner pi-spin"></i>
                            <span>Deleting...</span>
                          } @else {
                            <span>Delete document</span>
                          }
                        </button>
                      </div>
                    </div>
                  </td>
                </tr>
              }
              <!-- Document Versioning (Phase 19): Expanded Version History Row -->
              <!-- Shows chronological list of file updates when the history button is clicked -->
              @if (isExpanded(doc.id) && doc.versions) {
                <tr class="bg-slate-50 dark:bg-slate-900/50 border-b border-slate-200 dark:border-slate-700 last:border-0">
                  <td colspan="4" class="p-0">
                    <div class="px-5 py-4 pl-16">
                      <h4 class="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-3">Version History</h4>
                      <div class="flex flex-col gap-2">
                        @for (version of doc.versions; track version.id) {
                          <div class="flex items-center justify-between p-3 rounded-lg bg-white dark:bg-slate-950 border border-slate-200 dark:border-slate-800">
                            <div class="flex items-center gap-3">
                              <div class="w-7 h-7 rounded bg-slate-100 dark:bg-slate-900 flex items-center justify-center text-slate-500 font-medium text-xs">
                                v{{ version.versionNumber }}
                              </div>
                              <div class="flex flex-col">
                                <span class="text-sm font-medium text-slate-900 dark:text-white">
                                  {{ version.createdAt | date:'MMM d, y, h:mm a' }}
                                </span>
                                <span class="text-xs text-slate-500">
                                  @if (version.isActive) {
                                    <span class="text-emerald-600 dark:text-emerald-400 font-medium">Active Version</span>
                                  } @else {
                                    <span>Superseded</span>
                                  }
                                </span>
                              </div>
                            </div>
                            <div class="flex items-center gap-3">
                              <span class="text-xs font-medium px-2 py-1 rounded bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">
                                {{ version.status }}
                              </span>
                            </div>
                          </div>
                        }
                      </div>
                    </div>
                  </td>
                </tr>
              }
            } @empty {
              <tr>
                <td colspan="4" class="px-6 py-12 text-center text-slate-400 dark:text-slate-500 text-sm">
                  <div class="flex flex-col items-center gap-2">
                    <i class="pi pi-file text-3xl opacity-40"></i>
                    <span>No documents found.</span>
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
  departments = input<Department[]>([]);
  tenantName = input.required<string>();
  
  // Action events
  onConfirmDelete = output<string>();
  onCancelDelete = output<void>();
  onExecuteDelete = output<void>();
  onUploadNewVersion = output<string>();

  // State inputs
  documentToDeleteId = input<string | null>(null);
  isDeleting = input<boolean>(false);

  expandedRowId = signal<string | null>(null);

  getDepartmentName(id: string): string {
    const dept = this.departments().find(d => d.id === id);
    return dept ? dept.name : 'Unknown';
  }

  isExpanded(docId: string): boolean {
    return this.expandedRowId() === docId;
  }

  toggleExpand(docId: string): void {
    if (this.expandedRowId() === docId) {
      this.expandedRowId.set(null);
    } else {
      this.expandedRowId.set(docId);
    }
  }
}
