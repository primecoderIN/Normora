import { Component, input, output, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FileUpload } from 'primeng/fileupload';
import { Dialog } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { Department } from '@core/services/department.service';
import { UserService } from '@core/services/user.service';

@Component({
  selector: 'app-document-upload-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUpload, Dialog, MultiSelectModule],
  template: `
    <p-dialog
      [header]="documentId() ? 'Upload new version' : 'Upload document'"
      [visible]="visible()"
      (visibleChange)="visibleChange.emit($event)"
      [modal]="true"
      appendTo="body"
      [style]="{ width: '450px' }">
      
      <div class="flex flex-col gap-4">
        @if (!isPersonal() && !documentId()) {
          <div class="flex flex-col gap-2">
            <label class="text-sm font-semibold text-slate-700 dark:text-slate-200">Who has access</label>
            <p-multiselect
              [options]="departments()"
              [ngModel]="selectedDeptIds()"
              (ngModelChange)="selectedDeptIds.set($event)"
              optionLabel="name"
              optionValue="id"
              [placeholder]="'Everyone in ' + tenantName() + ' (Select departments to restrict access)'"
              [filter]="true"
              display="chip"
              appendTo="body"
              styleClass="w-full">
            </p-multiselect>
            <small class="text-xs text-slate-500 dark:text-slate-400">If no departments are selected, all employees can search this document.</small>
          </div>
        }

        <p-fileupload
          name="file"
          [url]="uploadUrl()"
          (onBeforeSend)="handleBeforeSend($event)"
          (onUpload)="handleUpload($event)"
          (onError)="handleError($event)"
          [multiple]="false"
          accept=".pdf,.docx,.txt"
          [maxFileSize]="20971520"
          chooseLabel="Select file"
          uploadLabel="Upload"
          cancelLabel="Cancel"
          styleClass="p-fileupload-custom w-full">
        </p-fileupload>
      </div>
    </p-dialog>
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

  userService = inject(UserService);

  handleBeforeSend(event: any) {
    // Add CSRF header for BFF
    event.xhr.setRequestHeader('X-CSRF', '1');
    event.xhr.withCredentials = true; // Essential for sending the BFF auth cookie!
    
    // Add Tenant ID header (since p-fileupload bypasses Angular Interceptors)
    const activeTenantId = this.userService.activeTenantId();
    const fallbackTenantId = this.userService.currentUser()?.memberships[0]?.tenantId;
    const tenantId = activeTenantId || fallbackTenantId;
    if (tenantId) {
      event.xhr.setRequestHeader('X-Tenant-Id', tenantId);
    }
    
    const deptIds = this.selectedDeptIds();
    // Document Versioning (Phase 19): Department scoping only applies to new documents.
    // If uploading a new version (documentId exists), we skip attaching departments.
    if (deptIds && deptIds.length > 0 && !this.documentId()) {
      deptIds.forEach(id => event.formData.append('departmentIds', id));
    }
    
    // Document Versioning (Phase 19): If present, append the existing document ID 
    // to signal the API to create a new DocumentVersion instead of a new Document.
    if (this.documentId()) {
      event.formData.append('documentId', this.documentId());
    }
  }

  handleUpload(event: any) {
    this.uploadSuccess.emit(event);
    this.selectedDeptIds.set([]);
  }

  handleError(event: any) {
    this.uploadError.emit(event);
  }
}
