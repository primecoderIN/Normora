import { Injectable, inject } from '@angular/core';
import { ApiMessages } from '@core/constants/api-messages';
import { ActionCardDto, MessageCitationDto } from './conversation.service';
import { UserService } from './user.service';
import { environment } from '@env/environment';

export interface StreamOptions {
  conversationId: string;
  question: string;
  limit?: number;
  tenantId?: string | null;
}

export interface StreamStatusPayload {
  stage: string;
  message: string;
  toolName?: string;
  detail?: string;
}

export interface StreamCitationsPayload {
  assistantMessageId: string;
  sources: MessageCitationDto[];
}

export interface StreamToolStartedPayload {
  toolCallId?: string;
  toolName?: string;
  label?: string;
}

export interface StreamToolCompletedPayload {
  toolCallId: string;
  toolName?: string;
  summary?: string;
  success?: boolean;
  changedData?: boolean;
  structuredData?: unknown;
}

export interface StreamCallbacks {
  onStatus?: (status: StreamStatusPayload) => void;
  onCitations?: (data: StreamCitationsPayload) => void;
  onToolStarted?: (tool: StreamToolStartedPayload) => void;
  onToolCompleted?: (tool: StreamToolCompletedPayload) => void;
  onToken?: (text: string) => void;
  onActionCard?: (card: ActionCardDto) => void;
  onDone?: () => void;
  onError?: (errMessage: string) => void;
}

/**
 * Centralized, DRY Server-Sent Events (SSE) streaming service for AI conversational responses.
 * Reusable across Copilot, full-screen conversation threads, and embedded widgets.
 */
@Injectable({ providedIn: 'root' })
export class ConversationStreamService {
  private userService = inject(UserService);
  private readonly baseUrl = `${environment.apiUrl}/api/conversations`;

  async streamMessage(options: StreamOptions, callbacks: StreamCallbacks): Promise<void> {
    const tenantId = options.tenantId || this.userService.activeTenantId() || this.userService.activeTenant()?.tenantId;

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      'X-CSRF': '1'
    };
    if (tenantId) {
      headers['X-Tenant-Id'] = tenantId;
    }

    const response = await fetch(`${this.baseUrl}/${options.conversationId}/messages/stream`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ question: options.question, limit: options.limit ?? 5 })
    });

    if (!response.ok) {
      if (response.status === 429) {
        throw new Error(ApiMessages.CopilotRateLimited);
      }
      if (response.status === 403) {
        throw new Error(ApiMessages.Forbidden.detail);
      }
      throw new Error(ApiMessages.CopilotStreamFailed);
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error(ApiMessages.CopilotStreamFailed);
    }

    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue;
        const jsonStr = line.substring(6).trim();
        if (!jsonStr) continue;

        try {
          const evt = JSON.parse(jsonStr);

          switch (evt.type) {
            case 'status':
              callbacks.onStatus?.({
                stage: evt.stage,
                message: evt.message,
                toolName: evt.toolName,
                detail: evt.detail
              });
              break;

            case 'citations':
              callbacks.onCitations?.({
                assistantMessageId: evt.assistantMessageId,
                sources: (evt.sources ?? []).map((s: {
                  documentId: string;
                  documentChunkId?: string;
                  fileName: string;
                  score: number;
                  isOutdated?: boolean;
                }) => ({
                  documentId: s.documentId,
                  documentChunkId: s.documentChunkId || '',
                  fileName: s.fileName,
                  score: s.score,
                  isOutdated: Boolean(s.isOutdated)
                }))
              });
              break;

            case 'tool_started':
              callbacks.onToolStarted?.({
                toolCallId: evt.toolCallId,
                toolName: evt.toolName,
                label: evt.label
              });
              break;

            case 'tool_completed':
            case 'tool_finished':
              callbacks.onToolCompleted?.({
                toolCallId: evt.toolCallId,
                toolName: evt.toolName,
                summary: evt.summary,
                success: evt.success !== undefined ? evt.success : true,
                changedData: evt.changedData,
                structuredData: evt.structuredData
              });
              break;

            case 'token':
            case 'text':
              callbacks.onToken?.(evt.text || evt.token || '');
              break;

            case 'action_card':
              callbacks.onActionCard?.({
                actionType: evt.actionType,
                cardType: evt.cardType || evt.actionType,
                title: evt.title,
                description: evt.description,
                parameters: evt.parameters || {},
                confirmButtonText: evt.confirmButtonText,
                cancelButtonText: evt.cancelButtonText,
                approvalId: evt.approvalId
              });
              break;

            case 'done':
            case 'finished':
              callbacks.onDone?.();
              break;

            case 'error':
              callbacks.onError?.(evt.message || ApiMessages.ServerError.detail);
              break;
          }
        } catch {
          // ignore malformed SSE line chunk
        }
      }
    }
  }
}
