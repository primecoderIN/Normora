import { Component, inject, signal, OnInit, HostListener } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TooltipModule } from 'primeng/tooltip';
import { SavedAnswerService, SavedAnswerDto, ExportFormat } from '@core/services/saved-answer.service';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

interface ExportOption {
  format: ExportFormat;
  label: string;
  icon: string;
  description: string;
}

@Component({
  selector: 'app-saved-answers',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterModule, TooltipModule],
  styles: [`
    .export-dropdown {
      position: absolute;
      top: calc(100% + 6px);
      right: 0;
      z-index: 50;
      min-width: 188px;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      box-shadow: 0 8px 24px rgba(0,0,0,0.10), 0 2px 6px rgba(0,0,0,0.06);
      overflow: hidden;
      animation: dropdown-in 120ms ease-out;
    }
    :host-context(.dark) .export-dropdown {
      background: #1e293b;
      border-color: #334155;
    }
    @keyframes dropdown-in {
      from { opacity: 0; transform: translateY(-4px) scale(0.98); }
      to   { opacity: 1; transform: translateY(0)    scale(1); }
    }
    .export-dropdown-item {
      display: flex;
      align-items: center;
      gap: 10px;
      width: 100%;
      padding: 9px 14px;
      border: none;
      background: transparent;
      cursor: pointer;
      text-align: left;
      transition: background 120ms;
    }
    .export-dropdown-item:hover {
      background: #f8fafc;
    }
    :host-context(.dark) .export-dropdown-item:hover {
      background: #0f172a;
    }
    .export-dropdown-item:not(:last-child) {
      border-bottom: 1px solid #f1f5f9;
    }
    :host-context(.dark) .export-dropdown-item:not(:last-child) {
      border-bottom-color: #1e293b;
    }
    .export-spinner {
      width: 12px;
      height: 12px;
      border: 2px solid #cbd5e1;
      border-top-color: #6366f1;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
  template: `
    <div class="flex flex-col gap-8 page-enter">

      <!-- Header -->
      <header class="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
        <div>
          <p class="text-[0.72rem] font-extrabold uppercase tracking-wider text-primary-600 m-0 mb-1">Library</p>
          <h1 class="text-[1.75rem] font-bold text-slate-900 dark:text-white leading-[1.15] m-0">Saved Answers</h1>
          <p class="text-[0.9rem] text-slate-500 dark:text-slate-400 m-0 mt-1.5">
            {{ isLoading() ? 'Loading…' : (savedAnswers().length + ' saved answer' + (savedAnswers().length === 1 ? '' : 's')) }}
          </p>
        </div>
      </header>

      <!-- Loading skeleton -->
      @if (isLoading()) {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          @for (n of [1,2,3,4,5,6]; track n) {
            <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl p-5 shadow-sm animate-pulse space-y-3">
              <div class="h-3 bg-slate-200 rounded-full w-1/3"></div>
              <div class="h-3 bg-slate-100 dark:bg-slate-800 rounded-full w-full"></div>
              <div class="h-3 bg-slate-100 dark:bg-slate-800 rounded-full w-5/6"></div>
              <div class="h-3 bg-slate-100 dark:bg-slate-800 rounded-full w-4/6"></div>
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
          <h2 class="text-lg font-bold text-slate-900 dark:text-white m-0">Could not load saved answers</h2>
          <p class="text-sm text-slate-500 dark:text-slate-400 m-0 max-w-sm">{{ error() }}</p>
          <button class="inline-flex items-center gap-2 px-4 py-2 bg-primary-600 hover:bg-primary-700 text-white text-base font-semibold rounded-lg border-none cursor-pointer transition-colors" (click)="load()">
            <i class="pi pi-refresh text-base"></i> Try again
          </button>
        </div>
      }

      <!-- Empty state -->
      @else if (savedAnswers().length === 0) {
        <div class="flex flex-col items-center justify-center gap-4 py-24 text-center">
          <div class="flex items-center justify-center w-20 h-20 rounded-2xl bg-amber-50 text-amber-500 shadow-sm">
            <i class="pi pi-bookmark text-3xl"></i>
          </div>
          <h2 class="text-lg font-bold text-slate-900 dark:text-white m-0">No saved answers yet</h2>
          <p class="text-sm text-slate-500 dark:text-slate-400 m-0 max-w-xs leading-relaxed">
            When you get a helpful answer in a conversation, click the
            <span class="inline-flex items-center justify-center w-5 h-5 bg-slate-100 dark:bg-slate-800 rounded mx-0.5 align-middle"><i class="pi pi-bookmark text-base"></i></span>
            icon to save it here.
          </p>
          <a routerLink="../conversations" class="inline-flex items-center gap-2 px-5 py-2.5 bg-primary-600 hover:bg-primary-700 text-white text-sm font-semibold rounded-lg no-underline transition-colors shadow-sm">
            <i class="pi pi-comments text-base"></i> Start a conversation
          </a>
        </div>
      }

      <!-- Answers grid -->
      @else {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          @for (answer of savedAnswers(); track answer.id) {
            <article class="group flex flex-col bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl shadow-sm hover:shadow-md hover:border-slate-300 transition-all overflow-hidden">

              <!-- Card Header -->
              <div class="flex items-center justify-between gap-2 px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-950/60">
                <div class="flex items-center gap-2">
                  <div class="flex items-center justify-center w-6 h-6 bg-primary-50 text-primary-500 rounded-md">
                    <i class="pi pi-sparkles text-base"></i>
                  </div>
                  <span class="text-[0.72rem] text-slate-500 dark:text-slate-400 font-medium">{{ answer.savedAt | date:'MMM d, y · h:mm a' }}</span>
                </div>

                <!-- Action buttons — visible on hover -->
                <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">

                  <!-- Export dropdown trigger -->
                  <div class="relative">
                    <button
                      id="export-btn-{{ answer.id }}"
                      class="flex items-center justify-center w-7 h-7 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-500 hover:bg-primary-50 hover:text-primary-600 hover:border-primary-200 transition-all cursor-pointer shadow-sm"
                      (click)="toggleExportMenu($event, answer.id)"
                      pTooltip="Export answer"
                      tooltipPosition="top"
                      aria-label="Export answer"
                      [attr.aria-expanded]="openExportMenuId() === answer.id"
                    >
                      @if (exportingId() === answer.id) {
                        <span class="export-spinner"></span>
                      } @else {
                        <i class="pi pi-download text-base"></i>
                      }
                    </button>

                    <!-- Export format dropdown -->
                    @if (openExportMenuId() === answer.id) {
                      <div class="export-dropdown" role="menu">
                        <p class="text-[0.65rem] font-bold uppercase tracking-widest text-slate-400 px-3.5 pt-2.5 pb-1 m-0">Export as</p>
                        @for (opt of exportOptions; track opt.format) {
                          <button
                            class="export-dropdown-item"
                            role="menuitem"
                            (click)="exportAnswer(answer, opt.format)"
                          >
                            <i class="{{ opt.icon }} text-base text-slate-500"></i>
                            <div class="flex flex-col">
                              <span class="text-[0.8rem] font-semibold text-slate-800 dark:text-slate-100">{{ opt.label }}</span>
                              <span class="text-[0.68rem] text-slate-400">{{ opt.description }}</span>
                            </div>
                          </button>
                        }
                      </div>
                    }
                  </div>

                  <!-- Unsave button -->
                  <button
                    class="flex items-center justify-center w-7 h-7 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-amber-500 hover:bg-red-50 hover:text-red-500 hover:border-red-200 transition-all cursor-pointer shadow-sm"
                    (click)="unsave(answer)"
                    pTooltip="Remove bookmark"
                    tooltipPosition="top"
                    aria-label="Remove saved answer"
                  >
                    <i class="pi pi-bookmark-fill text-base"></i>
                  </button>
                </div>
              </div>

              <!-- Export error inline callout -->
              @if (exportError() === answer.id) {
                <div class="flex items-center gap-2 px-4 py-2 bg-red-50 dark:bg-red-950/40 border-b border-red-100 dark:border-red-900 text-red-600 dark:text-red-400 text-xs font-medium">
                  <i class="pi pi-exclamation-circle text-base flex-none"></i>
                  <span>Export failed. Please try again.</span>
                </div>
              }

              <!-- Answer Content -->
              <div class="flex-1 px-5 py-4 prose-normora text-[0.875rem] leading-relaxed text-slate-800 dark:text-slate-100 overflow-hidden max-h-48 relative">
                <div [innerHTML]="renderMarkdown(answer.content)"></div>
                <!-- Fade mask at bottom -->
                <div class="absolute bottom-0 left-0 right-0 h-10 bg-linear-to-t from-white to-transparent pointer-events-none"></div>
              </div>

              <!-- Citations -->
              @if (answer.citations.length > 0) {
                <div class="flex flex-col gap-2 px-5 pb-3">
                  <div class="flex flex-wrap gap-1.5">
                    <span class="text-[0.65rem] font-bold text-slate-400 tracking-widest uppercase mr-1 self-center">Sources</span>
                    @for (cite of answer.citations; track cite.documentId) {
                      <div class="inline-flex items-center gap-1.5 px-2.5 py-1 border rounded-full text-[0.7rem] font-medium max-w-36"
                           [class.bg-slate-50]="!cite.isOutdated" [class.dark:bg-slate-950]="!cite.isOutdated" [class.border-slate-200]="!cite.isOutdated" [class.dark:border-slate-700]="!cite.isOutdated" [class.text-slate-600]="!cite.isOutdated" [class.dark:text-slate-300]="!cite.isOutdated"
                           [class.bg-amber-50]="cite.isOutdated" [class.dark:bg-amber-950/50]="cite.isOutdated" [class.border-amber-200]="cite.isOutdated" [class.dark:border-amber-900]="cite.isOutdated" [class.text-amber-700]="cite.isOutdated" [class.dark:text-amber-500]="cite.isOutdated"
                           [pTooltip]="cite.fileName" tooltipPosition="top">
                        <i class="pi pi-file-pdf text-base flex-none" [class.text-slate-400]="!cite.isOutdated" [class.text-amber-500]="cite.isOutdated"></i>
                        <span class="truncate">{{ cite.fileName }}</span>
                        <span class="font-bold flex-none" [class.text-primary-600]="!cite.isOutdated" [class.text-amber-600]="cite.isOutdated">{{ formatScore(cite.score) }}</span>
                      </div>
                    }
                  </div>
                  
                  <!-- Document Versioning (Phase 19): Show a warning if any of the citations are outdated. -->
                  @if (hasOutdatedCitations(answer)) {
                    <div class="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900 rounded-lg text-[0.75rem] text-amber-700 dark:text-amber-500 mt-1">
                      <i class="pi pi-exclamation-triangle flex-none"></i>
                      <span>One or more source documents have been updated. This answer may be outdated.</span>
                    </div>
                  }
                </div>
              }

              <!-- Footer -->
              <div class="px-5 py-3 border-t border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-950/40">
                <a
                  [routerLink]="['../conversations']"
                  [queryParams]="{ conversationId: answer.conversationId }"
                  class="inline-flex items-center gap-1.5 text-[0.8rem] font-semibold text-primary-600 hover:text-primary-700 no-underline transition-colors"
                >
                  <i class="pi pi-arrow-right text-base"></i> View conversation
                </a>
              </div>
            </article>
          }
        </div>

        <!-- Load more -->
        @if (hasMore()) {
          <div class="flex justify-center pt-2">
            <button class="inline-flex items-center gap-2 px-5 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-950 text-base font-semibold rounded-lg cursor-pointer transition-colors shadow-sm disabled:opacity-50" (click)="loadMore()" [disabled]="isLoadingMore()">
              @if (isLoadingMore()) {
                <i class="pi pi-spin pi-spinner text-base"></i> Loading…
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
  openExportMenuId = signal<string | null>(null);
  exportingId = signal<string | null>(null);
  exportError = signal<string | null>(null);
  private exportErrorTimer: ReturnType<typeof setTimeout> | null = null;

  private limit = 20;
  private offset = 0;
  hasMore = signal(false);

  private markdownCache = new Map<string, string>();

  readonly exportOptions: ExportOption[] = [
    { format: 'Markdown', label: 'Markdown', icon: 'pi pi-file', description: '.md — plain text with formatting' },
    { format: 'Pdf',      label: 'PDF',      icon: 'pi pi-file-pdf', description: '.pdf — print-ready document' },
    { format: 'Docx',     label: 'Word',     icon: 'pi pi-file-word', description: '.docx — editable Word document' },
  ];

  ngOnInit() {
    this.load();
  }

  /** Close the export dropdown when clicking outside any card. */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    if (this.openExportMenuId() !== null) {
      const target = event.target as HTMLElement;
      if (!target.closest('.export-dropdown') && !target.closest('[id^="export-btn-"]')) {
        this.openExportMenuId.set(null);
      }
    }
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

  toggleExportMenu(event: Event, answerId: string) {
    event.stopPropagation();
    this.openExportMenuId.update(current => current === answerId ? null : answerId);
  }

  exportAnswer(answer: SavedAnswerDto, format: ExportFormat) {
    if (this.exportingId()) return;

    this.openExportMenuId.set(null);
    this.exportingId.set(answer.id);
    this.exportError.set(null);

    this.savedAnswerService.exportAnswer(answer.id, format).subscribe({
      next: () => this.exportingId.set(null),
      error: () => {
        this.exportingId.set(null);
        this.exportError.set(answer.id);
        // Auto-clear the error callout after 4 seconds.
        if (this.exportErrorTimer) clearTimeout(this.exportErrorTimer);
        this.exportErrorTimer = setTimeout(() => this.exportError.set(null), 4000);
      },
    });
  }

  renderMarkdown(content: string): string {
    if (!content) return '';
    const cached = this.markdownCache.get(content);
    if (cached) return cached;
    let html = marked.parse(content, { async: false, breaks: true }) as string;
    html = DOMPurify.sanitize(html);
    this.markdownCache.set(content, html);
    return html;
  }

  formatScore(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  hasOutdatedCitations(answer: SavedAnswerDto): boolean {
    return answer.citations.some(c => c.isOutdated);
  }
}
