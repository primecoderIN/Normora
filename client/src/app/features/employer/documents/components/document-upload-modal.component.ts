import { Component, input, output, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Department } from '@core/services/department.service';
import { UserService } from '@core/services/user.service';
import {
  DsDialogComponent,
  DsMultiSelectComponent,
  DsFileUploadComponent,
  DsFileSelectEvent,
  DsButtonComponent,
} from '@design-system';

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
  uploadUrl = input.required<string>();
  isPersonal = input<boolean>(false);
  tenantName = input<string>('your organization');
  documentId = input<string | null>(null);
  visibleChange = output<boolean>();
  uploadSuccess = output<any>();
  uploadError = output<any>();

  selectedDeptIds = signal<string[]>([]);
  selectedFile = signal<File | null>(null);
  isUploading = signal<boolean>(false);

  private http = inject(HttpClient);
  userService = inject(UserService);

  handleFileSelect(event: DsFileSelectEvent): void {
    if (event.files && event.files.length > 0) {
      this.selectedFile.set(event.files[0]);
    } else {
      this.selectedFile.set(null);
    }
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file || this.isUploading()) return;

    this.isUploading.set(true);

    const formData = new FormData();
    formData.append('file', file);

    const deptIds = this.selectedDeptIds();
    // Document Versioning (Phase 19): Department scoping only applies to new documents.
    // If uploading a new version (documentId exists), we skip attaching departments.
    if (deptIds && deptIds.length > 0 && !this.documentId()) {
      deptIds.forEach((id) => formData.append('departmentIds', id));
    }

    // Document Versioning (Phase 19): If present, append the existing document ID 
    // to signal the API to create a new DocumentVersion instead of a new Document.
    const docId = this.documentId();
    if (docId) {
      formData.append('documentId', docId);
    }

    this.http.post(this.uploadUrl(), formData, { withCredentials: true }).subscribe({
      next: (res) => {
        this.isUploading.set(false);
        this.selectedFile.set(null);
        this.selectedDeptIds.set([]);
        this.uploadSuccess.emit(res);
      },
      error: (err) => {
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

