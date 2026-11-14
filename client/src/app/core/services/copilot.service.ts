import { Injectable, inject, signal, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { firstValueFrom, filter } from 'rxjs';
import { ConversationService, MessageDto, ConversationDto } from './conversation.service';
import { UserService } from './user.service';
import { RealtimeStateService } from './realtime-state.service';
import { ApiMessages } from '../constants/api-messages';

export type PersonaId = 'general' | 'hr' | 'finance' | 'it';

export interface AgentPersona {
  id: PersonaId;
  name: string;
  roleTitle: string;
  icon: string;
  colorClass: string;
  badgeClass: string;
  description: string;
  suggestedPrompts: string[];
}

export const AGENT_PERSONAS: AgentPersona[] = [
  {
    id: 'general',
    name: 'All-Rounder',
    roleTitle: 'Workplace Copilot',
    icon: 'pi pi-sparkles',
    colorClass: 'text-primary-600 dark:text-primary-400',
    badgeClass: 'bg-primary-500/10 text-primary-600 dark:text-primary-400 border-primary-500/20',
    description: 'Comprehensive enterprise knowledge assistant across all policies and systems.',
    suggestedPrompts: [
      'What is our company travel and hotel reimbursement cap?',
      'What are the guidelines for remote work and equipment?',
      'How do I escalate an inquiry to HR administration?'
    ]
  },
  {
    id: 'hr',
    name: 'HR & People Ops',
    roleTitle: 'People Ops Specialist',
    icon: 'pi pi-users',
    colorClass: 'text-indigo-600 dark:text-indigo-400',
    badgeClass: 'bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border-indigo-500/20',
    description: 'Expert on employee benefits, paid leave, wellness stipends, and conflict escalation.',
    suggestedPrompts: [
      'What are our parental, sick, and bereavement leave entitlements?',
      'How do I submit an ergonomic workspace allowance request?',
      'Escalate an inquiry regarding confidential HR administration'
    ]
  },
  {
    id: 'finance',
    name: 'Finance & Travel',
    roleTitle: 'Finance Specialist',
    icon: 'pi pi-wallet',
    colorClass: 'text-emerald-600 dark:text-emerald-400',
    badgeClass: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20',
    description: 'Expert on per-diem caps, expense reports, corporate card rules, and invoices.',
    suggestedPrompts: [
      'Calculate travel reimbursement limit for 4 nights in a Metro city',
      'What receipts are required for client dinner entertainment expenses?',
      'What are the submission deadlines for monthly expense reports?'
    ]
  },
  {
    id: 'it',
    name: 'IT & Security',
    roleTitle: 'Security Specialist',
    icon: 'pi pi-shield',
    colorClass: 'text-amber-600 dark:text-amber-400',
    badgeClass: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20',
    description: 'Expert on password policies, device encryption, VPN, phishing alerts, and access keys.',
    suggestedPrompts: [
      'What are our company password strength and rotation policies?',
      'How do I request elevated GitHub and cloud repository access?',
      'How do I report a suspected phishing email or compromised credential?'
    ]
  }
];

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
  private realtimeStateService = inject(RealtimeStateService);

  // ─── Reactive Signals ────────────────────────────────────────────────────────
  readonly isOpen = signal<boolean>(false);
  readonly isExpanded = signal<boolean>(false);
  readonly showHistoryPane = signal<boolean>(false);
  readonly isSending = signal<boolean>(false);
  readonly isLoadingHistory = signal<boolean>(false);
  readonly activeConversationId = signal<string | null>(null);
  readonly conversations = signal<ConversationDto[]>([]);
  readonly messages = signal<MessageDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly currentRoute = signal<string>(this.router.url);
  readonly modalActuation = signal<string | null>(null);
  readonly activePersonaId = signal<PersonaId>('general');
  readonly personas = AGENT_PERSONAS;

  readonly activePersona = computed<AgentPersona>(() => {
    return this.personas.find(p => p.id === this.activePersonaId()) || this.personas[0];
  });

  setPersona(id: PersonaId): void {
    this.activePersonaId.set(id);
  }

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.currentRoute.set(event.urlAfterRedirects || event.url);
      });
  }

  // ─── Context Awareness (AG-UI Perception) ──────────────────────────────────
  readonly activeContext = computed<CopilotContext>(() => {
    if (this.activePersonaId() !== 'general') {
      const persona = this.activePersona();
      return {
        pageTitle: persona.roleTitle,
        pageCategory: 'general',
        suggestedPrompts: persona.suggestedPrompts
      };
    }

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
      this.error.set('Failed to load conversation history.');
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
      this.error.set('Could not delete conversation.');
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
    const commands = Array.isArray(path) ? path : [path];
    void this.router.navigate(commands, { queryParams });
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

      const questionPayload = this.activePersonaId() !== 'general'
        ? `[Persona: ${this.activePersona().roleTitle}] ${text}`
        : text;

      // Stream Server-Sent Events (SSE) from the backend
      const response = await fetch(`${this.conversationService.baseUrl}/${conversationId}/messages/stream`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ question: questionPayload, limit: 5 })
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
                    })),
                    tools: []
                  };
                } else if (evt.type === 'tool_started') {
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
                  }
                  if (!assistantMsg.tools) assistantMsg.tools = [];
                  const existing = assistantMsg.tools.find(t => t.toolCallId === evt.toolCallId);
                  if (existing) {
                    existing.status = 'running';
                  } else {
                    assistantMsg.tools = [
                      ...assistantMsg.tools,
                      {
                        toolName: evt.toolName,
                        toolCallId: evt.toolCallId,
                        status: 'running'
                      }
                    ];
                  }
                  this.messages.update(list => {
                    const exists = list.some(m => m.id === assistantMsg!.id);
                    if (!exists) return [...list, { ...assistantMsg! }];
                    return list.map(m => (m.id === assistantMsg!.id ? { ...assistantMsg! } : m));
                  });
                } else if (evt.type === 'tool_finished') {
                  if (assistantMsg) {
                    if (!assistantMsg.tools) assistantMsg.tools = [];
                    const existing = assistantMsg.tools.find(t => t.toolCallId === evt.toolCallId);
                    if (existing) {
                      existing.status = evt.success ? 'completed' : 'failed';
                      existing.summary = evt.summary;
                    } else {
                      assistantMsg.tools = [
                        ...assistantMsg.tools,
                        {
                          toolName: evt.toolName,
                          toolCallId: evt.toolCallId,
                          status: evt.success ? 'completed' : 'failed',
                          summary: evt.summary
                        }
                      ];
                    }
                    this.messages.update(list => {
                      const exists = list.some(m => m.id === assistantMsg!.id);
                      if (!exists) return [...list, { ...assistantMsg! }];
                      return list.map(m => (m.id === assistantMsg!.id ? { ...assistantMsg! } : m));
                    });
                  }
                } else if (evt.type === 'action_card') {
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
                  }
                  assistantMsg.actionCard = {
                    actionType: evt.actionType,
                    cardType: evt.actionType,
                    title: evt.title,
                    description: evt.description,
                    parameters: evt.parameters || {},
                    confirmButtonText: evt.confirmButtonText,
                    cancelButtonText: evt.cancelButtonText,
                    approvalId: evt.approvalId
                  };
                  this.messages.update(list => {
                    const exists = list.some(m => m.id === assistantMsg!.id);
                    if (!exists) return [...list, { ...assistantMsg! }];
                    return list.map(m => (m.id === assistantMsg!.id ? { ...assistantMsg! } : m));
                  });
                } else if (evt.type === 'state_snapshot') {
                  this.realtimeStateService.handleStateSnapshot(evt.dataVersion);
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
      this.loadConversations();
    }
  }
}

