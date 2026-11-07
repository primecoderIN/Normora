import { Component, inject, signal, OnInit, HostListener, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DsTooltipDirective } from '@design-system';
import { SavedAnswerService, SavedAnswerDto, ExportFormat } from '@core/services/saved-answer.service';
import { MarkdownSanitizerService } from '@core/services/markdown-sanitizer.service';
import { ChunkPreviewDrawerComponent } from '@shared/components/chunk-preview-drawer/chunk-preview-drawer.component';
import { ApiMessages } from '@core/constants/api-messages';

interface ExportOption {
  format: ExportFormat;
  label: string;
  icon: string;
  description: string;
}

/**
 * Component responsible for displaying, managing, and exporting the employee's saved answers.
 * Adheres to SRP by delegating markdown sanitization to MarkdownSanitizerService and
 * citation previews to ChunkPreviewDrawerComponent.
 */
@Component({
  selector: 'app-saved-answers',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterModule, DsTooltipDirective, ChunkPreviewDrawerComponent],
  templateUrl: './saved-answers.html',
})
export class SavedAnswers implements OnInit {
  private savedAnswerService = inject(SavedAnswerService);
  public sanitizer = inject(MarkdownSanitizerService);

  @ViewChild('chunkDrawer') private chunkDrawer!: ChunkPreviewDrawerComponent;

  savedAnswers = signal<SavedAnswerDto[]>([]);
  isLoading = signal(false);
  isLoadingMore = signal(false);
  error = signal('');
  openExportMenuId = signal<string | null>(null);
  exportingId = signal<string | null>(null);
  exportError = signal<string | null>(null);
  exportErrorMessage = ApiMessages.ExportFailed.detail;
  private exportErrorTimer: ReturnType<typeof setTimeout> | null = null;

  private limit = 20;
  private offset = 0;
  hasMore = signal(false);

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
      if (!target.closest('[role="menu"]') && !target.closest('[id^="export-btn-"]')) {
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
        this.error.set(ApiMessages.SavedAnswersLoadFailed.detail);
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
    // Optimistic UI Removal & Rollback:
    // Immediately filters the saved answer card out of the reactive Signal list.
    // If the backend mutation fails, the error callback reverts the filter and restores the card.
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
    // Multi-Format File Export (Markdown / QuestPDF / OpenXML Word):
    // Prevents concurrent export spam by tracking exportingId.
    // Triggers blob download stream with appropriate MIME type and Content-Disposition file header.
    if (this.exportingId()) return;

    this.openExportMenuId.set(null);
    this.exportingId.set(answer.id);
    this.exportError.set(null);

    this.savedAnswerService.exportAnswer(answer.id, format).subscribe({
      next: () => this.exportingId.set(null),
      error: () => {
        this.exportingId.set(null);
        this.exportError.set(answer.id);
        if (this.exportErrorTimer) clearTimeout(this.exportErrorTimer);
        this.exportErrorTimer = setTimeout(() => this.exportError.set(null), 4000);
      },
    });
  }

  formatScore(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  hasOutdatedCitations(answer: SavedAnswerDto): boolean {
    // Knowledge Staleness Invariant:
    // When employer uploads newer versions of company documents, previously saved answers may reference
    // outdated chunks. We display an amber warning badge if any citation has isOutdated == true.
    return answer.citations.some(c => c.isOutdated);
  }

  openPreview(chunkId: string) {
    // Citation Drawer Activation:
    // Opens sliding passage inspection flyout with the underlying chunk text.
    this.chunkDrawer.open(chunkId);
  }
}
