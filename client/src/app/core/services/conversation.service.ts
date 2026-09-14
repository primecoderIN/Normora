import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

// ─── Domain models ────────────────────────────────────────────────────────────

export interface ConversationDto {
  id: string;
  title: string;
  summary?: string;
  createdAt: string;
  updatedAt: string;
  lastMessageAt: string;
}

export interface MessageCitationDto {
  documentId: string;
  documentChunkId: string;
  fileName: string;
  score: number;
}

export interface MessageDto {
  id: string;
  role: 'User' | 'Assistant';
  content: string;
  createdAt: string;
  rewritten: boolean;
  citations: MessageCitationDto[];
}

export interface ConversationDetailDto extends ConversationDto {
  messages: MessageDto[];
}

export interface SendMessageResult {
  conversationId: string;
  userMessageId: string;
  assistantMessageId: string;
  answer: string;
  sources: { documentId: string; fileName: string; chunkIndex: number; score: number }[];
}

// ─── API wrapper ──────────────────────────────────────────────────────────────

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api/conversations`;

  /** List all conversations for the current user, newest first. */
  getConversations(limit = 50, offset = 0): Observable<ConversationDto[]> {
    return this.http
      .get<ApiResponse<ConversationDto[]>>(this.base, { params: { limit, offset } })
      .pipe(map(r => r.data));
  }

  /** Load full conversation with all messages and citations. */
  getConversation(id: string): Observable<ConversationDetailDto> {
    return this.http
      .get<ApiResponse<ConversationDetailDto>>(`${this.base}/${id}`)
      .pipe(map(r => r.data));
  }

  /** Create an empty conversation shell (title assigned by server). */
  createConversation(): Observable<ConversationDto> {
    return this.http
      .post<ApiResponse<ConversationDto>>(this.base, {})
      .pipe(map(r => r.data));
  }

  /** Send a message and receive a grounded AI answer. */
  sendMessage(conversationId: string, question: string, limit = 5): Observable<SendMessageResult> {
    return this.http
      .post<ApiResponse<SendMessageResult>>(`${this.base}/${conversationId}/messages`, { question, limit })
      .pipe(map(r => r.data));
  }

  /** Permanently delete a conversation and all its messages. */
  deleteConversation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
