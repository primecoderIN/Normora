import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TooltipModule } from 'primeng/tooltip';
import { SavedAnswerService, SavedAnswerDto } from '@core/services/saved-answer.service';
import { marked } from 'marked';

@Component({
  selector: 'app-saved-answers',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterModule, TooltipModule],
  template: `
    <div class="flex flex-col gap-8 page-enter">

      <!-- Header -->
      <header class="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
        <div>
          <p class="text-[0.72rem] font-extrabold uppercase tracking-wider text-indigo-600 m-0 mb-1">Library</p>
          <h1 class="text-[1.75rem] font-bold text-surface-900 leading-[1.15] m-0">Saved Answers</h1>
          <p class="text-[0.9rem] text-surface-500 m-0 mt-1.5">
            {{ isLoading() ? 'Loading…' : (savedAnswers().length + ' saved answer' + (savedAnswers().length === 1 ? '' : 's')) }}
          </p>
        </div>
      </header>

      <!-- Loading skeleton -->
      @if (isLoading()) {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          @for (n of [1,2,3,4,5,6]; track n) {
            <div class="bg-white border border-surface-200 rounded-xl p-5 shadow-sm animate-pulse space-y-3">
              <div class="h-3 bg-surface-200 rounded-full w-1/3"></div>
              <div class="h-3 bg-surface-100 rounded-full w-full"></div>
              <div class="h-3 bg-surface-100 rounded-full w-5/6"></div>
              <div class="h-3 bg-surface-100 rounded-full w-4/6"></div>
            </div>
          }
        </div>
      }

      <!-- Error -->
      @else if (error()) {
        <div class="flex flex-col items-center justify-center gap-4 py-24 text-center">
          <div class="flex items-center justify-center w-16 h-16 rounded-2xl bg-red-50 text-red-500">
            <i class="pi pi-exclamation-circle text-3xl"></i>
          </div>
          <h2 class="text-lg font-bold text-surface-900 m-0">Could not load saved answers</h2>
          <p class="text-sm text-surface-500 m-0 max-w-sm">{{ error() }}</p>
          <button class="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg border-none cursor-pointer transition-colors" (click)="load()">
            <i class="pi pi-refresh text-xs"></i> Try again
          </button>
        </div>
      }

      <!-- Empty state -->
      @else if (savedAnswers().length === 0) {
        <div class="flex flex-col items-center justify-center gap-4 py-24 text-center">
          <div class="flex items-center justify-center w-20 h-20 rounded-2xl bg-amber-50 text-amber-500 shadow-sm">
            <i class="pi pi-bookmark text-3xl"></i>
          </div>
          <h2 class="text-lg font-bold text-surface-900 m-0">No saved answers yet</h2>
          <p class="text-sm text-surface-500 m-0 max-w-xs leading-relaxed">
            When you get a helpful answer in a conversation, click the
            <span class="inline-flex items-center justify-center w-5 h-5 bg-surface-100 rounded mx-0.5 align-middle"><i class="pi pi-bookmark text-[0.65rem]"></i></span>
            icon to save it here.
          </p>
          <a routerLink="/employee/conversations" class="inline-flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg no-underline transition-colors shadow-sm">
            <i class="pi pi-comments text-xs"></i> Start a conversation
          </a>
        </div>
      }

      <!-- Answers grid -->
      @else {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          @for (answer of savedAnswers(); track answer.id) {
            <article class="group flex flex-col bg-white border border-surface-200 rounded-xl shadow-sm hover:shadow-md hover:border-surface-300 transition-all overflow-hidden">
              <!-- Card Header -->
              <div class="flex items-center justify-between gap-2 px-5 py-3.5 border-b border-surface-100 bg-surface-50/60">
                <div class="flex items-center gap-2">
                  <div class="flex items-center justify-center w-6 h-6 bg-indigo-50 text-indigo-500 rounded-md">
                    <i class="pi pi-sparkles text-[0.65rem]"></i>
                  </div>
                  <span class="text-[0.72rem] text-surface-500 font-medium">{{ answer.savedAt | date:'MMM d, y · h:mm a' }}</span>
                </div>
                <button
                  class="flex items-center justify-center w-7 h-7 bg-white border border-surface-200 rounded-lg text-amber-500 hover:bg-red-50 hover:text-red-500 hover:border-red-200 transition-all cursor-pointer shadow-sm opacity-0 group-hover:opacity-100"
                  (click)="unsave(answer)"
                  pTooltip="Remove bookmark"
                  tooltipPosition="top"
                  aria-label="Remove saved answer"
                >
                  <i class="pi pi-bookmark-fill text-xs"></i>
                </button>
              </div>

              <!-- Answer Content -->
              <div class="flex-1 px-5 py-4 prose-normora text-[0.875rem] leading-relaxed text-surface-800 overflow-hidden max-h-48 relative">
                <div [innerHTML]="renderMarkdown(answer.content)"></div>
                <!-- Fade mask at bottom -->
                <div class="absolute bottom-0 left-0 right-0 h-10 bg-linear-to-t from-white to-transparent pointer-events-none"></div>
              </div>

              <!-- Citations -->
              @if (answer.citations.length > 0) {
                <div class="px-5 pb-3 flex flex-wrap gap-1.5">
                  <span class="text-[0.65rem] font-bold text-surface-400 tracking-widest uppercase mr-1 self-center">Sources</span>
                  @for (cite of answer.citations; track cite.documentId) {
                    <div class="inline-flex items-center gap-1.5 px-2.5 py-1 bg-surface-50 border border-surface-200 rounded-full text-surface-600 text-[0.7rem] font-medium max-w-36"
                         [pTooltip]="cite.fileName" tooltipPosition="top">
                      <i class="pi pi-file-pdf text-surface-400 text-[0.65rem] flex-none"></i>
                      <span class="truncate">{{ cite.fileName }}</span>
                      <span class="text-indigo-600 font-bold flex-none">{{ formatScore(cite.score) }}</span>
                    </div>
                  }
                </div>
              }

              <!-- Footer -->
              <div class="px-5 py-3 border-t border-surface-100 bg-surface-50/40">
                <a
                  [routerLink]="['/employee/conversations']"
                  [queryParams]="{ conversationId: answer.conversationId }"
                  class="inline-flex items-center gap-1.5 text-[0.8rem] font-semibold text-indigo-600 hover:text-indigo-700 no-underline transition-colors"
                >
                  <i class="pi pi-arrow-right text-[0.7rem]"></i> View conversation
                </a>
              </div>
            </article>
          }
        </div>

        <!-- Load more -->
        @if (hasMore()) {
          <div class="flex justify-center pt-2">
            <button class="inline-flex items-center gap-2 px-5 py-2.5 bg-white border border-surface-200 text-surface-700 hover:bg-surface-50 text-sm font-semibold rounded-lg cursor-pointer transition-colors shadow-sm disabled:opacity-50" (click)="loadMore()" [disabled]="isLoadingMore()">
              @if (isLoadingMore()) {
                <i class="pi pi-spin pi-spinner text-xs"></i> Loading…
              } @else {
                Load more
              }
            </button>
          </div>
        }
      }
    </div>
  `
})
export class SavedAnswers implements OnInit {
  private savedAnswerService = inject(SavedAnswerService);

