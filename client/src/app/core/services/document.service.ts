import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface Document {
  id: string;
  fileName: string;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Failed';
  uploadedAt: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
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

  uploadDocument(file: File): Observable<Document> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<Document>>(`${this.apiUrl}/upload`, formData)
      .pipe(map(response => response.data));
  }

  deleteDocument(id: string): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }
}
