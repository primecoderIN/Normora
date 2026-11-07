import { Component, input, output, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Department } from '@core/services/department.service';
import { DocumentService, Document } from '@core/services/document.service';
import {
  DsDialogComponent,
  DsMultiSelectComponent,
  DsFileUploadComponent,
  DsFileSelectEvent,
  DsButtonComponent,
} from '@design-system';

/**
 * Modal dialog for uploading new company documents or publishing revisions to existing documents.
 * Allows employers to scope document access to specific departments or designate them as Company-Wide.
 */
@Component({
  selector: 'app-document-upload-modal',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DsDialogComponent,
    DsMultiSelectComponent,
    DsFileUploadComponent,
    DsButtonComponent,
  ],
  template: `
    <ds-dialog
      [visible]="visible()"
      (visibleChange)="visibleChange.emit($event)"
      [title]="documentId() ? 'Upload new version' : 'Upload document'"
      widthClass="max-w-lg w-full"
    >
      <div class="flex flex-col gap-4">
        @if (!isPersonal() && !documentId()) {
          <div class="flex flex-col gap-2">
            <label class="text-sm font-semibold text-slate-700 dark:text-slate-200">Who has access</label>
            <ds-multi-select
              [options]="departments()"
              [ngModel]="selectedDeptIds()"
              (ngModelChange)="selectedDeptIds.set($event)"
              optionLabel="name"
              optionValue="id"
              [placeholder]="'Everyone in ' + tenantName() + ' (Select departments to restrict access)'"
              styleClass="w-full"
            />
            <small class="text-xs text-slate-500 dark:text-slate-400">If no departments are selected, all employees can search this document.</small>
          </div>
        }

        <div class="flex flex-col gap-2">
          <label class="text-sm font-semibold text-slate-700 dark:text-slate-200">File</label>
          <ds-file-upload
            chooseLabel="Select document"
            [accept]="'.pdf,.docx,.txt'"
            [maxFileSize]="20971520"
            [multiple]="false"
            (select)="handleFileSelect($event)"
          />
        </div>
      </div>

      <div dsDialogFooter class="flex items-center justify-end gap-2.5">
        <ds-button
          variant="outline"
          (click)="cancel()"
          [disabled]="isUploading()"
        >
          Cancel
        </ds-button>
        <ds-button
          variant="primary"
          (click)="upload()"
          [disabled]="!selectedFile() || isUploading()"
          [loading]="isUploading()"
          icon="pi pi-cloud-upload"
        >
          Upload
        </ds-button>
      </div>
    </ds-dialog>
  `
})
export class DocumentUploadModalComponent {
  visible = input.required<boolean>();
  departments = input.required<Department[]>();
  uploadUrl = input<string>();
  isPersonal = input<boolean>(false);
  tenantName = input<string>('your organization');
  documentId = input<string | null>(null);
  visibleChange = output<boolean>();
  uploadSuccess = output<Document | undefined>();
  uploadError = output<unknown>();

  selectedDeptIds = signal<string[]>([]);
  selectedFile = signal<File | null>(null);
  isUploading = signal<boolean>(false);

  private documentService = inject(DocumentService);

  // File Selection:
  // Extracts the first selected File object and holds it in reactive signal state for validation.
  handleFileSelect(event: DsFileSelectEvent): void {
    if (event.files && event.files.length > 0) {
      this.selectedFile.set(event.files[0]);
    } else {
      this.selectedFile.set(null);
    }
  }

  // Upload Dispatcher:
  // Submits the multipart form payload with selected department scopes to the DocumentService.
  // Tracks upload progress events and emits uploadSuccess on completion.
  upload(): void {
    const file = this.selectedFile();
    if (!file || this.isUploading()) return;

    this.isUploading.set(true);

    this.documentService.uploadDocument(file, this.selectedDeptIds(), this.documentId()).subscribe({
      next: (event) => {
        if (event.percent === 100) {
          this.isUploading.set(false);
          this.selectedFile.set(null);
          this.selectedDeptIds.set([]);
          this.uploadSuccess.emit(event.document);
        }
      },
      error: (err: unknown) => {
        this.isUploading.set(false);
        this.uploadError.emit(err);
      },
    });
  }

  cancel(): void {
    this.selectedFile.set(null);
    this.selectedDeptIds.set([]);
    this.visibleChange.emit(false);
  }
}

