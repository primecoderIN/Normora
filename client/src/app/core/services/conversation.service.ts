import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { environment } from '@env/environment';

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
  private oidcSecurityService = inject(OidcSecurityService);
  
  public readonly baseUrl = `${environment.apiUrl}/api/conversations`;

  getAccessToken(): Observable<string> {
    return this.oidcSecurityService.getAccessToken();
  }

  /** List all conversations for the current user, newest first. */
  getConversations(limit = 50, offset = 0): Observable<ConversationDto[]> {
    return this.http
      .get<ApiResponse<ConversationDto[]>>(this.baseUrl, { params: { limit, offset } })
      .pipe(map(r => r.data));
  }

  /** Load full conversation with all messages and citations. */
  getConversation(id: string): Observable<ConversationDetailDto> {
    return this.http
      .get<ApiResponse<ConversationDetailDto>>(`${this.baseUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  /** Create an empty conversation shell (title assigned by server). */
  createConversation(): Observable<ConversationDto> {
    return this.http
      .post<ApiResponse<ConversationDto>>(this.baseUrl, {})
      .pipe(map(r => r.data));
  }

  // Send a chat message to the backend and wait for the LLM to stream or return a grounded answer with citations
  sendMessage(conversationId: string, question: string, limit = 5): Observable<SendMessageResult> {
    return this.http
      .post<ApiResponse<SendMessageResult>>(`${this.baseUrl}/${conversationId}/messages`, { question, limit })
      .pipe(map(r => r.data));
  }

  /** Permanently delete a conversation and all its messages. */
  deleteConversation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
