import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest, HttpResponse } from '@angular/common/http';
import { environment } from '@env/environment';
import { Observable } from 'rxjs';
import { map, filter } from 'rxjs/operators';

// Document Versioning (Phase 19): Added interface to map backend versions to frontend.
export interface DocumentVersion {
  id: string;
  versionNumber: number;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Failed';
  isActive: boolean;
  createdAt: string;
}

export interface DocumentChunkPreviewDto {
  text: string;
  documentName: string;
  section?: string;
  pageNumber?: number;
}

export interface Document {
  id: string;
  fileName: string;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Failed';
  uploadedAt: string;
  departmentIds: string[];
  versions?: DocumentVersion[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/** Upload progress event emitted during document upload. */
export interface UploadProgress {
  /** Percentage complete (0–100), or null while response is being processed. */
  percent: number | null;
  /** Set when the upload is fully complete and the server has responded. */
  document?: Document;
}

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/documents`;

  getDocuments(): Observable<Document[]> {
    // Multi-Tenant Knowledge Base Roster:
    // Dispatches GET /api/documents to fetch active documents owned by the ambient workspace.
    // Eagerly brings back version history summaries and department bindings for UI badges.
    return this.http.get<ApiResponse<Document[]>>(this.apiUrl)
      .pipe(map(response => response.data));
  }

  /**
   * UX-13: Uploads a document with real-time progress reporting.
   * Emits UploadProgress events with a `percent` field (0–100) while uploading,
   * and a final event with `document` set when the server has processed the file.
   */
  uploadDocument(file: File, departmentIds?: string[], documentId?: string | null): Observable<UploadProgress> {
    // Multipart Binary Upload with Angular HTTP Progress Reporting:
    // 1. Constructs FormData containing the binary file stream.
    // 2. If appending a new revision to an existing document, passes documentId to trigger version bump.
    // 3. If new document, passes departmentIds for ACL bindings.
    // 4. reportProgress: true emits intermediate HttpEventType.UploadProgress events calculated
    //    via Math.round((100 * loaded) / total) to power UI progress bars.
    const formData = new FormData();
    formData.append('file', file);
    if (departmentIds && departmentIds.length > 0 && !documentId) {
      departmentIds.forEach(id => formData.append('departmentIds', id));
    }
    if (documentId) {
      formData.append('documentId', documentId);
    }

    const req = new HttpRequest('POST', `${this.apiUrl}/upload`, formData, {
      reportProgress: true
    });

    return this.http.request<ApiResponse<Document>>(req).pipe(
      filter(event =>
        event.type === HttpEventType.UploadProgress ||
        event.type === HttpEventType.Response
      ),
      map(event => {
        if (event.type === HttpEventType.UploadProgress) {
          const percent = event.total
            ? Math.round((100 * event.loaded) / event.total)
            : null;
          return { percent } as UploadProgress;
        }
        // Response received — upload complete
        const response = event as HttpResponse<ApiResponse<Document>>;
        return {
          percent: 100,
          document: response.body?.data
        } as UploadProgress;
      })
    );
  }

  deleteDocument(id: string): Observable<void> {
    // Soft-Delete Dispatch:
    // Calls DELETE /api/documents/{id} to mark the document as soft-deleted.
    // The backend broadcasts SignalR event to immediately remove the row on all open client sessions.
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  getDocumentChunk(chunkId: string): Observable<DocumentChunkPreviewDto> {
    // Citation Drawer Raw Passage Lookup:
    // Fetches the exact extracted text chunk for citation drawer inspection.
    return this.http.get<ApiResponse<DocumentChunkPreviewDto>>(`${this.apiUrl}/chunks/${chunkId}`)
      .pipe(map(response => response.data));
  }
}
