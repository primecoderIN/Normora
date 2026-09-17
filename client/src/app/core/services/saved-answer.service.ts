import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@env/environment';

// ─── Domain models ─────────────────────────────────────────────────────────────

export interface CitationDto {
  documentId: string;
  fileName: string;
  score: number;
}

export interface SavedAnswerDto {
  id: string;
  messageId: string;
  conversationId: string;
  content: string;
  citations: CitationDto[];
  savedAt: string;
}

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
}
