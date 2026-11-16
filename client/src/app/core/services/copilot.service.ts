import { Injectable, inject, signal, computed } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiMessages } from '@core/constants/api-messages';
import {
  ConversationService,
  ConversationDto,
  MessageDto,
  ActionCardDto
} from './conversation.service';
import { UserService } from './user.service';
import { WorkspaceRouteService } from './workspace-route.service';
import { RealtimeStateService } from './realtime-state.service';
import {
  CopilotContextService,
  PersonaId,
  AgentPersona,
  AGENT_PERSONAS,
  CopilotContext
} from './copilot-context.service';
import { ConversationStreamService } from './conversation-stream.service';

export type { PersonaId, AgentPersona, CopilotContext } from './copilot-context.service';
export { AGENT_PERSONAS } from './copilot-context.service';

export interface AgentLiveStatus {
  stage: 'thinking' | 'searching' | 'grounding' | 'answering' | 'tool_calling' | 'tool_evaluating';
  message: string;
  toolName?: string;
  detail?: string;
}

/**
 * Service managing state and orchestrating actions for the omnipresent Copilot sidebar.
 * Implements the AG-UI / CopilotKit architectural pattern by binding active routing context
 * and workspace identity directly to conversational assistant threads.
 */
@Injectable({
  providedIn: 'root'
})
export class CopilotService {
  private conversationService = inject(ConversationService);
  private userService = inject(UserService);
  private realtimeStateService = inject(RealtimeStateService);
  private workspaceRouteService = inject(WorkspaceRouteService);
  private contextService = inject(CopilotContextService);
  private streamService = inject(ConversationStreamService);

  // ─── Reactive Signals ────────────────────────────────────────────────────────
  readonly isOpen = signal<boolean>(false);
  readonly isExpanded = signal<boolean>(false);
  readonly showHistoryPane = signal<boolean>(false);
  readonly isSending = signal<boolean>(false);
  readonly currentLiveStatus = signal<AgentLiveStatus | null>(null);
  readonly isLoadingHistory = signal<boolean>(false);
  readonly activeConversationId = signal<string | null>(null);
  readonly conversations = signal<ConversationDto[]>([]);
  readonly messages = signal<MessageDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly modalActuation = signal<string | null>(null);

  // ─── Delegated Context Perception ──────────────────────────────────────────
  readonly currentRoute = this.contextService.currentRoute;
  readonly activePersonaId = this.contextService.activePersonaId;
  readonly personas = this.contextService.personas;
  readonly activePersona = this.contextService.activePersona;
  readonly activeContext = this.contextService.activeContext;

  setPersona(id: PersonaId): void {
    this.contextService.setPersona(id);
  }

  // Determines if the user is currently on the full-screen conversations page
  readonly isFullPageConversation = computed<boolean>(() => {
    return this.currentRoute().includes('/conversations');
  });

