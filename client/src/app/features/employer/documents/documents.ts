import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DocumentService, Document } from '@core/services/document.service';
import { AuthService } from '@core/services/auth.service';
import { environment } from '@env/environment';

import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { DocumentListComponent } from '@shared/components/document-list/document-list.component';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { DocumentUploadModalComponent } from './components/document-upload-modal.component';

import { UserService } from '@core/services/user.service';
import { DocumentRealtimeService, DocumentStatusChanged } from '@core/services/document-realtime.service';
import { DepartmentService, Department } from '@core/services/department.service';
import { ApiMessages } from '@core/constants/api-messages';
import { DsSkeletonComponent, DsToastComponent, DsToastService, DsInputComponent } from '@design-system';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    StatCardComponent,
    DocumentListComponent,
    EmptyStateComponent,
    DocumentUploadModalComponent,
    DsSkeletonComponent,
    DsToastComponent,
    DsInputComponent,
  ],
  templateUrl: './documents.html',
})
export class Documents implements OnInit, OnDestroy {
  private documentService = inject(DocumentService);
  private documentRealtimeService = inject(DocumentRealtimeService);
  private messageService = inject(DsToastService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private departmentService = inject(DepartmentService);
  public userService = inject(UserService);

  departments = signal<Department[]>([]);

  documents = signal<Document[]>([]);
  uploadUrl = `${environment.apiUrl}/api/documents/upload`;

  showUploadDialog = signal(false);
  uploadingDocumentId = signal<string | null>(null);
  isLoading = signal(true);
  selectedStatus = signal<'All' | Document['status']>('All');
  searchTerm = signal('');

  // Phase 20: Inline delete confirmation state
  documentToDelete = signal<string | null>(null);
  isDeleting = signal(false);

  isPersonalWorkspace = this.userService.isPersonalWorkspace;
  activeTenantName = this.userService.activeTenantName;

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

    const activeId = this.userService.activeTenantId();
    const tenantId = activeId || this.userService.currentUser()?.memberships[0]?.tenantId;
    if (tenantId) {
      void this.documentRealtimeService
        .connect(
          tenantId, 
          event => this.onDocumentStatusChanged(event),
          documentId => this.onDocumentDeleted(documentId)
        )
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

  onUploadSuccess(_event: unknown) {
    this.messageService.add({ severity: 'success', summary: ApiMessages.DocumentUploaded.summary, detail: ApiMessages.DocumentUploaded.detail });
    this.showUploadDialog.set(false);
    this.uploadingDocumentId.set(null);
    this.loadDocuments();
  }

  onUploadError(event: { error?: { error?: string } }) {
    let errorDetail: string = ApiMessages.UploadFailed.detail;
    if (event.error?.error) {
      errorDetail = typeof event.error.error === 'string' ? event.error.error : ApiMessages.UploadFailed.detail;
    }
    this.messageService.add({ severity: 'error', summary: ApiMessages.UploadFailed.summary, detail: errorDetail });
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
      this.messageService.add({ severity: 'success', summary: ApiMessages.DocumentReady.summary, detail: `${event.fileName} is ready for search.` });
    } else if (event.status === 'Failed') {
      this.messageService.add({ severity: 'error', summary: ApiMessages.DocumentProcessingFailed.summary, detail: `${event.fileName} could not be processed.` });
    }
  }

  private onDocumentDeleted(documentId: string) {
    this.documents.set(this.documents().filter(d => d.id !== documentId));
  }

  confirmDelete(id: string) {
    this.documentToDelete.set(id);
  }

  cancelDelete() {
    this.documentToDelete.set(null);
  }

  executeDelete() {
    const id = this.documentToDelete();
    if (!id) return;
    
    this.isDeleting.set(true);
    const documentToRemove = this.documents().find(d => d.id === id);
    
    // Optimistic UI removal
    this.documents.set(this.documents().filter(d => d.id !== id));
    this.documentToDelete.set(null);

    this.documentService.deleteDocument(id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: ApiMessages.DocumentDeleted.summary, detail: ApiMessages.DocumentDeleted.detail });
        this.isDeleting.set(false);
      },
      error: (err) => {
        console.error('Failed to delete', err);
        // Revert optimistic removal
        if (documentToRemove) {
          this.documents.set([...this.documents(), documentToRemove]);
        }
        this.messageService.add({ severity: 'error', summary: ApiMessages.DocumentDeleteFailed.summary, detail: ApiMessages.DocumentDeleteFailed.detail });
        this.isDeleting.set(false);
      }
    });
  }

  openUploadDialog() {
    this.uploadingDocumentId.set(null);
    this.showUploadDialog.set(true);
  }

  // Document Versioning (Phase 19): Triggered from the document list to upload a new file version.
  // It passes the documentId to the DocumentUploadModalComponent.
  openUploadNewVersion(documentId: string) {
    this.uploadingDocumentId.set(documentId);
    this.showUploadDialog.set(true);
  }

  selectStatus(status: 'All' | Document['status']) {
    this.selectedStatus.set(status);
  }

  private countByStatus(status: Document['status']) {
    return this.documents().filter(document => document.status === status).length;
  }
}
