import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Toast } from 'primeng/toast';
import { InputText } from 'primeng/inputtext';
import { MessageService } from 'primeng/api';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { DocumentService, Document } from '../../../core/services/document.service';
import { environment } from '../../../../environments/environment';

import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { DocumentListComponent } from '../../../shared/components/document-list/document-list.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { DocumentUploadModalComponent } from './components/document-upload-modal.component';

import { UserService } from '../../../core/services/user.service';
import { DocumentRealtimeService, DocumentStatusChanged } from '../../../core/services/document-realtime.service';
import { DepartmentService, Department } from '../../../core/services/department.service';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Toast,
    InputText,
    StatCardComponent,
    DocumentListComponent,
    EmptyStateComponent,
    ButtonModule,
    DocumentUploadModalComponent,
    SkeletonModule,
  ],
  providers: [MessageService],
  templateUrl: './documents.html',
  styleUrl: './documents.css',
})
export class Documents implements OnInit, OnDestroy {
  private documentService = inject(DocumentService);
  private documentRealtimeService = inject(DocumentRealtimeService);
  private messageService = inject(MessageService);
  private oidcSecurityService = inject(OidcSecurityService);
  private departmentService = inject(DepartmentService);
  public userService = inject(UserService);

  departments = signal<Department[]>([]);

  documents = signal<Document[]>([]);
  uploadUrl = `${environment.apiUrl}/api/documents/upload`;
  token = signal('');

  showUploadDialog = signal(false);
  isLoading = signal(true);
  selectedStatus = signal<'All' | Document['status']>('All');
  searchTerm = signal('');

  filteredDocuments = computed(() => {
    const status = this.selectedStatus();
    const query = this.searchTerm().trim().toLowerCase();

    return this.documents().filter(document => {
      const matchesStatus = status === 'All' || document.status === status;
      const matchesQuery = !query || document.fileName.toLowerCase().includes(query);
      return matchesStatus && matchesQuery;
    });
  });

  readyCount = computed(() => this.countByStatus('Ready'));
  processingCount = computed(() => this.countByStatus('Processing'));
  failedCount = computed(() => this.countByStatus('Failed'));

  ngOnInit() {
    this.loadDocuments();
    this.loadDepartments();
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
    this.isLoading.set(true);
    this.documentService.getDocuments().subscribe({
      next: (docs) => { this.documents.set(docs || []); this.isLoading.set(false); },
      error: (err) => { console.error('Failed to load documents', err); this.isLoading.set(false); }
    });
  }

  loadDepartments() {
    this.departmentService.getAll().subscribe({
      next: (depts) => this.departments.set(depts || [])
    });
  }

  onUploadSuccess(event: any) {
    this.messageService.add({ severity: 'info', summary: 'Success', detail: 'Document uploaded successfully' });
    this.showUploadDialog.set(false);
    this.loadDocuments();
  }

  onUploadError(event: any) {
    let errorDetail = 'Upload failed.';
    if (event.error?.error) {
      errorDetail = typeof event.error.error === 'string' ? event.error.error : 'Invalid file type or size.';
    }
    this.messageService.add({ severity: 'error', summary: 'Upload Error', detail: errorDetail });
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

  deleteDocument(id: string) {
    this.documentService.deleteDocument(id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Document deleted' });
        this.loadDocuments();
      },
      error: (err) => { console.error('Failed to delete', err); }
    });
  }

  selectStatus(status: 'All' | Document['status']) {
    this.selectedStatus.set(status);
  }

  private countByStatus(status: Document['status']) {
    return this.documents().filter(document => document.status === status).length;
  }
}
