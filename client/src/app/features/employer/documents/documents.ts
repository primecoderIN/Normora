import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FileUploadModule, FileUploadEvent } from 'primeng/fileupload';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { DocumentService, Document } from '../../core/services/document.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FileUploadModule, TableModule, ButtonModule, ToastModule, TagModule, DatePipe],
  providers: [MessageService],
  styleUrl: './documents.css',
  templateUrl: './documents.html',
})
export class Documents implements OnInit {
  private documentService = inject(DocumentService);
  private messageService = inject(MessageService);

  documents: Document[] = [];
  uploadUrl = `${environment.apiUrl}/api/documents/upload`;
  
  ngOnInit() {
    this.loadDocuments();
  }

  loadDocuments() {
    this.documentService.getDocuments().subscribe({
      next: (docs) => this.documents = docs,
      error: (err) => {
        console.error('Failed to load documents', err);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load documents.' });
      }
    });
  }

  // Handle successful upload from PrimeNG FileUpload
  onUpload(event: any) {
    this.messageService.add({ severity: 'info', summary: 'Success', detail: 'Document uploaded successfully' });
    this.loadDocuments(); // Refresh the list
  }

  // Handle errors from PrimeNG FileUpload
  onError(event: any) {
    let errorDetail = 'Upload failed.';
    if (event.error?.error) {
       errorDetail = typeof event.error.error === 'string' ? event.error.error : 'Invalid file type or size.';
    }
    this.messageService.add({ severity: 'error', summary: 'Upload Error', detail: errorDetail });
  }

  // Pass authorization token with the PrimeNG upload request
  onBeforeUpload(event: any) {
    const token = sessionStorage.getItem('angular-auth-oidc-client-token');
    if (token) {
      event.xhr.setRequestHeader('Authorization', `Bearer ${token}`);
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
      case 1: return 'PROCESSED';
      case 0: return 'PROCESSING';
      case 2: return 'ERROR';
      default: return 'UNKNOWN';
    }
  }
}

