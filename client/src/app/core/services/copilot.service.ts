import { Injectable, inject, signal, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { firstValueFrom, filter } from 'rxjs';
import { ConversationService, MessageDto } from './conversation.service';
import { UserService } from './user.service';
import { ApiMessages } from '../constants/api-messages';

export interface CopilotContext {
  pageTitle: string;
  pageCategory: 'documents' | 'departments' | 'user-groups' | 'dashboard' | 'employees' | 'settings' | 'general';
  suggestedPrompts: string[];
}

/**
 * Service managing state, context awareness, and streaming for the omnipresent Copilot sidebar.
 * Implements the AG-UI / CopilotKit architectural pattern by binding active routing context
 * and workspace identity directly to conversational assistant threads.
 */
@Injectable({
  providedIn: 'root'
})
export class CopilotService {
  private router = inject(Router);
  private conversationService = inject(ConversationService);
  private userService = inject(UserService);

  // ─── Reactive Signals ────────────────────────────────────────────────────────
  readonly isOpen = signal<boolean>(false);
  readonly isSending = signal<boolean>(false);
  readonly activeConversationId = signal<string | null>(null);
  readonly messages = signal<MessageDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly currentRoute = signal<string>(this.router.url);

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.currentRoute.set(event.urlAfterRedirects || event.url);
      });
  }

  // ─── Context Awareness (AG-UI Perception) ──────────────────────────────────
  readonly activeContext = computed<CopilotContext>(() => {
    const url = this.currentRoute().toLowerCase();

    if (url.includes('/employer/documents')) {
      return {
        pageTitle: 'Document Management',
        pageCategory: 'documents',
        suggestedPrompts: [
          'What remote work and expense policies are uploaded?',
          'Which documents have inactive or outdated versions?',
          'How do department clearances affect document visibility?'
        ]
      };
    }

    if (url.includes('/employer/departments') || url.includes('/employer/user-groups')) {
      return {
        pageTitle: 'Access Control & Departments',
        pageCategory: 'departments',
        suggestedPrompts: [
          'How do user groups map to department clearances?',
          'Which departments have access to restricted policies?',
          'Explain our role-based authorization model'
        ]
      };
    }

    if (url.includes('/employer/dashboard')) {
      return {
        pageTitle: 'Admin Overview',
        pageCategory: 'dashboard',
        suggestedPrompts: [
          'Summarize our workspace knowledge base status',
          'What policies are most frequently referenced?',
          'Which departments have documents uploaded?'
        ]
      };
    }

    if (url.includes('/employer/employees')) {
      return {
        pageTitle: 'Employee Directory',
        pageCategory: 'employees',
        suggestedPrompts: [
          'How do I invite a new employee with department access?',
          'What roles exist in this workspace?',
          'How does member deactivation and soft-delete work?'
        ]
      };
    }

    if (url.includes('/employer/settings')) {
      return {
        pageTitle: 'Workspace Settings',
        pageCategory: 'settings',
        suggestedPrompts: [
          'What are our workspace plan quotas and seat limits?',
          'How do dynamic white-label branding styles apply?',
          'What actions are recorded in the workspace audit log?'
        ]
      };
    }

    return {
      pageTitle: 'Knowledge Assistant',
      pageCategory: 'general',
      suggestedPrompts: [
        'What is our company travel and hotel reimbursement cap?',
        'What are the guidelines for remote work and equipment?',
        'How do I escalate an inquiry to HR administration?'
      ]
    };
  });

  // Determines if the user is currently on the full-screen conversations page
  readonly isFullPageConversation = computed<boolean>(() => {
    return this.currentRoute().includes('/conversations');
  });

  // ─── Actions ───────────────────────────────────────────────────────────────
  toggle(): void {
    this.isOpen.update(v => !v);
  }

  open(): void {
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
  }

  resetConversation(): void {
    this.activeConversationId.set(null);
    this.messages.set([]);
    this.error.set(null);
  }

  async sendMessage(prompt: string): Promise<void> {
    const text = prompt.trim();
    if (!text || this.isSending()) return;

    this.error.set(null);
    this.isSending.set(true);

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
      const activeTenant = this.userService.activeTenant();
      const tenantId = this.userService.activeTenantId() || activeTenant?.tenantId;

      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'X-CSRF': '1'
      };
      if (tenantId) {
        headers['X-Tenant-Id'] = tenantId;
      }

      // Stream Server-Sent Events (SSE) from the backend
      const response = await fetch(`${this.conversationService.baseUrl}/${conversationId}/messages/stream`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ question: text, limit: 5 })
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
      const decoder = new TextDecoder();

      let assistantMsg: MessageDto | null = null;
      let buffer = '';

      if (reader) {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n\n');
          buffer = lines.pop() || '';

          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const jsonStr = line.substring(6);
              try {
                const evt = JSON.parse(jsonStr);

                if (evt.type === 'citations') {
                  assistantMsg = {
                    id: evt.assistantMessageId,
                    role: 'Assistant',
                    content: '',
                    createdAt: new Date().toISOString(),
                    rewritten: false,
                    citations: (evt.sources ?? []).map((s: { documentId: string; documentChunkId?: string; fileName: string; score: number; isOutdated?: boolean }) => ({
                      documentId: s.documentId,
                      documentChunkId: s.documentChunkId || '',
                      fileName: s.fileName,
                      score: s.score,
                      isOutdated: Boolean(s.isOutdated)
                    }))
                  };
                } else if (evt.type === 'text') {
                  if (assistantMsg) {
                    assistantMsg.content += evt.text;
                    this.messages.update(list => {
                      const exists = list.some(m => m.id === assistantMsg!.id);
                      if (!exists) {
                        return [...list, { ...assistantMsg! }];
                      }
                      return list.map(m => (m.id === assistantMsg!.id ? { ...assistantMsg! } : m));
                    });
                  }
                }
              } catch {
                // Ignore partial JSON parse errors between chunks
              }
            }
          }
        }
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : ApiMessages.CopilotStreamFailed;
      this.error.set(message);
    } finally {
      this.isSending.set(false);
    }
  }
}

