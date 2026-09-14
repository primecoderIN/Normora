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

@Component({
  selector: 'app-conversations',
  standalone: true,
  imports: [ConversationSidebarComponent, ConversationChatComponent],
  template: `
    <div class="flex h-full min-h-0 bg-surface-50 rounded-xl border border-surface-200 shadow-sm overflow-hidden">
      <!-- Sidebar -->
      <app-conversation-sidebar
        class="hidden md:flex"
        [conversations]="sortedConversations()"
        [activeId]="activeId()"
        [isLoading]="isLoadingList()"
        [error]="listError()"
        [deletingId]="isDeletingId()"
        (onSelect)="selectConversation($event)"
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
      />
    </div>
  `,
})
export class Conversations implements OnInit, AfterViewChecked {
  private conversationService = inject(ConversationService);
  @ViewChild(ConversationChatComponent) private chatComponent!: ConversationChatComponent;

  // ─── State ────────────────────────────────────────────────────────────────

  conversations = signal<ConversationDto[]>([]);
  activeConversation = signal<ConversationDetailDto | null>(null);
  
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
      id: '', // Blank ID indicates it hasn't been created on the backend yet
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

  // ─── Send message ────────────────────────────────────────────────────────

  async sendMessage(text: string) {
    let conv = this.activeConversation();
    if (!conv || this.isSending()) return;

    this.isSending.set(true);
    this.chatError.set('');

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

    this.conversationService.sendMessage(conv.id, text).subscribe({
      next: result => {
        const assistantMsg: MessageDto = {
          id: result.assistantMessageId,
          role: 'Assistant',
          content: result.answer,
          createdAt: new Date().toISOString(),
          rewritten: false,
          citations: result.sources.map(s => ({
            documentId: s.documentId,
            documentChunkId: '',
            fileName: s.fileName,
            score: s.score,
          })),
        };

        this.activeConversation.update(c => c ? { ...c, messages: [...c.messages, assistantMsg] } : c);

        // Refresh conversation list to get updated title / lastMessageAt
        this.loadConversations();

        this.isSending.set(false);
        this.shouldScrollToBottom = true;
      },
      error: err => {
        this.chatError.set(err?.error?.message || 'Something went wrong. Please try again.');
        // Remove optimistic user message on failure
        this.activeConversation.update(c =>
          c ? { ...c, messages: c.messages.filter(m => m.id !== optimisticUser.id) } : c
        );
        this.isSending.set(false);
      },
    });
  }
}
