import { Component, ElementRef, EventEmitter, Input, Output, ViewChild, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { SidebarModule } from 'primeng/sidebar';
import { ConversationDetailDto, MessageDto, ConversationService } from '@core/services/conversation.service';
import { SavedAnswerService } from '@core/services/saved-answer.service';
import { DocumentService, DocumentChunkPreviewDto } from '@core/services/document.service';
import { MessageService } from 'primeng/api';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

@Component({
  selector: 'app-conversation-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, TooltipModule, DatePipe, SidebarModule],
  template: `
    <section class="flex flex-col flex-1 min-w-0 bg-white dark:bg-slate-900 overflow-hidden">
      
      @if (!conversation) {
        <!-- Empty State -->
        <div class="flex flex-col flex-1 items-center justify-center gap-6 p-8 text-center bg-slate-50 dark:bg-slate-950">
          <div>
            <div class="flex items-center justify-center w-20 h-20 bg-linear-to-br from-primary-100 to-purple-100 text-primary-600 text-3xl rounded-2xl mx-auto mb-4 shadow-sm">
              <i class="pi pi-comments"></i>
            </div>
            <h2 class="text-2xl font-bold text-slate-900 dark:text-white m-0 mb-1">How can I help you today?</h2>
            <p class="text-sm text-slate-500 dark:text-slate-400 max-w-sm mx-auto m-0">Ask anything about company policies, benefits, or your specific context.</p>
          </div>
          
          <!-- Suggestion Cards -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 max-w-3xl w-full mt-4">
            <!-- Card 1 -->
            <button type="button" class="flex flex-col text-left gap-3 p-4 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl hover:border-primary-300 hover:shadow-md transition-all cursor-pointer group" (click)="question = 'How do I request time off?'">
              <div class="w-10 h-10 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center group-hover:scale-110 transition-transform">
                <i class="pi pi-file text-lg"></i>
              </div>
              <div class="font-semibold text-slate-800 dark:text-slate-100 text-sm">How do I request time off?</div>
            </button>
            <!-- Card 2 -->
            <button type="button" class="flex flex-col text-left gap-3 p-4 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl hover:border-primary-300 hover:shadow-md transition-all cursor-pointer group" (click)="question = 'What is our remote work policy?'">
              <div class="w-10 h-10 rounded-lg bg-pink-50 text-pink-600 flex items-center justify-center group-hover:scale-110 transition-transform">
                <i class="pi pi-users text-lg"></i>
              </div>
              <div class="font-semibold text-slate-800 dark:text-slate-100 text-sm">What is our remote work policy?</div>
            </button>
            <!-- Card 3 -->
            <button type="button" class="flex flex-col text-left gap-3 p-4 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl hover:border-primary-300 hover:shadow-md transition-all cursor-pointer group" (click)="question = 'How do I submit expenses?'">
              <div class="w-10 h-10 rounded-lg bg-purple-50 text-purple-600 flex items-center justify-center group-hover:scale-110 transition-transform">
                <i class="pi pi-wallet text-lg"></i>
              </div>
              <div class="font-semibold text-slate-800 dark:text-slate-100 text-sm">How do I submit expenses?</div>
            </button>
          </div>
        </div>
      } @else {
        <!-- Chat Header -->
        <header class="flex items-center justify-between p-4 px-5 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-700 flex-none gap-4">
          <div class="flex items-center gap-3 min-w-0">
            <button
              type="button"
              class="flex md:hidden items-center justify-center flex-none w-9 h-9 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-600 dark:text-slate-300 cursor-pointer transition-colors"
              (click)="onToggleSidebar.emit()"
              aria-label="Toggle sidebar"
            >
              <i class="pi pi-bars text-base"></i>
            </button>
            <div class="flex items-center justify-center flex-none w-9 h-9 bg-primary-50 text-primary-600 rounded-lg text-sm">
              <i class="pi pi-comments"></i>
            </div>
            <div class="min-w-0">
              <div class="text-[0.95rem] font-bold text-slate-900 dark:text-white truncate">{{ conversation.title }}</div>
              <div class="text-[0.75rem] text-slate-400">
                {{ conversation.messages.length > 0 ? (conversation.messages.length + ' messages') : 'No messages yet' }}
              </div>
            </div>
          </div>
          <div class="hidden sm:flex items-center gap-2 px-3 py-1 bg-green-50 border border-green-200 rounded-full text-green-600 text-[0.68rem] font-bold tracking-widest uppercase whitespace-nowrap">
            <span class="block w-1.5 h-1.5 bg-green-500 rounded-full shadow-[0_0_0_2px_rgba(34,197,94,0.2)]"></span> Knowledge base online
          </div>
        </header>

        <!-- Messages Area -->
        <div class="flex-1 min-h-0 overflow-y-auto p-5 md:p-6 flex flex-col gap-5 custom-scrollbar" aria-live="polite">
          
          @if (isLoading) {
            <div class="flex flex-col gap-4">
              @for (n of [1,2,3]; track n) {
                <div class="flex" [class.justify-end]="n % 2 === 0">
                  <div class="w-7/12 h-14 bg-slate-100 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
                </div>
              }
            </div>
          } @else if (conversation.messages.length === 0) {
            <div class="flex flex-col flex-1 items-center justify-center gap-3 text-center text-slate-400">
              <div class="w-16 h-16 flex items-center justify-center bg-linear-to-br from-primary-50 to-purple-50 rounded-2xl">
                <i class="pi pi-sparkles text-3xl text-primary-300"></i>
              </div>
              <p class="text-sm m-0">Ask your first question below.</p>
            </div>
          } @else {
            @for (msg of conversation.messages; track msg.id; let i = $index) {
              <div class="flex items-end gap-2.5 max-w-[85%] md:max-w-[75%] msg-enter"
                   [class.self-end]="msg.role === 'User'"
                   [class.flex-row-reverse]="msg.role === 'User'"
                   [class.self-start]="msg.role === 'Assistant'">
                
                <!-- Avatar -->
                @if (msg.role === 'Assistant') {
                  <div class="flex items-center justify-center flex-none w-8 h-8 bg-linear-to-br from-primary-50 to-purple-50 text-primary-600 rounded-full text-base shadow-sm">
                    <i class="pi pi-sparkles"></i>
                  </div>
                }

                <div class="flex flex-col gap-1.5 min-w-0">
                  <!-- Bubble -->
                  <div class="group relative px-4 py-3 rounded-2xl wrap-break-word"
                       [class.bg-primary-600]="msg.role === 'User'"
                       [class.text-white]="msg.role === 'User'"
                       [class.rounded-br-sm]="msg.role === 'User'"
                       [class.bg-white]="msg.role === 'Assistant'" [class.dark:bg-slate-900]="msg.role === 'Assistant'"
                       [class.text-slate-800]="msg.role === 'Assistant'" [class.dark:text-slate-100]="msg.role === 'Assistant'"
                       [class.border]="msg.role === 'Assistant'"
                       [class.border-slate-200]="msg.role === 'Assistant'" [class.dark:border-slate-700]="msg.role === 'Assistant'"
                       [class.rounded-bl-sm]="msg.role === 'Assistant'"
                       [class.shadow-sm]="msg.role === 'Assistant'">
                    
                    @if (msg.role === 'User') {
                      <p class="text-[0.9rem] leading-relaxed whitespace-pre-wrap m-0">{{ msg.content }}</p>
                    } @else {
                      <div class="prose-normora text-[0.9rem] leading-relaxed" [innerHTML]="renderMarkdown(msg.content)"></div>
                    }

                    <!-- Action buttons for assistant messages -->
                    @if (msg.role === 'Assistant' && msg.content) {
                      <div class="absolute top-2 right-2 flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-all">
                        <!-- Bookmark button -->
                        <button
                          type="button"
                          class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-amber-500 transition-all cursor-pointer"
                          (click)="toggleSave(msg.id)"
                          [pTooltip]="isSaved(msg.id) ? 'Unsave' : 'Save answer'"
                          tooltipPosition="top"
                          [attr.aria-label]="isSaved(msg.id) ? 'Unsave answer' : 'Save answer'"
                        >
                          <i class="text-base pi"
                             [class.pi-bookmark-fill]="isSaved(msg.id)"
                             [class.text-amber-500]="isSaved(msg.id)"
                             [class.pi-bookmark]="!isSaved(msg.id)"></i>
                        </button>
                        <!-- Feedback buttons -->
                        <button
                          type="button"
                          class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-green-500 transition-all cursor-pointer"
                          (click)="toggleFeedback(msg, 1)"
                          pTooltip="Helpful"
                          tooltipPosition="top"
                          aria-label="Mark helpful"
                        >
                          <i class="text-base pi"
                             [class.pi-thumbs-up-fill]="msg.feedback === 1"
                             [class.text-green-500]="msg.feedback === 1"
                             [class.pi-thumbs-up]="msg.feedback !== 1"></i>
                        </button>
                        <button
                          type="button"
                          class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-red-500 transition-all cursor-pointer"
                          (click)="toggleFeedback(msg, 2)"
                          pTooltip="Not helpful"
                          tooltipPosition="top"
                          aria-label="Mark not helpful"
                        >
                          <i class="text-base pi"
                             [class.pi-thumbs-down-fill]="msg.feedback === 2"
                             [class.text-red-500]="msg.feedback === 2"
                             [class.pi-thumbs-down]="msg.feedback !== 2"></i>
                        </button>
                        <!-- Copy button -->
                        <button
                          type="button"
                          class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 transition-all cursor-pointer"
                          (click)="copyToClipboard(msg.content, msg.id)"
                          [pTooltip]="copiedId === msg.id ? 'Copied!' : 'Copy'"
                          tooltipPosition="top"
                          [attr.aria-label]="'Copy message'"
                        >
                          <i class="pi text-base" [class.pi-check]="copiedId === msg.id" [class.text-green-600]="copiedId === msg.id" [class.pi-copy]="copiedId !== msg.id"></i>
                        </button>
                      </div>
                    }
                  </div>

                  <!-- Citations -->
                  @if (msg.role === 'Assistant' && msg.citations.length > 0) {
                    <div class="flex flex-col gap-2">
                      <div class="flex flex-wrap items-center gap-1.5 px-1">
                        <span class="text-[0.68rem] font-bold text-slate-400 tracking-widest uppercase mr-1">Sources</span>
                        @for (cite of msg.citations; track cite.documentId + cite.fileName) {
                          <div class="inline-flex items-center gap-1.5 px-2.5 py-1 border rounded-full text-[0.72rem] font-medium max-w-40 cursor-pointer hover:border-primary-300 hover:shadow-sm transition-all"
                               [class.bg-slate-50]="!cite.isOutdated" [class.dark:bg-slate-950]="!cite.isOutdated" [class.border-slate-200]="!cite.isOutdated" [class.dark:border-slate-700]="!cite.isOutdated" [class.text-slate-600]="!cite.isOutdated" [class.dark:text-slate-300]="!cite.isOutdated"
                               [class.bg-amber-50]="cite.isOutdated" [class.dark:bg-amber-950/50]="cite.isOutdated" [class.border-amber-200]="cite.isOutdated" [class.dark:border-amber-900]="cite.isOutdated" [class.text-amber-700]="cite.isOutdated" [class.dark:text-amber-500]="cite.isOutdated"
                               [pTooltip]="cite.fileName" tooltipPosition="top"
                               (click)="openPreview(cite.documentChunkId)">
                            <i class="pi pi-file-pdf text-base flex-none" [class.text-slate-400]="!cite.isOutdated" [class.text-amber-500]="cite.isOutdated"></i>
                            <span class="truncate">{{ cite.fileName }}</span>
                            <span class="font-bold flex-none" [class.text-primary-600]="!cite.isOutdated" [class.text-amber-600]="cite.isOutdated">{{ formatScore(cite.score) }}</span>
                          </div>
                        }
                      </div>

                      <!-- Document Versioning (Phase 19): Show a warning if any of the citations are outdated. -->
                      @if (hasOutdatedCitations(msg)) {
                        <div class="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900 rounded-lg text-[0.75rem] text-amber-700 dark:text-amber-500">
                          <i class="pi pi-exclamation-triangle flex-none"></i>
                          <span>One or more source documents have been updated. This answer may be outdated.</span>
                        </div>
                      }
                    </div>
                  }

                  <!-- Rewritten indicator -->
                  @if (msg.role === 'Assistant' && msg.rewritten) {
                    <div class="inline-flex items-center gap-1 text-[0.7rem] text-purple-600 px-1">
                      <i class="pi pi-refresh text-base"></i> Query rewritten for better retrieval
                    </div>
                  }

                  <span class="text-[0.68rem] text-slate-400 px-1"
                        [class.text-right]="msg.role === 'User'">
                    {{ msg.createdAt | date:'h:mm a' }}
                  </span>
                </div>

                <!-- User Avatar -->
                @if (msg.role === 'User') {
                  <div class="flex items-center justify-center flex-none w-8 h-8 bg-primary-100 text-primary-700 rounded-full text-base">
                    <i class="pi pi-user"></i>
                  </div>
                }
              </div>
            }
          }

          <!-- Typing Indicator -->
          @if (isSending) {
            <div class="flex items-end gap-2.5 max-w-[85%] self-start msg-enter">
              <div class="flex items-center justify-center flex-none w-8 h-8 bg-linear-to-br from-primary-50 to-purple-50 text-primary-600 rounded-full text-base shadow-sm">
                <i class="pi pi-sparkles"></i>
              </div>
              <div class="flex items-center gap-1.5 px-4 py-3.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-2xl rounded-bl-sm shadow-sm">
                <span class="w-1.5 h-1.5 bg-primary-300 rounded-full animate-bounce animate-delay-none"></span>
                <span class="w-1.5 h-1.5 bg-primary-300 rounded-full animate-bounce [animation-delay:0.2s]"></span>
                <span class="w-1.5 h-1.5 bg-primary-300 rounded-full animate-bounce [animation-delay:0.4s]"></span>
              </div>
            </div>
          }

          @if (error) {
            <div class="flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-600 text-[0.82rem]" role="alert">
              <i class="pi pi-exclamation-circle flex-none"></i>
              <span class="flex-1">{{ error }}</span>
              <button
                type="button"
                class="flex items-center gap-1 px-3 py-1.5 bg-red-100 hover:bg-red-200 text-red-700 text-base font-semibold rounded-md border-none cursor-pointer transition-colors"
                (click)="onRetry.emit()"
              >
                <i class="pi pi-refresh text-base"></i> Retry
              </button>
            </div>
          }

          <div #messagesEnd></div>
        </div>

        <!-- Input Bar -->
        <div class="p-4 md:p-6 bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-700 flex-none">
          <form class="flex items-end gap-3 p-2 bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-700 rounded-2xl transition-all focus-within:border-primary-300 focus-within:ring-4 focus-within:ring-primary-100/50 shadow-[0_2px_12px_rgba(0,0,0,0.02)]"
                (ngSubmit)="onSubmit()">
            
            <button type="button" class="flex-none flex items-center justify-center w-10 h-10 rounded-xl text-slate-400 hover:bg-slate-200 hover:text-slate-600 dark:hover:text-slate-300 transition-colors" title="Attach file" (click)="showComingSoon()">
              <i class="pi pi-paperclip text-xl"></i>
            </button>

            <textarea
              #textareaEl
              class="flex-1 bg-transparent border-none! outline-none! focus:outline-none! focus:border-transparent! focus:ring-0! resize-none px-2 py-2.5 text-[0.95rem] text-slate-900 dark:text-white placeholder:text-slate-400 font-sans leading-relaxed max-h-32 custom-scrollbar disabled:opacity-50"
              [(ngModel)]="question"
              name="question"
              rows="1"
              maxlength="1000"
              placeholder="Ask a question..."
              [disabled]="isSending"
              (keydown)="onKeydown($event)"
              (input)="autoResize()"
              aria-label="Your message"
            ></textarea>
            <button
              type="submit"
              class="flex items-center justify-center flex-none w-10 h-10 bg-primary-600 text-white border-none rounded-xl cursor-pointer transition-all hover:bg-primary-700 hover:shadow-md disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed disabled:shadow-none"
              [disabled]="!question.trim() || isSending"
              aria-label="Send message"
            >
              <i class="pi text-xl" [class.pi-spin]="isSending" [class.pi-spinner]="isSending" [class.pi-send]="!isSending" [class.-ml-1]="!isSending" [class.mt-1]="!isSending"></i>
            </button>
          </form>
          <p class="text-center text-[0.7rem] text-slate-400 mt-2 mb-0">
            Press <kbd class="px-1 py-0.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-[0.65rem] font-sans">Enter</kbd> to send · 
            <kbd class="px-1 py-0.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded text-[0.65rem] font-sans">Shift+Enter</kbd> for new line
          </p>
        </div>
      }
    </section>

    <!-- Document Preview Sidebar -->
    <p-sidebar [(visible)]="showPreview" position="right" styleClass="w-full md:w-[450px] bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-700" [showCloseIcon]="true">
      <ng-template pTemplate="header">
        <div class="font-bold text-lg flex items-center gap-2 text-slate-900 dark:text-white">
          <i class="pi pi-file-pdf text-primary-600"></i> Document Preview
        </div>
      </ng-template>
      <ng-template pTemplate="content">
        @if (isLoadingPreview) {
          <div class="flex flex-col gap-4 animate-pulse p-4">
            <div class="h-4 bg-slate-200 dark:bg-slate-800 rounded w-3/4"></div>
            <div class="h-4 bg-slate-200 dark:bg-slate-800 rounded w-full"></div>
            <div class="h-4 bg-slate-200 dark:bg-slate-800 rounded w-5/6"></div>
          </div>
        } @else if (previewError) {
          <div class="p-4 text-red-600 bg-red-50 dark:bg-red-950/50 dark:text-red-400 rounded-lg text-sm border border-red-100 dark:border-red-900">
            {{ previewError }}
          </div>
        } @else if (previewData) {
          <div class="flex flex-col gap-4 p-2">
            <div class="flex flex-col gap-1 pb-4 border-b border-slate-100 dark:border-slate-800">
              <span class="text-base font-semibold text-slate-900 dark:text-white">{{ previewData.documentName }}</span>
              @if (previewData.section || previewData.pageNumber) {
                <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                  {{ previewData.section ? 'Section: ' + previewData.section : '' }}
                  {{ previewData.section && previewData.pageNumber ? ' • ' : '' }}
                  {{ previewData.pageNumber ? 'Page ' + previewData.pageNumber : '' }}
                </span>
              }
            </div>
            <div class="text-sm leading-relaxed text-slate-700 dark:text-slate-300 whitespace-pre-wrap font-serif">
              {{ previewData.text }}
            </div>
          </div>
        }
      </ng-template>
    </p-sidebar>
  `
})
export class ConversationChatComponent {
  private savedAnswerService = inject(SavedAnswerService);
  private conversationService = inject(ConversationService);
  private documentService = inject(DocumentService);
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
  @ViewChild('textareaEl') private textareaEl!: ElementRef<HTMLTextAreaElement>;

  question = '';
  copiedId: string | null = null;

  /** Set of message IDs that the current user has saved. */
  private savedMessageIds = signal(new Set<string>());

  isSaved(messageId: string): boolean {
    return this.savedMessageIds().has(messageId);
  }

  hasOutdatedCitations(msg: MessageDto): boolean {
    return msg.citations.some(c => c.isOutdated);
  }

  toggleSave(messageId: string) {
    if (this.isSaved(messageId)) {
      // Optimistically remove
      this.savedMessageIds.update(s => { const n = new Set(s); n.delete(messageId); return n; });
      this.savedAnswerService.unsaveAnswer(messageId).subscribe({
        error: () => this.savedMessageIds.update(s => new Set(s).add(messageId))
      });
    } else {
      // Optimistically add
      this.savedMessageIds.update(s => new Set(s).add(messageId));
      this.savedAnswerService.saveAnswer(messageId).subscribe({
        error: () => this.savedMessageIds.update(s => { const n = new Set(s); n.delete(messageId); return n; })
      });
    }
  }

  toggleFeedback(msg: MessageDto, rating: 1 | 2) {
    // If they click the same rating again, we could potentially un-rate it if the backend supports it, 
    // but the plan just says "submit feedback". Let's assume clicking it updates it to that rating.
    const prevRating = msg.feedback;
    msg.feedback = rating; // Optimistic update
    
    if (this.conversation?.id) {
      this.conversationService.submitFeedback(this.conversation.id, msg.id, rating, undefined).subscribe({
        error: () => {
          msg.feedback = prevRating; // Revert on error
        }
      });
    }
  }

  private markdownCache = new Map<string, string>();

  onSubmit() {
    const text = this.question.trim();
    if (text && !this.isSending) {
      this.onSendMessage.emit(text);
      this.question = '';
      this.resetTextareaHeight();
    }
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSubmit();
    }
  }

  scrollToBottom() {
    try {
      this.messagesEnd?.nativeElement?.scrollIntoView({ behavior: 'smooth' });
    } catch {}
  }

  formatScore(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  renderMarkdown(content: string): string {
    if (!content) return '';
    
    const cached = this.markdownCache.get(content);
    if (cached) return cached;
    
    let html = marked.parse(content, { async: false, breaks: true }) as string;
    html = DOMPurify.sanitize(html);
    
    // Only cache completed (non-streaming) messages to avoid stale cache entries
    if (content.length > 50) {
      this.markdownCache.set(content, html);
      // Keep cache size manageable
      if (this.markdownCache.size > 100) {
        const firstKey = this.markdownCache.keys().next().value;
        if (firstKey) this.markdownCache.delete(firstKey);
      }
    }
    
    return html;
  }

  copyToClipboard(text: string, id: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedId = id;
      setTimeout(() => this.copiedId = null, 2000);
    });
  }

  showComingSoon() {
    this.messageService.add({ severity: 'info', summary: 'Coming Soon', detail: 'This feature will be available in a future update.' });
  }

  autoResize() {
    const el = this.textareaEl?.nativeElement;
    if (el) {
      el.style.height = 'auto';
      el.style.height = Math.min(el.scrollHeight, 128) + 'px';
    }
  }

  private resetTextareaHeight() {
    setTimeout(() => {
      const el = this.textareaEl?.nativeElement;
      if (el) el.style.height = 'auto';
    });
  }

  showPreview = false;
  isLoadingPreview = false;
  previewError = '';
  previewData: DocumentChunkPreviewDto | null = null;

  openPreview(chunkId: string) {
    if (!chunkId) {
      this.previewError = 'This citation does not have an associated chunk ID.';
      this.showPreview = true;
      return;
    }
    this.showPreview = true;
    this.isLoadingPreview = true;
    this.previewError = '';
    this.previewData = null;

    this.documentService.getDocumentChunk(chunkId).subscribe({
      next: (data) => {
        this.previewData = data;
        this.isLoadingPreview = false;
      },
      error: () => {
        this.previewError = 'Failed to load document preview. You might not have permission to view it or it may have been deleted.';
        this.isLoadingPreview = false;
      }
    });
  }
}