  savedAnswers = signal<SavedAnswerDto[]>([]);
  isLoading = signal(false);
  isLoadingMore = signal(false);
  error = signal('');

  private limit = 20;
  private offset = 0;
  hasMore = signal(false);

  private markdownCache = new Map<string, string>();

  ngOnInit() {
    this.load();
  }

  load() {
    this.isLoading.set(true);
    this.error.set('');
    this.offset = 0;

    this.savedAnswerService.getSavedAnswers(this.limit, this.offset).subscribe({
      next: items => {
        this.savedAnswers.set(items);
        this.offset = items.length;
        this.hasMore.set(items.length === this.limit);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Something went wrong. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  loadMore() {
    if (this.isLoadingMore()) return;
    this.isLoadingMore.set(true);

    this.savedAnswerService.getSavedAnswers(this.limit, this.offset).subscribe({
      next: items => {
        this.savedAnswers.update(curr => [...curr, ...items]);
        this.offset += items.length;
        this.hasMore.set(items.length === this.limit);
        this.isLoadingMore.set(false);
      },
      error: () => this.isLoadingMore.set(false)
    });
  }

  unsave(answer: SavedAnswerDto) {
    // Optimistic removal
    this.savedAnswers.update(list => list.filter(a => a.id !== answer.id));
    this.savedAnswerService.unsaveAnswer(answer.messageId).subscribe({
      error: () => this.savedAnswers.update(list => [answer, ...list])
    });
  }

  renderMarkdown(content: string): string {
    if (!content) return '';
    const cached = this.markdownCache.get(content);
    if (cached) return cached;
    const html = marked.parse(content, { async: false, breaks: true }) as string;
    this.markdownCache.set(content, html);
    return html;
  }

  formatScore(score: number): string {
    return `${Math.round(score * 100)}%`;
  }
}
