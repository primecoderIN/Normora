import { Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { ConversationDetailDto, MessageDto, ConversationService, SuggestedQuestionDto } from '@core/services/conversation.service';
import { SavedAnswerService } from '@core/services/saved-answer.service';
import { MessageService } from 'primeng/api';
import { ApiMessages } from '@core/constants/api-messages';
import { ChatMessageBubbleComponent } from './chat-message-bubble.component';
import { ChatInputBarComponent } from './chat-input-bar.component';
import { ChunkPreviewDrawerComponent } from '@shared/components/chunk-preview-drawer/chunk-preview-drawer.component';

/**
 * Component responsible solely for orchestrating the active conversation chat interface:
 * suggestion prompts, chat header actions, message feed delegation, typing indicators,
 * and coordinating with specialized child components.
 */
@Component({
  selector: 'app-conversation-chat',
  standalone: true,
  imports: [
    CommonModule,
    TooltipModule,
    ChatMessageBubbleComponent,
    ChatInputBarComponent,
    ChunkPreviewDrawerComponent,
  ],
  templateUrl: './conversation-chat.component.html',
})
export class ConversationChatComponent implements OnInit {
  private savedAnswerService = inject(SavedAnswerService);
  private conversationService = inject(ConversationService);
  private messageService = inject(MessageService);

  @Input({ required: true }) conversation: ConversationDetailDto | null = null;
  @Input({ required: true }) isLoading = false;
  @Input({ required: true }) isSending = false;
  @Input({ required: true }) error = '';

  @Output() onNewConversation = new EventEmitter<void>();
  @Output() onSendMessage = new EventEmitter<string>();
  @Output() onRetry = new EventEmitter<void>();
  @Output() onToggleSidebar = new EventEmitter<void>();

  @ViewChild('messagesEnd') private messagesEnd!: ElementRef<HTMLDivElement>;
  @ViewChild('chatInputBar') private chatInputBar!: ChatInputBarComponent;
  @ViewChild('chunkDrawer') private chunkDrawer!: ChunkPreviewDrawerComponent;

  suggestions = signal<SuggestedQuestionDto[]>([]);
  isLoadingSuggestions = signal<boolean>(false);
  isSharing = signal<boolean>(false);

  /** Set of message IDs that the current user has saved. */
  private savedMessageIds = signal(new Set<string>());

  private readonly defaultSuggestions: SuggestedQuestionDto[] = [
    { question: 'How do I request time off?', iconClass: 'pi-file' },
    { question: 'What is our remote work policy?', iconClass: 'pi-users' },
    { question: 'How do I submit expenses?', iconClass: 'pi-wallet' }
  ];

  ngOnInit(): void {
    this.loadSuggestions();
  }

  loadSuggestions(): void {
    this.isLoadingSuggestions.set(true);
    this.conversationService.getSuggestions().subscribe({
      next: (data) => {
        if (data && data.length > 0) {
          this.suggestions.set(data);
        } else {
          this.suggestions.set(this.defaultSuggestions);
        }
        this.isLoadingSuggestions.set(false);
      },
      error: () => {
        this.suggestions.set(this.defaultSuggestions);
        this.isLoadingSuggestions.set(false);
      }
    });
  }

  selectSuggestion(questionText: string): void {
    this.chatInputBar?.setValue(questionText);
  }

  shareConversation(): void {
    if (!this.conversation?.id || this.isSharing()) return;
    this.isSharing.set(true);
    this.conversationService.shareConversation(this.conversation.id).subscribe({
      next: (res) => {
        this.isSharing.set(false);
        const match = window.location.pathname.match(/\/app\/workspaces\/[^/]+/);
        const workspacePath = match ? match[0] : '';
        const shareUrl = `${window.location.origin}${workspacePath}/employee/conversations/shared/${res.token}`;

        navigator.clipboard.writeText(shareUrl).then(() => {
          this.messageService.add({
            severity: 'success',
            summary: ApiMessages.ShareLinkCopied.summary,
            detail: ApiMessages.ShareLinkCopied.detail
          });
        }).catch(() => {
          this.messageService.add({
            severity: 'info',
            summary: ApiMessages.ShareLinkCopied.summary,
            detail: shareUrl
          });
        });
      },
      error: () => {
        this.isSharing.set(false);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.ShareLinkFailed.summary,
          detail: ApiMessages.ShareLinkFailed.detail
        });
      }
    });
  }

  isSaved(messageId: string): boolean {
    return this.savedMessageIds().has(messageId);
  }

  toggleSave(messageId: string): void {
    if (this.isSaved(messageId)) {
      this.savedMessageIds.update(s => {
        const next = new Set(s);
        next.delete(messageId);
        return next;
      });
      this.savedAnswerService.unsaveAnswer(messageId).subscribe({
        error: () => this.savedMessageIds.update(s => new Set(s).add(messageId))
      });
    } else {
      this.savedMessageIds.update(s => new Set(s).add(messageId));
      this.savedAnswerService.saveAnswer(messageId).subscribe({
        error: () => this.savedMessageIds.update(s => {
          const next = new Set(s);
          next.delete(messageId);
          return next;
        })
      });
    }
  }

  toggleFeedback(event: { message: MessageDto; rating: 1 | 2 }): void {
    const { message, rating } = event;
    const prevRating = message.feedback;
    message.feedback = rating; // Optimistic update
    
    if (this.conversation?.id) {
      this.conversationService.submitFeedback(this.conversation.id, message.id, rating, undefined).subscribe({
        error: () => {
          message.feedback = prevRating; // Revert on error
        }
      });
    }
  }

  openPreview(chunkId: string): void {
    this.chunkDrawer.open(chunkId);
  }

  scrollToBottom(): void {
    try {
      this.messagesEnd?.nativeElement?.scrollIntoView({ behavior: 'smooth' });
    } catch {}
  }

  showComingSoon(): void {
    this.messageService.add({
      severity: 'info',
      summary: ApiMessages.ComingSoon.summary,
      detail: ApiMessages.ComingSoon.detail
    });
  }
}