  // ─── Actions ───────────────────────────────────────────────────────────────
  toggle(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen() && this.conversations().length === 0) {
      this.loadConversations();
    }
  }

  open(): void {
    this.isOpen.set(true);
    if (this.conversations().length === 0) {
      this.loadConversations();
    }
  }

  openExpanded(): void {
    this.isOpen.set(true);
    this.isExpanded.set(true);
    this.loadConversations();
  }

  toggleExpanded(): void {
    this.isExpanded.update(v => !v);
  }

  toggleHistoryPane(): void {
    this.showHistoryPane.update(v => !v);
  }

  close(): void {
    this.isOpen.set(false);
  }

  resetConversation(): void {
    this.activeConversationId.set(null);
    this.messages.set([]);
    this.error.set(null);
  }

  async selectConversation(id: string): Promise<void> {
    if (this.activeConversationId() === id) return;
    this.isLoadingHistory.set(true);
    this.error.set(null);
    try {
      const detail = await firstValueFrom(this.conversationService.getConversation(id));
      this.activeConversationId.set(detail.id);
      this.messages.set(detail.messages || []);
    } catch {
      this.error.set(ApiMessages.CopilotStreamFailed);
    } finally {
      this.isLoadingHistory.set(false);
    }
  }

  async loadConversations(): Promise<void> {
    this.isLoadingHistory.set(true);
    try {
      const list = await firstValueFrom(this.conversationService.getConversations(50, 0));
      this.conversations.set(list || []);
    } catch {
      // Non-critical background retrieval
    } finally {
      this.isLoadingHistory.set(false);
    }
  }

  async deleteConversation(id: string): Promise<void> {
    try {
      await firstValueFrom(this.conversationService.deleteConversation(id));
      this.conversations.update(list => list.filter(c => c.id !== id));
      if (this.activeConversationId() === id) {
        this.resetConversation();
      }
    } catch {
      this.error.set(ApiMessages.DocumentDeleteFailed.detail);
    }
  }

  // ─── Agent UI Actuation ───────────────────────────────────────────────────
  triggerModal(modalName: string): void {
    this.modalActuation.set(modalName);
  }

  clearModalActuation(): void {
    this.modalActuation.set(null);
  }

  navigateActuation(path: string | any[], queryParams?: Record<string, any>): void {
    if (this.isExpanded()) {
      this.isExpanded.set(false);
    }
    const target = Array.isArray(path) ? path.join('/') : (path || '');
    this.workspaceRouteService.resolveAndNavigate(target, queryParams);
  }

  async sendMessage(prompt: string): Promise<void> {
    const text = prompt.trim();
    if (!text || this.isSending()) return;

    this.error.set(null);
    this.isSending.set(true);
    this.currentLiveStatus.set({ stage: 'thinking', message: 'Thinking . . .' });

    // Ensure an active conversation thread exists
    let conversationId = this.activeConversationId();
    if (!conversationId) {
      try {
        const shell = await firstValueFrom(this.conversationService.createConversation());
        conversationId = shell.id;
        this.activeConversationId.set(conversationId);
      } catch {
        this.error.set(ApiMessages.CopilotStreamFailed);
        this.isSending.set(false);
        return;
      }
    }

    // Optimistic UI state update
    const optimisticUser: MessageDto = {
      id: crypto.randomUUID(),
      role: 'User',
      content: text,
      createdAt: new Date().toISOString(),
      rewritten: false,
      citations: []
    };

    this.messages.update(list => [...list, optimisticUser]);

    try {
      const questionPayload = this.activePersonaId() !== 'general'
        ? `[Persona: ${this.activePersona().roleTitle}] ${text}`
        : text;

      let assistantMsg: MessageDto | null = null;

      const ensureAssistantMsg = () => {
        if (!assistantMsg) {
          assistantMsg = {
            id: crypto.randomUUID(),
            role: 'Assistant',
            content: '',
            createdAt: new Date().toISOString(),
            rewritten: false,
            citations: [],
            tools: []
          };
          this.messages.update(list => [...list, { ...assistantMsg! }]);
        }
      };

      const syncAssistantMsg = () => {
        if (assistantMsg) {
          this.messages.update(list => {
            const exists = list.some(m => m.id === assistantMsg!.id);
            if (!exists) return [...list, { ...assistantMsg! }];
            return list.map(m => (m.id === assistantMsg!.id ? { ...assistantMsg! } : m));
          });
        }
      };

      await this.streamService.streamMessage(
        {
          conversationId,
          question: questionPayload,
          limit: 5
        },
        {
          onStatus: (status) => {
            this.currentLiveStatus.set({
              stage: status.stage as AgentLiveStatus['stage'],
              message: status.message,
              toolName: status.toolName,
              detail: status.detail
            });
          },
          onCitations: (data) => {
            assistantMsg = {
              id: data.assistantMessageId,
              role: 'Assistant',
              content: assistantMsg?.content || '',
              createdAt: new Date().toISOString(),
              rewritten: false,
              citations: data.sources,
              tools: assistantMsg?.tools || []
            };
            syncAssistantMsg();
          },
          onToolStarted: (tool) => {
            if (tool.label) {
              this.currentLiveStatus.set({
                stage: 'tool_calling',
                message: tool.label,
                toolName: tool.toolName
              });
            }
            ensureAssistantMsg();
            if (!assistantMsg!.tools) assistantMsg!.tools = [];
            const existing = assistantMsg!.tools.find(t => t.toolCallId === tool.toolCallId);
            if (existing) {
              existing.status = 'running';
              if (tool.label) existing.label = tool.label;
            } else {
              assistantMsg!.tools = [
                ...assistantMsg!.tools,
                {
                  toolName: tool.toolName || 'tool',
                  toolCallId: tool.toolCallId || crypto.randomUUID(),
                  status: 'running',
                  label: tool.label
                }
              ];
            }
            syncAssistantMsg();
          },
          onToolCompleted: (tool) => {
            if (tool.summary) {
              this.currentLiveStatus.set({
                stage: 'tool_evaluating',
                message: `Synthesizing results from ${tool.summary}...`,
                toolName: tool.toolName
              });
            }
            if (assistantMsg) {
              if (!assistantMsg.tools) assistantMsg.tools = [];
              const existing = assistantMsg.tools.find(t => t.toolCallId === tool.toolCallId);
              if (existing) {
                existing.status = tool.success ? 'completed' : 'failed';
                existing.summary = tool.summary;
              } else {
                assistantMsg.tools = [
                  ...assistantMsg.tools,
                  {
                    toolName: tool.toolName || 'tool',
                    toolCallId: tool.toolCallId,
                    status: tool.success ? 'completed' : 'failed',
                    summary: tool.summary
                  }
                ];
              }
              syncAssistantMsg();
            }
          },
          onActionCard: (card) => {
            ensureAssistantMsg();
            assistantMsg!.actionCard = {
              actionType: card.actionType,
              cardType: card.cardType || card.actionType,
              title: card.title,
              description: card.description,
              parameters: card.parameters,
              confirmButtonText: card.confirmButtonText,
              cancelButtonText: card.cancelButtonText,
              approvalId: card.approvalId
            };
            syncAssistantMsg();
          },
          onToken: (chunk) => {
            ensureAssistantMsg();
            this.currentLiveStatus.set({ stage: 'answering', message: 'Streaming response...' });
            assistantMsg!.content += chunk;
            syncAssistantMsg();
          },
          onDone: () => {
            this.currentLiveStatus.set(null);
          },
          onError: (errMsg) => {
            this.error.set(errMsg);
          }
        }
      );
    } catch (err: any) {
      this.error.set(err.message || ApiMessages.CopilotStreamFailed);
    } finally {
      this.isSending.set(false);
      this.currentLiveStatus.set(null);
    }
  }

  async confirmActionCardApproval(approvalId: string): Promise<boolean> {
    try {
      const res = await firstValueFrom(this.conversationService.confirmApproval(approvalId));
      if (res.data?.success && res.data?.changedData) {
        this.realtimeStateService.notifyDataChanged();
      }
      return res.data?.success ?? false;
    } catch {
      return false;
    }
  }

  async dismissActionCardApproval(approvalId: string): Promise<boolean> {
    try {
      const res = await firstValueFrom(this.conversationService.dismissApproval(approvalId));
      return res.success;
    } catch {
      return false;
    }
  }
}
