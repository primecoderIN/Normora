import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { environment } from '@env/environment';

// ─── Domain models ─────────────────────────────────────────────────────────────

export interface CitationDto {
  documentId: string;
  fileName: string;
  score: number;
  // Document Versioning (Phase 19): True if the cited document chunk belongs to an inactive (older) DocumentVersion.
  isOutdated: boolean;
}

export interface SavedAnswerDto {
  id: string;
  messageId: string;
  conversationId: string;
  content: string;
  citations: CitationDto[];
  savedAt: string;
}

export type ExportFormat = 'Markdown' | 'Pdf' | 'Docx';

// ─── API wrapper ───────────────────────────────────────────────────────────────

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class SavedAnswerService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/saved-answers`;

  /** Returns all saved answers for the current user, newest first. */
  getSavedAnswers(limit = 50, offset = 0): Observable<SavedAnswerDto[]> {
    return this.http
      .get<ApiResponse<SavedAnswerDto[]>>(this.baseUrl, { params: { limit, offset } })
      .pipe(map(r => r.data));
  }

  /** Bookmarks an assistant message. Idempotent — returns the saved-answer ID. */
  saveAnswer(messageId: string): Observable<string> {
    return this.http
      .post<ApiResponse<string>>(this.baseUrl, { messageId })
      .pipe(map(r => r.data));
  }

  /** Removes a saved answer by the original message ID. Idempotent. */
  unsaveAnswer(messageId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${messageId}`);
  }

  /**
   * Exports a saved answer as a file download in the requested format.
   * Fetches the binary payload from the server and triggers a browser save-as dialog.
   */
  exportAnswer(savedAnswerId: string, format: ExportFormat): Observable<void> {
    const extensionMap: Record<ExportFormat, string> = {
      Markdown: '.md',
      Pdf: '.pdf',
      Docx: '.docx',
    };

    return this.http
      .get(`${this.baseUrl}/${savedAnswerId}/export`, {
        params: { format },
        responseType: 'blob',
        observe: 'response',
      })
      .pipe(
        tap(response => {
          const blob = response.body!;
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          const today = new Date().toISOString().slice(0, 10);
          anchor.href = url;
          anchor.download = `normora-answer-${today}${extensionMap[format]}`;
          anchor.click();
          URL.revokeObjectURL(url);
        }),
        map(() => void 0),
      );
  }
}
