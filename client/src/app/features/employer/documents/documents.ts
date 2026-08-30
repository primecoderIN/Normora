import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FileUpload, FileUploadEvent } from 'primeng/fileupload';
import { Toast } from 'primeng/toast';
import { Dialog } from 'primeng/dialog';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { DocumentService, Document } from '../../../core/services/document.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FileUpload, Toast, DatePipe, Dialog, Checkbox, InputText],
  providers: [MessageService],
  styleUrl: './documents.css',
  templateUrl: './documents.html',
})
export class Documents implements OnInit {
  private documentService = inject(DocumentService);
  private messageService = inject(MessageService);
  private oidcSecurityService = inject(OidcSecurityService);
  private cdr = inject(ChangeDetectorRef);

  documents: Document[] = [];
  uploadUrl = `${environment.apiUrl}/api/documents/upload`;
  token = '';

  showUploadDialog = false;
  
  ngOnInit() {
    this.loadDocuments();
    this.oidcSecurityService.getAccessToken().subscribe((token: string) => {
      this.token = token;
    });
  }

  loadDocuments() {
    this.documentService.getDocuments().subscribe({
      next: (docs) => {
        this.documents = docs || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load documents', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load documents.' });
      }
    });
  }

  // Handle successful upload from PrimeNG native uploader
  onUpload(event: any) {
    this.messageService.add({ severity: 'info', summary: 'Success', detail: 'Document uploaded successfully' });
    this.showUploadDialog = false;
    this.loadDocuments(); // Refresh the list
  }

  // Handle errors from PrimeNG native uploader
  onError(event: any) {
    let errorDetail = 'Upload failed.';
    if (event.error?.error) {
       errorDetail = typeof event.error.error === 'string' ? event.error.error : 'Invalid file type or size.';
    }
    this.messageService.add({ severity: 'error', summary: 'Upload Error', detail: errorDetail });
  }

  // Intercept the native XHR request to add the Bearer token
  onBeforeUpload(event: any) {
    if (this.token) {
      event.xhr.setRequestHeader('Authorization', `Bearer ${this.token}`);
    }
  }

  deleteDocument(id: string) {
    this.documentService.deleteDocument(id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Document deleted' });
        this.loadDocuments();
      },
      error: (err) => {
        console.error('Failed to delete', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete document.' });
      }
    });
  }

  getSeverity(status: number) {
    switch (status) {
      case 1: return 'success'; // Processed
      case 0: return 'warn';    // Processing
      case 2: return 'danger';  // Error
      default: return 'info';
    }
  }

    getStatusText(status: number) {
    switch (status) {
      case 1: return 'Published';
      case 0: return 'Draft';
      case 2: return 'Archived';
      default: return 'Draft';
    }
  }

  getFileExtension(fileName: string): string {
    if (!fileName) return 'FILE';
    const parts = fileName.split('.');
    return parts.length > 1 ? parts[parts.length - 1].toUpperCase() : 'FILE';
  }

  getFileColorClass(fileName: string): string {
    const ext = this.getFileExtension(fileName).toLowerCase();
    switch (ext) {
      case 'pdf': return 'bg-red-50 text-red-500';
      case 'doc':
      case 'docx': return 'bg-blue-50 text-blue-500';
      case 'xls':
      case 'xlsx': return 'bg-green-50 text-green-600';
      case 'ppt':
      case 'pptx': return 'bg-orange-50 text-orange-500';
      case 'txt': return 'bg-surface-100 text-surface-600';
      default: return 'bg-indigo-50 text-indigo-500';
    }
  }
}

