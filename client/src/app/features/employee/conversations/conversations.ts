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

@Component({
  selector: 'app-conversations',
  standalone: true,
  imports: [ConversationSidebarComponent, ConversationChatComponent],
  template: `
    <div class="flex h-full min-h-0 bg-surface-50 rounded-xl border border-surface-200 shadow-sm overflow-hidden relative">
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
          this.activeConversation.set(null);
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
    this.chatError.set('');
    this.lastSentText = text;

    // Seamless start: if it's a blank slate, create it first
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

    // Optimistically add the user message
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
      const token = await new Promise<string>((resolve) => {
        this.conversationService.getAccessToken().subscribe(resolve);
      });

      const currentUser = this.userService.currentUser();
      const tenantId = currentUser?.memberships?.[0]?.tenantId;

      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      };
      if (tenantId) {
        headers['X-Tenant-Id'] = tenantId;
      }

      const response = await fetch(`${this.conversationService.baseUrl}/${conversationId}/messages/stream`, {
        method: 'POST',
        headers,
        body: JSON.stringify({ question: text, limit: 5 })
      });

      if (!response.ok) {
        throw new Error('Network response was not ok');
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
                    citations: evt.sources.map((s: any) => ({
                      documentId: s.documentId,
                      documentChunkId: '',
                      fileName: s.fileName,
                      score: s.score
                    }))
                  };
                  this.activeConversation.update(c => c ? { ...c, messages: [...c.messages, assistantMsg!] } : c);
                  this.shouldScrollToBottom = true;
                } else if (evt.type === 'text') {
                  if (assistantMsg) {
                    assistantMsg.content += evt.text;
                    this.activeConversation.update(c => {
                      if (!c) return c;
                      const newMessages = [...c.messages];
                      const idx = newMessages.findIndex(m => m.id === assistantMsg!.id);
                      if (idx >= 0) newMessages[idx] = { ...assistantMsg! };
                      return { ...c, messages: newMessages };
                    });
                    this.shouldScrollToBottom = true;
                  }
                } else if (evt.type === 'finished') {
                  // Done
                }
              } catch (e) {
                console.error('Failed to parse SSE chunk', e);
              }
            }
          }
        }
      }

      // Refresh list for updated title
      this.loadConversations();
    } catch (err: any) {
      this.chatError.set(err?.message || 'Something went wrong while streaming the response.');
      this.activeConversation.update(c =>
        c ? { ...c, messages: c.messages.filter(m => m.id !== optimisticUser.id) } : c
      );
    } finally {
      this.isSending.set(false);
    }
  }
}
