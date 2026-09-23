import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FileUpload } from 'primeng/fileupload';
import { Dialog } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { Department } from '@core/services/department.service';

@Component({
  selector: 'app-document-upload-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, FileUpload, Dialog, MultiSelectModule],
  template: `
    <p-dialog
      header="Upload document"
      [visible]="visible()"
      (visibleChange)="visibleChange.emit($event)"
      [modal]="true"
      appendTo="body"
      [style]="{ width: '450px' }">
      
      <div class="flex flex-col gap-4">
        @if (!isPersonal()) {
          <div class="flex flex-col gap-2">
            <label class="text-sm font-semibold text-slate-700">Departments (Optional)</label>
            <p-multiselect
              [options]="departments()"
              [ngModel]="selectedDeptIds()"
              (ngModelChange)="selectedDeptIds.set($event)"
              optionLabel="name"
              optionValue="id"
              placeholder="Company wide (Select departments to restrict access)"
              [filter]="true"
              display="chip"
              appendTo="body"
              styleClass="w-full">
            </p-multiselect>
            <small class="text-xs text-slate-500">If no departments are selected, all employees can search this document.</small>
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
  visibleChange = output<boolean>();
  uploadSuccess = output<any>();
  uploadError = output<any>();

  selectedDeptIds = signal<string[]>([]);

  handleBeforeSend(event: any) {
    // Add CSRF header for BFF
    event.xhr.setRequestHeader('X-CSRF', '1');
    const deptIds = this.selectedDeptIds();
    if (deptIds && deptIds.length > 0) {
      deptIds.forEach(id => event.formData.append('departmentIds', id));
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
