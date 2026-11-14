import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '@env/environment';

// ─── Domain models ────────────────────────────────────────────────────────────

export interface SuggestedQuestionDto {
  question: string;
  iconClass: string;
}

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
  // Document Versioning (Phase 19): True if the cited document chunk belongs to an inactive (older) DocumentVersion.
  isOutdated: boolean;
}

export interface ToolExecutionDto {
  toolName: string;
  toolCallId: string;
  status: 'running' | 'completed' | 'failed';
  summary?: string;
}

export interface ActionCardDto {
  actionType: string;
  title: string;
  description: string;
  parameters: Record<string, string>;
  confirmButtonText?: string;
  cancelButtonText?: string;
  isConfirmed?: boolean;
  isDismissed?: boolean;
}

export interface MessageDto {
  id: string;
  role: 'User' | 'Assistant';
  content: string;
  createdAt: string;
  rewritten: boolean;
  citations: MessageCitationDto[];
  feedback?: 1 | 2;
  tools?: ToolExecutionDto[];
  actionCard?: ActionCardDto;
}

export interface ConversationDetailDto extends ConversationDto {
  messages: MessageDto[];
}

export interface ConversationShareDto {
  token: string;
  expiresAt?: string;
}

export interface SharedConversationDetailDto extends ConversationDetailDto {
  sharedByName?: string;
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
  
  public readonly baseUrl = `${environment.apiUrl}/api/conversations`;

  /** List all conversations for the current user, newest first. */
  getConversations(limit = 50, offset = 0): Observable<ConversationDto[]> {
    // Keyset / Offset Pagination:
    // Retrieves conversation threads scoped to the authenticated caller's OIDC ExternalUserId and active workspace.
    // Ordered by LastMessageAt descending so actively active conversations appear first.
    return this.http
      .get<ApiResponse<ConversationDto[]>>(this.baseUrl, { params: { limit, offset } })
      .pipe(map(r => r.data));
  }

  /** Load full conversation with all messages and citations. */
  getConversation(id: string): Observable<ConversationDetailDto> {
    // BOLA-Protected Thread Retrieval:
    // Fetches full message history and citation links.
    // If the conversation belongs to another user or workspace, the backend throws BolaException (404).
    return this.http
      .get<ApiResponse<ConversationDetailDto>>(`${this.baseUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  /** Create an empty conversation shell (title assigned by server). */
  createConversation(): Observable<ConversationDto> {
    // JIT Thread Creation:
    // Initializes a new conversation shell with default title "New conversation" and SummaryVersion = 0.
    return this.http
      .post<ApiResponse<ConversationDto>>(this.baseUrl, {})
      .pipe(map(r => r.data));
  }

  // Send a chat message to the backend and wait for the LLM to stream or return a grounded answer with citations
  sendMessage(conversationId: string, question: string, limit = 5): Observable<SendMessageResult> {
    // Synchronous Non-Streaming Fallback:
    // Used when client does not support SSE streams or for automated testing assertions.
    return this.http
      .post<ApiResponse<SendMessageResult>>(`${this.baseUrl}/${conversationId}/messages`, { question, limit })
      .pipe(map(r => r.data));
  }

  /** Permanently delete a conversation and all its messages. */
  deleteConversation(id: string): Observable<void> {
    // BOLA-Protected Thread Purge:
    // Cascades deletion of messages, citations, and public share links.
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Submit positive (1) or negative (2) feedback for an assistant message. */
  submitFeedback(conversationId: string, messageId: string, rating: 1 | 2, comment?: string): Observable<void> {
    // RAG Evaluation Telemetry:
    // Records thumbs up/down rating and optional feedback comment for model alignment and retrieval scoring analysis.
    return this.http.post<void>(`${this.baseUrl}/${conversationId}/messages/${messageId}/feedback`, { rating, comment });
  }

  /** Fetch AI-generated suggested questions based on documents available to the employee. */
  getSuggestions(): Observable<SuggestedQuestionDto[]> {
    // Contextual Followup Suggestions:
    // Calls Gemini with accessible document file names to generate dynamic starter prompt chips.
    return this.http
      .get<ApiResponse<SuggestedQuestionDto[]>>(`${environment.apiUrl}/api/suggestions`)
      .pipe(map(r => r.data));
  }

  /** Create or retrieve a shareable read-only link token for a conversation. */
  shareConversation(id: string): Observable<ConversationShareDto> {
    // Idempotent Share Link Generation:
    // Generates a cryptographically random 16-character token (~95 bits entropy) valid for 30 days.
    return this.http
      .post<ApiResponse<ConversationShareDto>>(`${this.baseUrl}/${id}/share`, {})
      .pipe(map(r => r.data));
  }

  /** Retrieve a read-only shared conversation by token. */
  getSharedConversation(token: string): Observable<SharedConversationDetailDto> {
    // Public Read-Only Access:
    // Anonymous endpoint returning sanitized thread history without mutating permissions.
    return this.http
      .get<ApiResponse<SharedConversationDetailDto>>(`${this.baseUrl}/shared/${token}`)
      .pipe(map(r => r.data));
  }
}
