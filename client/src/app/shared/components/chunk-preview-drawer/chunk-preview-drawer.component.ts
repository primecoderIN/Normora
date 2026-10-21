import { Component, EventEmitter, Input, Output, inject, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DsDrawerComponent } from '@design-system';
import { DocumentService, DocumentChunkPreviewDto } from '@core/services/document.service';
import { MarkdownSanitizerService } from '@core/services/markdown-sanitizer.service';
import { ApiMessages } from '@core/constants/api-messages';

/**
 * Component responsible solely for displaying document chunk citation previews in a slide-over drawer.
 * Adheres to SRP by handling citation chunk data fetching, loading state, error display, and rendering.
 */
@Component({
  selector: 'app-chunk-preview-drawer',
  standalone: true,
  imports: [CommonModule, DsDrawerComponent],
  template: `
    <ds-drawer
      [visible]="visible"
      (visibleChange)="setVisible($event)"
      (closed)="onDrawerHide()"
      position="right"
      title="Document Preview"
      widthClass="max-w-[450px] w-full"
    >
      @if (isLoading()) {
        <div class="flex flex-col gap-4 animate-pulse p-4">
          <div class="h-4 bg-slate-200 dark:bg-slate-800 rounded w-3/4"></div>
          <div class="h-4 bg-slate-200 dark:bg-slate-800 rounded w-full"></div>
          <div class="h-4 bg-slate-200 dark:bg-slate-800 rounded w-5/6"></div>
        </div>
      } @else if (error()) {
        <div class="p-4 text-red-600 bg-red-50 dark:bg-red-950/50 dark:text-red-400 rounded-lg text-sm border border-red-100 dark:border-red-900">
          {{ error() }}
        </div>
      } @else if (previewData()) {
        <div class="flex flex-col gap-4 p-2">
          <div class="flex flex-col gap-1 pb-4 border-b border-slate-100 dark:border-slate-800">
            <span class="text-base font-semibold text-slate-900 dark:text-white">
              {{ previewData()!.documentName }}
            </span>
            @if (previewData()!.section || previewData()!.pageNumber) {
              <span class="text-xs text-slate-500 dark:text-slate-400 font-medium">
                {{ previewData()!.section ? 'Section: ' + previewData()!.section : '' }}
                {{ previewData()!.section && previewData()!.pageNumber ? ' • ' : '' }}
                {{ previewData()!.pageNumber ? 'Page ' + previewData()!.pageNumber : '' }}
              </span>
            }
          </div>
          <div
            class="text-sm leading-relaxed text-slate-700 dark:text-slate-300 whitespace-pre-wrap font-serif"
            [innerHTML]="sanitizer.render(previewData()!.text)"
          ></div>
        </div>
      }
    </ds-drawer>
  `
})
export class ChunkPreviewDrawerComponent {
  private documentService = inject(DocumentService);
  private cdr = inject(ChangeDetectorRef);
  public sanitizer = inject(MarkdownSanitizerService);

  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();

  previewData = signal<DocumentChunkPreviewDto | null>(null);
  isLoading = signal(false);
  error = signal('');

  /**
   * Opens the drawer and retrieves the document chunk citation preview.
   *
   * @param chunkId The unique ID of the document chunk
   */
  open(chunkId: string): void {
    if (!chunkId) {
      this.error.set(ApiMessages.ChunkMissing);
      this.previewData.set(null);
      this.setVisible(true);
      return;
    }

    this.setVisible(true);
    this.isLoading.set(true);
    this.error.set('');
    this.previewData.set(null);

    this.documentService.getDocumentChunk(chunkId).subscribe({
      next: (data) => {
        this.previewData.set(data);
        this.isLoading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.error.set(ApiMessages.ChunkPreviewFailed);
        this.isLoading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  close(): void {
    this.setVisible(false);
  }

  onDrawerHide(): void {
    this.setVisible(false);
  }

  setVisible(val: boolean): void {
    this.visible = val;
    this.visibleChange.emit(val);
  }
}
