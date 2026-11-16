import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  ViewChild,
  AfterViewChecked,
} from '@angular/core';
import {
  ConversationDetailDto,
  ConversationDto,
  ConversationService,
  MessageDto,
} from '@core/services/conversation.service';
import { ConversationSidebarComponent } from './components/conversation-sidebar.component';
import { ConversationChatComponent } from './components/conversation-chat.component';
import { UserService } from '@core/services/user.service';
import { CopilotService, AgentLiveStatus } from '@core/services/copilot.service';
import { RealtimeStateService } from '@core/services/realtime-state.service';
import { ConversationStreamService } from '@core/services/conversation-stream.service';

@Component({
  selector: 'app-conversations',
  standalone: true,
  imports: [ConversationSidebarComponent, ConversationChatComponent],
  template: `
    <div class="flex h-full min-h-0 bg-slate-50 dark:bg-slate-950 rounded-xl border border-slate-200 dark:border-slate-700 shadow-sm overflow-hidden relative">
      <!-- Mobile sidebar overlay -->
      @if (showMobileSidebar()) {
        <div class="absolute inset-0 z-20 bg-black/30 md:hidden" (click)="showMobileSidebar.set(false)"></div>
      }

      <!-- Sidebar -->
      <app-conversation-sidebar
        class="md:flex"
        [class.hidden]="!showMobileSidebar()"
        [class.absolute]="showMobileSidebar()"
        [class.z-30]="showMobileSidebar()"
        [class.h-full]="showMobileSidebar()"
        [conversations]="sortedConversations()"
        [activeId]="activeId()"
        [isLoading]="isLoadingList()"
        [error]="listError()"
        [deletingId]="isDeletingId()"
        (onSelect)="onSidebarSelect($event)"
        (onNewConversation)="startBlankConversation()"
        (onDelete)="deleteConversation($event)"
        (onLoadMore)="loadMoreConversations()"
      />

      <!-- Chat Area -->
      <app-conversation-chat
        class="flex flex-col flex-1 min-w-0"
        [conversation]="activeConversation()!"
        [isLoading]="isLoadingMessages()"
        [isSending]="isSending()"
        [currentLiveStatus]="currentLiveStatus()"
        [error]="chatError()"
        (onNewConversation)="startBlankConversation()"
        (onSendMessage)="sendMessage($event)"
        (onRetry)="retryLastMessage()"
        (onToggleSidebar)="showMobileSidebar.set(!showMobileSidebar())"
      />
    </div>
  `,
})
export class Conversations implements OnInit, AfterViewChecked {
  private conversationService = inject(ConversationService);
  private userService = inject(UserService);
  public copilotService = inject(CopilotService);
  private realtimeStateService = inject(RealtimeStateService);
  private streamService = inject(ConversationStreamService);
  @ViewChild(ConversationChatComponent) private chatComponent!: ConversationChatComponent;

  // ─── State ────────────────────────────────────────────────────────────────

  conversations = signal<ConversationDto[]>([]);
  activeConversation = signal<ConversationDetailDto | null>(null);
  showMobileSidebar = signal(false);
  
  // Pagination
  limit = 20;
  offset = 0;
  hasMore = true;
  isLoadingMore = false;

  isLoadingList = signal(false);
  isLoadingMessages = signal(false);
  isSending = signal(false);
  currentLiveStatus = signal<AgentLiveStatus | null>(null);
  isDeletingId = signal<string | null>(null);
  
  listError = signal('');
  chatError = signal('');

  private shouldScrollToBottom = false;
  private lastSentText = '';

  // ─── Computed ────────────────────────────────────────────────────────────

  sortedConversations = computed(() =>
    [...this.conversations()].sort(
      (a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime()
    )
  );

  activeId = computed(() => this.activeConversation()?.id || null);

  // ─── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit() {
    this.copilotService.openExpanded();
    this.startBlankConversation();
    this.loadConversations();
  }

