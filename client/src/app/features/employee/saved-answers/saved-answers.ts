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
  styleUrl: './saved-answers.css',
  template: `
    <div class="saved-answers-page">
      <!-- Header -->
      <header class="saved-answers-header">
        <div class="header-left">
          <div class="header-icon">
            <i class="pi pi-bookmark-fill"></i>
          </div>
          <div>
            <h1 class="header-title">Saved Answers</h1>
            <p class="header-subtitle">
              {{ isLoading() ? 'Loading…' : (savedAnswers().length + ' saved answer' + (savedAnswers().length === 1 ? '' : 's')) }}
            </p>
          </div>
        </div>
      </header>

      <!-- Loading skeleton -->
      @if (isLoading()) {
        <div class="answers-grid">
          @for (n of [1,2,3]; track n) {
            <div class="answer-card skeleton-card">
              <div class="skeleton-line w-3/4"></div>
              <div class="skeleton-line w-full mt-2"></div>
              <div class="skeleton-line w-5/6 mt-1"></div>
              <div class="skeleton-line w-1/2 mt-1"></div>
            </div>
          }
        </div>
      }

      <!-- Error -->
      @else if (error()) {
        <div class="empty-state">
          <div class="empty-icon error-icon"><i class="pi pi-exclamation-circle"></i></div>
          <h2 class="empty-title">Could not load saved answers</h2>
          <p class="empty-subtitle">{{ error() }}</p>
          <button class="btn-primary" (click)="load()">
            <i class="pi pi-refresh"></i> Try again
          </button>
        </div>
      }

      <!-- Empty state -->
      @else if (savedAnswers().length === 0) {
        <div class="empty-state">
          <div class="empty-icon"><i class="pi pi-bookmark"></i></div>
          <h2 class="empty-title">No saved answers yet</h2>
          <p class="empty-subtitle">
            When you get a helpful answer in a conversation, click the
            <span class="inline-icon"><i class="pi pi-bookmark"></i></span>
            bookmark icon to save it here.
          </p>
          <a routerLink="/employee/conversations" class="btn-primary">
            <i class="pi pi-comments"></i> Start a conversation
          </a>
        </div>
      }

      <!-- Answers grid -->
      @else {
        <div class="answers-grid">
          @for (answer of savedAnswers(); track answer.id) {
            <article class="answer-card">
              <!-- Card Header -->
              <div class="card-header">
                <div class="card-meta">
                  <i class="pi pi-sparkles meta-icon"></i>
                  <span class="meta-date">{{ answer.savedAt | date:'MMM d, y · h:mm a' }}</span>
                </div>
                <button
                  class="unsave-btn"
                  (click)="unsave(answer)"
                  pTooltip="Remove bookmark"
                  tooltipPosition="top"
                  aria-label="Remove saved answer"
                >
                  <i class="pi pi-bookmark-fill"></i>
                </button>
              </div>

              <!-- Answer Content -->
              <div class="card-content prose-normora"
                   [innerHTML]="renderMarkdown(answer.content)">
              </div>

              <!-- Citations -->
              @if (answer.citations.length > 0) {
                <div class="card-citations">
                  <span class="citations-label">Sources</span>
                  @for (cite of answer.citations; track cite.documentId) {
                    <div class="citation-chip"
                         [pTooltip]="cite.fileName"
                         tooltipPosition="top">
                      <i class="pi pi-file-pdf citation-file-icon"></i>
                      <span class="citation-filename">{{ cite.fileName }}</span>
                      <span class="citation-score">{{ formatScore(cite.score) }}</span>
                    </div>
                  }
                </div>
              }

              <!-- Footer -->
              <div class="card-footer">
                <a
                  [routerLink]="['/employee/conversations']"
                  [queryParams]="{ conversationId: answer.conversationId }"
                  class="view-conversation-link"
                >
                  <i class="pi pi-arrow-right"></i> View conversation
                </a>
              </div>
            </article>
          }
        </div>

        <!-- Load more -->
        @if (hasMore()) {
          <div class="load-more-row">
            <button class="btn-secondary" (click)="loadMore()" [disabled]="isLoadingMore()">
              @if (isLoadingMore()) {
                <i class="pi pi-spinner pi-spin"></i> Loading…
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
