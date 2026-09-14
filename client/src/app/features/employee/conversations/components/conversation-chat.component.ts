import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { ConversationDetailDto } from '@core/services/conversation.service';

@Component({
  selector: 'app-conversation-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, TooltipModule, DatePipe],
  template: `
    <section class="flex flex-col flex-1 min-w-0 bg-white overflow-hidden">
      
      @if (!conversation) {
        <!-- Empty State -->
        <div class="flex flex-col flex-1 items-center justify-center gap-4 p-8 text-center bg-surface-50">
          <div class="flex items-center justify-center w-16 h-16 bg-indigo-50 text-indigo-600 text-2xl rounded-full mb-2">
            <i class="pi pi-comments"></i>
          </div>
          <h2 class="text-xl font-bold text-surface-900 m-0">Ask Normora anything</h2>
          <p class="text-sm text-surface-500 max-w-sm m-0">Select a conversation on the left, or start a new one to get grounded answers from your company documents.</p>
          <button
            type="button"
            class="mt-2 inline-flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm rounded-lg border-none cursor-pointer transition-colors"
            (click)="onNewConversation.emit()"
          >
            <i class="pi pi-plus text-xs"></i> New conversation
          </button>
        </div>
      } @else {
        <!-- Chat Header -->
        <header class="flex items-center justify-between p-4 px-5 bg-white border-b border-surface-200 flex-none gap-4">
          <div class="flex items-center gap-3 min-w-0">
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
            <div class="flex flex-col flex-1 items-center justify-center gap-2 text-center text-surface-400">
              <i class="pi pi-sparkles text-3xl text-indigo-200"></i>
              <p class="text-sm m-0">Ask your first question below.</p>
            </div>
          } @else {
            @for (msg of conversation.messages; track msg.id) {
              <div class="flex items-end gap-2.5 max-w-[85%] md:max-w-[75%]"
                   [class.self-end]="msg.role === 'User'"
                   [class.flex-row-reverse]="msg.role === 'User'"
                   [class.self-start]="msg.role === 'Assistant'">
                
                <!-- Avatar -->
                @if (msg.role === 'Assistant') {
                  <div class="flex items-center justify-center flex-none w-8 h-8 bg-indigo-50 text-indigo-600 rounded-full text-xs">
                    <i class="pi pi-sparkles"></i>
                  </div>
                }

                <div class="flex flex-col gap-1.5 min-w-0">
                  <!-- Bubble -->
                  <div class="px-4 py-3 rounded-2xl wrap-break-word"
                       [class.bg-indigo-600]="msg.role === 'User'"
                       [class.text-white]="msg.role === 'User'"
                       [class.rounded-br-sm]="msg.role === 'User'"
                       [class.bg-white]="msg.role === 'Assistant'"
                       [class.text-surface-800]="msg.role === 'Assistant'"
                       [class.border]="msg.role === 'Assistant'"
                       [class.border-surface-200]="msg.role === 'Assistant'"
                       [class.rounded-bl-sm]="msg.role === 'Assistant'"
                       [class.shadow-sm]="msg.role === 'Assistant'">
                    <p class="text-[0.9rem] leading-relaxed whitespace-pre-wrap m-0">{{ msg.content }}</p>
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
            <div class="flex items-end gap-2.5 max-w-[85%] self-start">
              <div class="flex items-center justify-center flex-none w-8 h-8 bg-indigo-50 text-indigo-600 rounded-full text-xs">
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
            <div class="flex items-center gap-2 p-2.5 bg-red-50 border border-red-200 rounded-lg text-red-600 text-[0.82rem]" role="alert">
              <i class="pi pi-exclamation-circle"></i>
              {{ error }}
            </div>
          }

          <div #messagesEnd></div>
        </div>

        <!-- Input Bar -->
        <div class="p-3 md:p-4 bg-white border-t border-surface-200 flex-none">
          <form class="flex items-end gap-2.5 p-2 bg-surface-50 border border-surface-200 rounded-xl transition-all focus-within:border-indigo-300 focus-within:ring-2 focus-within:ring-indigo-100"
                (ngSubmit)="onSubmit()">
            <textarea
              class="flex-1 bg-transparent border-none outline-none resize-none px-2 py-1.5 text-[0.9rem] text-surface-900 placeholder:text-surface-400 font-sans leading-relaxed max-h-32 custom-scrollbar disabled:opacity-50"
              [(ngModel)]="question"
              name="question"
              rows="1"
              maxlength="1000"
              placeholder="Ask a question about company policies, benefits, or procedures…"
              [disabled]="isSending"
              (keydown)="onKeydown($event)"
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

  @ViewChild('messagesEnd') private messagesEnd!: ElementRef<HTMLDivElement>;

  question = '';

  onSubmit() {
    const text = this.question.trim();
    if (text && !this.isSending) {
      this.onSendMessage.emit(text);
      this.question = '';
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
}
