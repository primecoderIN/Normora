import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { MessageDto } from '@core/services/conversation.service';
import { MarkdownSanitizerService } from '@core/services/markdown-sanitizer.service';

/**
 * Component responsible solely for presenting an individual chat message bubble (User or Assistant),
 * its citations, outdated warnings, copy action, bookmark toggle, and feedback rating.
 */
@Component({
  selector: 'app-chat-message-bubble',
  standalone: true,
  imports: [CommonModule, TooltipModule, DatePipe],
  template: `
    <div
      class="flex items-end gap-2.5 max-w-[85%] md:max-w-[75%] msg-enter"
      [class.self-end]="message.role === 'User'"
      [class.flex-row-reverse]="message.role === 'User'"
      [class.self-start]="message.role === 'Assistant'"
    >
      <!-- Assistant Avatar -->
      @if (message.role === 'Assistant') {
        <div class="flex items-center justify-center flex-none w-8 h-8 bg-linear-to-br from-primary-50 to-purple-50 text-primary-600 rounded-full text-base shadow-sm">
          <i class="pi pi-sparkles"></i>
        </div>
      }

      <div class="flex flex-col gap-1.5 min-w-0">
        <!-- Bubble Content -->
        <div
          class="group relative px-4 py-3 rounded-2xl wrap-break-word"
          [class.bg-primary-600]="message.role === 'User'"
          [class.text-white]="message.role === 'User'"
          [class.rounded-br-sm]="message.role === 'User'"
          [class.bg-white]="message.role === 'Assistant'"
          [class.dark:bg-slate-900]="message.role === 'Assistant'"
          [class.text-slate-800]="message.role === 'Assistant'"
          [class.dark:text-slate-100]="message.role === 'Assistant'"
          [class.border]="message.role === 'Assistant'"
          [class.border-slate-200]="message.role === 'Assistant'"
          [class.dark:border-slate-700]="message.role === 'Assistant'"
          [class.rounded-bl-sm]="message.role === 'Assistant'"
          [class.shadow-sm]="message.role === 'Assistant'"
        >
          @if (message.role === 'User') {
            <p class="text-[0.9rem] leading-relaxed whitespace-pre-wrap m-0">{{ message.content }}</p>
          } @else {
            <div class="prose-normora text-[0.9rem] leading-relaxed" [innerHTML]="sanitizer.render(message.content)"></div>
          }

          <!-- Action buttons for assistant messages -->
          @if (message.role === 'Assistant' && message.content) {
            <div class="absolute top-2 right-2 flex items-center gap-1 opacity-100 md:opacity-0 md:group-hover:opacity-100 focus-within:opacity-100 transition-all">
              <!-- Bookmark button -->
              <button
                type="button"
                class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-amber-500 transition-all cursor-pointer"
                (click)="onToggleSave.emit(message.id)"
                [pTooltip]="isSaved ? 'Unsave' : 'Save answer'"
                tooltipPosition="top"
                [attr.aria-label]="isSaved ? 'Unsave answer' : 'Save answer'"
              >
                <i
                  class="text-base pi"
                  [class.pi-bookmark-fill]="isSaved"
                  [class.text-amber-500]="isSaved"
                  [class.pi-bookmark]="!isSaved"
                ></i>
              </button>

              <!-- Helpful Feedback button -->
              <button
                type="button"
                class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-green-500 transition-all cursor-pointer"
                (click)="onToggleFeedback.emit({ message, rating: 1 })"
                pTooltip="Helpful"
                tooltipPosition="top"
                aria-label="Mark helpful"
              >
                <i
                  class="text-base pi"
                  [class.pi-thumbs-up-fill]="message.feedback === 1"
                  [class.text-green-500]="message.feedback === 1"
                  [class.pi-thumbs-up]="message.feedback !== 1"
                ></i>
              </button>

              <!-- Not Helpful Feedback button -->
              <button
                type="button"
                class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-red-500 transition-all cursor-pointer"
                (click)="onToggleFeedback.emit({ message, rating: 2 })"
                pTooltip="Not helpful"
                tooltipPosition="top"
                aria-label="Mark not helpful"
              >
                <i
                  class="text-base pi"
                  [class.pi-thumbs-down-fill]="message.feedback === 2"
                  [class.text-red-500]="message.feedback === 2"
                  [class.pi-thumbs-down]="message.feedback !== 2"
                ></i>
              </button>

              <!-- Copy button -->
              <button
                type="button"
                class="flex items-center justify-center w-7 h-7 bg-slate-50 dark:bg-slate-950 hover:bg-slate-100 dark:hover:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 transition-all cursor-pointer"
                (click)="copyToClipboard()"
                [pTooltip]="copied() ? 'Copied!' : 'Copy'"
                tooltipPosition="top"
                aria-label="Copy message"
              >
                <i
                  class="pi text-base"
                  [class.pi-check]="copied()"
                  [class.text-green-600]="copied()"
                  [class.pi-copy]="!copied()"
                ></i>
              </button>
            </div>
          }
        </div>

        <!-- Citations -->
        @if (message.role === 'Assistant' && message.citations.length > 0) {
          <div class="flex flex-col gap-2">
            <div class="flex flex-wrap items-center gap-1.5 px-1">
              <span class="text-[0.68rem] font-bold text-slate-400 tracking-widest uppercase mr-1">Sources</span>
              @for (cite of message.citations; track cite.documentId + cite.fileName) {
                <div
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 border rounded-full text-[0.72rem] font-medium max-w-40 cursor-pointer hover:border-primary-300 hover:shadow-sm transition-all"
                  [class.bg-slate-50]="!cite.isOutdated"
                  [class.dark:bg-slate-950]="!cite.isOutdated"
                  [class.border-slate-200]="!cite.isOutdated"
                  [class.dark:border-slate-700]="!cite.isOutdated"
                  [class.text-slate-600]="!cite.isOutdated"
                  [class.dark:text-slate-300]="!cite.isOutdated"
                  [class.bg-amber-50]="cite.isOutdated"
                  [class.dark:bg-amber-950/50]="cite.isOutdated"
                  [class.border-amber-200]="cite.isOutdated"
                  [class.dark:border-amber-900]="cite.isOutdated"
                  [class.text-amber-700]="cite.isOutdated"
                  [class.dark:text-amber-500]="cite.isOutdated"
                  [pTooltip]="cite.fileName"
                  tooltipPosition="top"
                  (click)="onPreviewCitation.emit(cite.documentChunkId)"
                >
                  <i
                    class="pi pi-file-pdf text-base flex-none"
                    [class.text-slate-400]="!cite.isOutdated"
                    [class.text-amber-500]="cite.isOutdated"
                  ></i>
                  <span class="truncate">{{ cite.fileName }}</span>
                  <span
                    class="font-bold flex-none"
                    [class.text-primary-600]="!cite.isOutdated"
                    [class.text-amber-600]="cite.isOutdated"
                  >
                    {{ formatScore(cite.score) }}
                  </span>
                </div>
              }
            </div>

            <!-- Outdated Version Warning -->
            @if (hasOutdatedCitations()) {
              <div class="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900 rounded-lg text-[0.75rem] text-amber-700 dark:text-amber-500">
                <i class="pi pi-exclamation-triangle flex-none"></i>
                <span>One or more source documents have been updated. This answer may be outdated.</span>
              </div>
            }
          </div>
        }

        <!-- Rewritten Query Indicator -->
        @if (message.role === 'Assistant' && message.rewritten) {
          <div class="inline-flex items-center gap-1 text-[0.7rem] text-purple-600 dark:text-purple-400 px-1">
            <i class="pi pi-refresh text-base"></i>
            <span>Query rewritten for better retrieval</span>
          </div>
        }

        <!-- Timestamp -->
        <span
          class="text-[0.68rem] text-slate-400 px-1"
          [class.text-right]="message.role === 'User'"
        >
          {{ message.createdAt | date:'h:mm a' }}
        </span>
      </div>

      <!-- User Avatar -->
      @if (message.role === 'User') {
        <div class="flex items-center justify-center flex-none w-8 h-8 bg-primary-100 text-primary-700 rounded-full text-base">
          <i class="pi pi-user"></i>
        </div>
      }
    </div>
  `
})
export class ChatMessageBubbleComponent {
  public sanitizer = inject(MarkdownSanitizerService);

  @Input({ required: true }) message!: MessageDto;
  @Input() isSaved = false;

  @Output() onToggleSave = new EventEmitter<string>();
  @Output() onToggleFeedback = new EventEmitter<{ message: MessageDto; rating: 1 | 2 }>();
  @Output() onPreviewCitation = new EventEmitter<string>();

  copied = signal(false);

  copyToClipboard(): void {
    if (!this.message.content) return;
    navigator.clipboard.writeText(this.message.content).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  hasOutdatedCitations(): boolean {
    return this.message.citations?.some(c => c.isOutdated) ?? false;
  }

  formatScore(score: number): string {
    return `${Math.round(score * 100)}%`;
  }
}
