import { Component, ElementRef, EventEmitter, Input, Output, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { ConversationDetailDto, MessageDto } from '@core/services/conversation.service';
import { marked } from 'marked';

@Component({
  selector: 'app-conversation-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, TooltipModule, DatePipe],
  template: `
    <section class="flex flex-col flex-1 min-w-0 bg-white overflow-hidden">
      
      @if (!conversation) {
        <!-- Empty State -->
        <div class="flex flex-col flex-1 items-center justify-center gap-4 p-8 text-center bg-surface-50">
          <div class="flex items-center justify-center w-20 h-20 bg-linear-to-br from-indigo-100 to-purple-100 text-indigo-600 text-3xl rounded-2xl mb-2 shadow-sm">
            <i class="pi pi-comments"></i>
          </div>
          <h2 class="text-xl font-bold text-surface-900 m-0">Ask Normora anything</h2>
          <p class="text-sm text-surface-500 max-w-sm m-0">Select a conversation on the left, or start a new one to get grounded answers from your company documents.</p>
          <button
            type="button"
            class="mt-2 inline-flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm rounded-lg border-none cursor-pointer transition-colors shadow-sm"
            (click)="onNewConversation.emit()"
          >
            <i class="pi pi-plus text-xs"></i> New conversation
          </button>
        </div>
      } @else {
        <!-- Chat Header -->
        <header class="flex items-center justify-between p-4 px-5 bg-white border-b border-surface-200 flex-none gap-4">
          <div class="flex items-center gap-3 min-w-0">
            <button
              type="button"
              class="flex md:hidden items-center justify-center flex-none w-9 h-9 bg-surface-50 hover:bg-surface-100 border border-surface-200 rounded-lg text-surface-600 cursor-pointer transition-colors"
              (click)="onToggleSidebar.emit()"
              aria-label="Toggle sidebar"
            >
              <i class="pi pi-bars text-sm"></i>
            </button>
            <div class="flex items-center justify-center flex-none w-9 h-9 bg-indigo-50 text-indigo-600 rounded-lg text-sm">
              <i class="pi pi-comments"></i>
            </div>
            <div class="min-w-0">
              <div class="text-[0.95rem] font-bold text-surface-900 truncate">{{ conversation.title }}</div>
              <div class="text-[0.75rem] text-surface-400">
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
                  <div class="w-7/12 h-14 bg-surface-100 rounded-2xl animate-pulse"></div>
                </div>
              }
            </div>
          } @else if (conversation.messages.length === 0) {
            <div class="flex flex-col flex-1 items-center justify-center gap-3 text-center text-surface-400">
              <div class="w-16 h-16 flex items-center justify-center bg-linear-to-br from-indigo-50 to-purple-50 rounded-2xl">
                <i class="pi pi-sparkles text-3xl text-indigo-300"></i>
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
                  <div class="flex items-center justify-center flex-none w-8 h-8 bg-linear-to-br from-indigo-50 to-purple-50 text-indigo-600 rounded-full text-xs shadow-sm">
                    <i class="pi pi-sparkles"></i>
                  </div>
                }

                <div class="flex flex-col gap-1.5 min-w-0">
                  <!-- Bubble -->
                  <div class="group relative px-4 py-3 rounded-2xl wrap-break-word"
                       [class.bg-indigo-600]="msg.role === 'User'"
                       [class.text-white]="msg.role === 'User'"
                       [class.rounded-br-sm]="msg.role === 'User'"
                       [class.bg-white]="msg.role === 'Assistant'"
                       [class.text-surface-800]="msg.role === 'Assistant'"
                       [class.border]="msg.role === 'Assistant'"
                       [class.border-surface-200]="msg.role === 'Assistant'"
                       [class.rounded-bl-sm]="msg.role === 'Assistant'"
                       [class.shadow-sm]="msg.role === 'Assistant'">
                    
                    @if (msg.role === 'User') {
                      <p class="text-[0.9rem] leading-relaxed whitespace-pre-wrap m-0">{{ msg.content }}</p>
                    } @else {
                      <div class="prose-normora text-[0.9rem] leading-relaxed" [innerHTML]="renderMarkdown(msg.content)"></div>
                    }

                    <!-- Copy button for assistant messages -->
                    @if (msg.role === 'Assistant' && msg.content) {
                      <button
                        type="button"
                        class="absolute top-2 right-2 flex items-center justify-center w-7 h-7 bg-surface-50 hover:bg-surface-100 border border-surface-200 rounded-md text-surface-400 hover:text-surface-600 opacity-0 group-hover:opacity-100 transition-all cursor-pointer"
                        (click)="copyToClipboard(msg.content)"
                        [pTooltip]="copiedId === msg.id ? 'Copied!' : 'Copy'"
                        tooltipPosition="top"
                        [attr.aria-label]="'Copy message'"
                      >
                        <i class="text-xs" [class.pi-check]="copiedId === msg.id" [class.text-green-600]="copiedId === msg.id" [class.pi-copy]="copiedId !== msg.id"></i>
                      </button>
                    }
                  </div>

                  <!-- Citations -->
                  @if (msg.role === 'Assistant' && msg.citations.length > 0) {
                    <div class="flex flex-wrap items-center gap-1.5 px-1">
                      <span class="text-[0.68rem] font-bold text-surface-400 tracking-widest uppercase mr-1">Sources</span>
                      @for (cite of msg.citations; track cite.documentId + cite.fileName) {
                        <div class="inline-flex items-center gap-1.5 px-2.5 py-1 bg-surface-50 border border-surface-200 rounded-full text-surface-600 text-[0.72rem] font-medium max-w-40"
                             [pTooltip]="cite.fileName" tooltipPosition="top">
                          <i class="pi pi-file-pdf text-surface-400 text-[0.7rem] flex-none"></i>
                          <span class="truncate">{{ cite.fileName }}</span>
                          <span class="text-indigo-600 font-bold flex-none">{{ formatScore(cite.score) }}</span>
                        </div>
                      }
                    </div>
                  }

                  <!-- Rewritten indicator -->
                  @if (msg.role === 'Assistant' && msg.rewritten) {
                    <div class="inline-flex items-center gap-1 text-[0.7rem] text-purple-600 px-1">
                      <i class="pi pi-refresh text-[0.7rem]"></i> Query rewritten for better retrieval
                    </div>
                  }

                  <span class="text-[0.68rem] text-surface-400 px-1"
                        [class.text-right]="msg.role === 'User'">
                    {{ msg.createdAt | date:'h:mm a' }}
                  </span>
                </div>

                <!-- User Avatar -->
                @if (msg.role === 'User') {
                  <div class="flex items-center justify-center flex-none w-8 h-8 bg-indigo-100 text-indigo-700 rounded-full text-xs">
                    <i class="pi pi-user"></i>
                  </div>
                }
              </div>
            }
          }

          <!-- Typing Indicator -->
          @if (isSending) {
            <div class="flex items-end gap-2.5 max-w-[85%] self-start msg-enter">
              <div class="flex items-center justify-center flex-none w-8 h-8 bg-linear-to-br from-indigo-50 to-purple-50 text-indigo-600 rounded-full text-xs shadow-sm">
                <i class="pi pi-sparkles"></i>
              </div>
              <div class="flex items-center gap-1.5 px-4 py-3.5 bg-white border border-surface-200 rounded-2xl rounded-bl-sm shadow-sm">
                <span class="w-1.5 h-1.5 bg-indigo-300 rounded-full animate-bounce animate-delay-none"></span>
                <span class="w-1.5 h-1.5 bg-indigo-300 rounded-full animate-bounce [animation-delay:0.2s]"></span>
                <span class="w-1.5 h-1.5 bg-indigo-300 rounded-full animate-bounce [animation-delay:0.4s]"></span>
              </div>
            </div>
          }

          @if (error) {
            <div class="flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg text-red-600 text-[0.82rem]" role="alert">
              <i class="pi pi-exclamation-circle flex-none"></i>
              <span class="flex-1">{{ error }}</span>
              <button
                type="button"
                class="flex items-center gap-1 px-3 py-1.5 bg-red-100 hover:bg-red-200 text-red-700 text-[0.78rem] font-semibold rounded-md border-none cursor-pointer transition-colors"
                (click)="onRetry.emit()"
              >
                <i class="pi pi-refresh text-[0.7rem]"></i> Retry
              </button>
            </div>
          }

          <div #messagesEnd></div>
        </div>

        <!-- Input Bar -->
        <div class="p-3 md:p-4 bg-white border-t border-surface-200 flex-none">
          <form class="flex items-end gap-2.5 p-2 bg-surface-50 border border-surface-200 rounded-xl transition-all focus-within:border-indigo-300 focus-within:ring-2 focus-within:ring-indigo-100"
                (ngSubmit)="onSubmit()">
            <textarea
              #textareaEl
              class="flex-1 bg-transparent border-none outline-none resize-none px-2 py-1.5 text-[0.9rem] text-surface-900 placeholder:text-surface-400 font-sans leading-relaxed max-h-32 custom-scrollbar disabled:opacity-50"
              [(ngModel)]="question"
              name="question"
              rows="1"
              maxlength="1000"
              placeholder="Ask a question about company policies, benefits, or procedures…"
              [disabled]="isSending"
              (keydown)="onKeydown($event)"
              (input)="autoResize()"
              aria-label="Your message"
            ></textarea>
            <button
              type="submit"
              class="flex items-center justify-center flex-none w-9 h-9 bg-indigo-600 text-white border-none rounded-lg cursor-pointer transition-all hover:bg-indigo-700 disabled:bg-surface-200 disabled:text-surface-400 disabled:cursor-not-allowed"
              [disabled]="!question.trim() || isSending"
              aria-label="Send message"
            >
              <i class="text-sm" [class.pi-spin]="isSending" [class.pi-spinner]="isSending" [class.pi-arrow-up]="!isSending"></i>
            </button>
          </form>
          <p class="text-center text-[0.7rem] text-surface-400 mt-2 mb-0">
            Press <kbd class="px-1 py-0.5 bg-surface-100 border border-surface-200 rounded text-[0.65rem] font-sans">Enter</kbd> to send · 
            <kbd class="px-1 py-0.5 bg-surface-100 border border-surface-200 rounded text-[0.65rem] font-sans">Shift+Enter</kbd> for new line
          </p>
        </div>
      }
    </section>
  `
})
export class ConversationChatComponent {
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
    
    const html = marked.parse(content, { async: false, breaks: true }) as string;
    
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

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      // Find the message by content to set copiedId
      const msg = this.conversation?.messages.find(m => m.content === text);
      if (msg) {
        this.copiedId = msg.id;
        setTimeout(() => this.copiedId = null, 2000);
      }
    });
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
}