  ngAfterViewChecked() {
    if (this.shouldScrollToBottom && this.chatComponent) {
      this.chatComponent.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  // ─── Conversation list ────────────────────────────────────────────────────

  loadConversations(append = false) {
    if (!append) {
      this.isLoadingList.set(true);
      this.offset = 0;
      this.hasMore = true;
    } else {
      if (!this.hasMore || this.isLoadingMore) return;
      this.isLoadingMore = true;
    }

    this.listError.set('');
    this.conversationService.getConversations(this.limit, this.offset).subscribe({
      next: list => {
        if (list.length < this.limit) {
          this.hasMore = false;
        }
        this.offset += list.length;
        
        if (append) {
          this.conversations.update(curr => [...curr, ...list]);
          this.isLoadingMore = false;
        } else {
          this.conversations.set(list);
          this.isLoadingList.set(false);
        }
      },
      error: () => {
        this.listError.set('Could not load conversations.');
        this.isLoadingList.set(false);
        this.isLoadingMore = false;
      },
    });
  }

  loadMoreConversations() {
    this.loadConversations(true);
  }

  onSidebarSelect(id: string) {
    this.showMobileSidebar.set(false);
    this.selectConversation(id);
  }

  selectConversation(id: string) {
    if (this.activeId() === id) return;
    this.chatError.set('');
    this.isLoadingMessages.set(true);
    
    this.conversationService.getConversation(id).subscribe({
      next: detail => {
        this.activeConversation.set(detail);
        this.isLoadingMessages.set(false);
        this.shouldScrollToBottom = true;
      },
      error: () => {
        this.chatError.set('Could not load this conversation.');
        this.isLoadingMessages.set(false);
      },
    });
  }

  startBlankConversation() {
    this.activeConversation.set({
      id: '',
      title: 'New conversation',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      lastMessageAt: new Date().toISOString(),
      messages: []
    });
    this.chatError.set('');
  }

  deleteConversation(id: string) {
    this.isDeletingId.set(id);
    this.conversationService.deleteConversation(id).subscribe({
      next: () => {
        this.conversations.update(list => list.filter(c => c.id !== id));
        if (this.activeId() === id) {
          this.startBlankConversation();
        }
        this.isDeletingId.set(null);
      },
      error: () => {
        this.isDeletingId.set(null);
      },
    });
  }

  // ─── Send message (streaming) ───────────────────────────────────────────

  retryLastMessage() {
    if (this.lastSentText) {
      this.chatError.set('');
      this.sendMessage(this.lastSentText);
    }
  }

  async sendMessage(text: string) {
    let conv = this.activeConversation();
    if (!conv || this.isSending()) return;

    this.isSending.set(true);
    this.currentLiveStatus.set({ stage: 'thinking', message: 'Thinking . . .' });
    this.chatError.set('');
    this.lastSentText = text;

    // JIT Lazy Conversation Provisioning:
    // When the user opens the page, they are presented with a blank slate (conv.id is empty).
    // Instead of creating an empty conversation in the database immediately, we wait until
    // the user actually sends their first message to create the conversation container.
    if (!conv.id) {
      try {
        const newConv = await new Promise<ConversationDto>((resolve, reject) => {
          this.conversationService.createConversation().subscribe({ next: resolve, error: reject });
        });
        conv = { ...newConv, messages: [] };
        this.activeConversation.set(conv);
        this.conversations.update(list => [newConv, ...list]);
      } catch (err) {
        this.chatError.set('Could not start conversation.');
        this.isSending.set(false);
        return;
      }
    }

    const conversationId = conv.id;

    // Optimistic UI State Update:
    // Generate a temporary client-side GUID (crypto.randomUUID()) and append the user message
    // immediately to the active conversation Signal so the chat bubble appears with zero perceived latency.
    const optimisticUser: MessageDto = {
      id: crypto.randomUUID(),
      role: 'User',
      content: text,
      createdAt: new Date().toISOString(),
      rewritten: false,
      citations: [],
    };
    
    this.activeConversation.update(c => c ? { ...c, messages: [...c.messages, optimisticUser] } : c);
    this.shouldScrollToBottom = true;

    try {
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
          this.activeConversation.update(c => c ? { ...c, messages: [...c.messages, { ...assistantMsg! }] } : c);
          this.shouldScrollToBottom = true;
        }
      };

      const syncAssistantMsg = () => {
        if (assistantMsg) {
          this.activeConversation.update(c => {
            if (!c) return c;
            const exists = c.messages.some(m => m.id === assistantMsg!.id);
            if (!exists) return { ...c, messages: [...c.messages, { ...assistantMsg! }] };
            return { ...c, messages: c.messages.map(m => (m.id === assistantMsg!.id ? { ...assistantMsg! } : m)) };
          });
          this.shouldScrollToBottom = true;
        }
      };

      await this.streamService.streamMessage(
        {
          conversationId,
          question: text,
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
            if (tool.changedData) {
              this.realtimeStateService.notifyDataChanged();
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
              parameters: card.parameters || {},
              confirmButtonText: card.confirmButtonText,
              cancelButtonText: card.cancelButtonText,
              approvalId: card.approvalId
            };
            syncAssistantMsg();
          },
          onToken: (chunk) => {
            ensureAssistantMsg();
            this.currentLiveStatus.set(null);
            assistantMsg!.content += chunk;
            syncAssistantMsg();
          },
          onDone: () => {
            this.currentLiveStatus.set(null);
          },
          onError: (errMsg) => {
            this.chatError.set(errMsg);
          }
        }
      );

      // Re-fetch conversation roster to synchronize the auto-generated title
      this.loadConversations();
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : 'Something went wrong while streaming the response.';
      this.chatError.set(errorMsg);
      this.activeConversation.update(c =>
        c ? { ...c, messages: c.messages.filter(m => m.id !== optimisticUser.id) } : c
      );
    } finally {
      this.isSending.set(false);
      this.currentLiveStatus.set(null);
    }
  }
}
