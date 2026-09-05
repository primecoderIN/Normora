import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUpload, FileUploadEvent } from 'primeng/fileupload';
import { Toast } from 'primeng/toast';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { DocumentService, Document } from '../../../core/services/document.service';
import { environment } from '../../../../environments/environment';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { DocumentListComponent } from '../../../shared/components/document-list/document-list.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { UserService } from '../../../core/services/user.service';
import { DocumentRealtimeService, DocumentStatusChanged } from '../../../core/services/document-realtime.service';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FileUpload, Toast, Dialog, InputText, StatCardComponent, DocumentListComponent, EmptyStateComponent],
  providers: [MessageService],
  styleUrl: './documents.css',
  templateUrl: './documents.html',
})
export class Documents implements OnInit, OnDestroy {
  private documentService = inject(DocumentService);
  private documentRealtimeService = inject(DocumentRealtimeService);
  private messageService = inject(MessageService);
  private oidcSecurityService = inject(OidcSecurityService);
  public userService = inject(UserService);

  documents = signal<Document[]>([]);
  uploadUrl = `${environment.apiUrl}/api/documents/upload`;
  token = signal('');

  showUploadDialog = signal(false);
  
  ngOnInit() {
    this.loadDocuments();
    this.oidcSecurityService.getAccessToken().subscribe((token: string) => {
      this.token.set(token);
    });

    const tenantId = this.userService.currentUser()?.memberships[0]?.tenantId;
    if (tenantId) {
      void this.documentRealtimeService
        .connect(tenantId, event => this.onDocumentStatusChanged(event))
        .catch(error => console.error('Failed to connect to document realtime events:', error));
    }
  }

  ngOnDestroy() {
    void this.documentRealtimeService.disconnect();
  }

  loadDocuments() {
    this.documentService.getDocuments().subscribe({
      next: (docs) => {
        this.documents.set(docs || []);
      },
      error: (err) => {
        console.error('Failed to load documents', err);
        // Error handling is now globally covered by API interceptor
      }
    });
  }

  onUpload(event: any) {
    this.messageService.add({ severity: 'info', summary: 'Success', detail: 'Document uploaded successfully' });
    this.showUploadDialog.set(false);
    this.loadDocuments();
  }

  private onDocumentStatusChanged(event: DocumentStatusChanged) {
    const currentDocuments = this.documents();
    const documentExists = currentDocuments.some(document => document.id === event.documentId);

    if (!documentExists) {
      this.loadDocuments();
      return;
    }

    this.documents.set(currentDocuments.map(document =>
      document.id === event.documentId
        ? { ...document, status: event.status }
        : document));

    if (event.status === 'Ready') {
      this.messageService.add({ severity: 'success', summary: 'Document ready', detail: `${event.fileName} is ready for search.` });
    } else if (event.status === 'Failed') {
      this.messageService.add({ severity: 'error', summary: 'Processing failed', detail: `${event.fileName} could not be processed.` });
    }
  }

  onError(event: any) {
    let errorDetail = 'Upload failed.';
    if (event.error?.error) {
       errorDetail = typeof event.error.error === 'string' ? event.error.error : 'Invalid file type or size.';
    }
    this.messageService.add({ severity: 'error', summary: 'Upload Error', detail: errorDetail });
  }

  onBeforeUpload(event: any) {
    if (this.token()) {
      event.xhr.setRequestHeader('Authorization', `Bearer ${this.token()}`);
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
      }
    });
  }
}

