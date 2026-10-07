import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest } from '@angular/common/http';
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
    return this.http.get<ApiResponse<Document[]>>(this.apiUrl)
      .pipe(map(response => response.data));
  }

  /**
   * UX-13: Uploads a document with real-time progress reporting.
   * Emits UploadProgress events with a `percent` field (0–100) while uploading,
   * and a final event with `document` set when the server has processed the file.
   */
  uploadDocument(file: File): Observable<UploadProgress> {
    const formData = new FormData();
    formData.append('file', file);

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
        const response = event as any;
        return {
          percent: 100,
          document: response.body?.data as Document
        } as UploadProgress;
      })
    );
  }

  deleteDocument(id: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  getDocumentChunk(chunkId: string): Observable<DocumentChunkPreviewDto> {
    return this.http.get<ApiResponse<DocumentChunkPreviewDto>>(`${this.apiUrl}/chunks/${chunkId}`)
      .pipe(map(response => response.data));
  }
}
