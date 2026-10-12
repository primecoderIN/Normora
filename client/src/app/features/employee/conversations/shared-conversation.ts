import { Component, OnInit, inject, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TooltipModule } from 'primeng/tooltip';
import { DrawerModule } from 'primeng/drawer';
import { ConversationService, SharedConversationDetailDto, MessageDto } from '@core/services/conversation.service';
import { DocumentService, DocumentChunkPreviewDto } from '@core/services/document.service';
import { MessageService } from 'primeng/api';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

@Component({
  selector: 'app-shared-conversation',
  standalone: true,
  imports: [CommonModule, RouterModule, TooltipModule, DatePipe, DrawerModule],
  template: `
    <div class="flex flex-col h-full bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100">
      
      <!-- Top Bar / Header -->
      <header class="flex items-center justify-between px-6 py-4 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-700 shadow-xs flex-none">
        <div class="flex items-center gap-4 min-w-0">
          <button
            type="button"
            class="flex items-center justify-center w-9 h-9 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 rounded-lg text-slate-600 dark:text-slate-300 transition-colors cursor-pointer"
            (click)="goToConversations()"
            pTooltip="Back to conversations"
            tooltipPosition="bottom"
          >
            <i class="pi pi-arrow-left text-sm"></i>
          </button>

          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <h1 class="text-lg font-bold text-slate-900 dark:text-white truncate m-0">
                {{ conversation()?.title || 'Shared Conversation' }}
              </h1>
              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-50 dark:bg-amber-950/50 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-800">
                <i class="pi pi-lock text-[0.65rem] mr-1"></i> Read-only
              </span>
            </div>
            @if (conversation()?.sharedByName) {
              <p class="text-xs text-slate-500 dark:text-slate-400 m-0 mt-0.5">
                Shared by <strong class="text-slate-700 dark:text-slate-200">{{ conversation()?.sharedByName }}</strong>
              </p>
            }
          </div>
        </div>

        <div class="flex items-center gap-3">
          <button
            type="button"
            class="hidden sm:inline-flex items-center gap-2 px-4 py-2 bg-primary-600 hover:bg-primary-700 text-white text-xs font-semibold rounded-lg shadow-sm transition-colors cursor-pointer"
            (click)="goToConversations()"
          >
            <i class="pi pi-plus text-xs"></i>
            <span>Start your own conversation</span>
          </button>
        </div>
      </header>

      <!-- Main Content Area -->
      <main class="flex-1 min-h-0 overflow-y-auto p-4 sm:p-6 custom-scrollbar">
        <div class="max-w-4xl mx-auto flex flex-col gap-5">

          <!-- Loading Skeleton -->
          @if (isLoading()) {
            <div class="flex flex-col gap-4 animate-pulse pt-4">
              <div class="flex justify-end">
                <div class="w-2/3 h-16 bg-slate-200 dark:bg-slate-800 rounded-2xl"></div>
              </div>
              <div class="flex justify-start">
                <div class="w-3/4 h-28 bg-slate-200 dark:bg-slate-800 rounded-2xl"></div>
              </div>
              <div class="flex justify-end">
                <div class="w-1/2 h-14 bg-slate-200 dark:bg-slate-800 rounded-2xl"></div>
              </div>
            </div>
          }

          <!-- Error / Link Invalid State -->
          @else if (errorMessage()) {
            <div class="flex flex-col items-center justify-center p-12 text-center bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-xs mt-8">
              <div class="w-16 h-16 rounded-2xl bg-red-50 dark:bg-red-950/50 text-red-500 dark:text-red-400 flex items-center justify-center mb-4">
                <i class="pi pi-exclamation-circle text-2xl"></i>
              </div>
              <h2 class="text-xl font-bold text-slate-900 dark:text-white m-0 mb-2">Conversation Not Found</h2>
              <p class="text-sm text-slate-500 dark:text-slate-400 max-w-md m-0 mb-6">
                {{ errorMessage() }}
              </p>
              <button
                type="button"
                class="inline-flex items-center gap-2 px-5 py-2.5 bg-primary-600 hover:bg-primary-700 text-white text-sm font-semibold rounded-xl shadow-xs transition-colors cursor-pointer"
                (click)="goToConversations()"
              >
                <i class="pi pi-arrow-left text-sm"></i>
                <span>Return to Conversations</span>
              </button>
            </div>
          }

          <!-- Messages List -->
          @else if (conversation()) {
            <!-- Notice Banner -->
            <div class="flex items-center gap-3 p-3.5 bg-blue-50/70 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-900/50 rounded-xl text-blue-900 dark:text-blue-200 text-xs">
              <i class="pi pi-info-circle text-blue-600 dark:text-blue-400 text-base flex-none"></i>
              <span>You are viewing a shared, read-only snapshot of this conversation. Replies cannot be added to this link.</span>
            </div>

            @for (msg of conversation()!.messages; track msg.id) {
              <!-- User Message -->
              @if (msg.role === 'User') {
                <div class="flex justify-end gap-3 max-w-[85%] self-end">
                  <div class="flex flex-col items-end">
                    <div class="p-4 rounded-2xl rounded-tr-xs bg-primary-600 text-white shadow-xs text-sm leading-relaxed whitespace-pre-wrap font-sans">
                      {{ msg.content }}
                    </div>
                    <span class="text-[0.7rem] text-slate-400 mt-1 px-1">
                      {{ msg.createdAt | date:'shortTime' }}
                    </span>
                  </div>
                </div>
              }

              <!-- Assistant Message -->
              @else {
                <div class="flex gap-3 max-w-[92%] self-start">
                  <div class="flex items-center justify-center w-8 h-8 rounded-xl bg-linear-to-br from-primary-500 to-indigo-600 text-white text-xs shadow-xs flex-none mt-1">
                    <i class="pi pi-sparkles"></i>
                  </div>
                  
                  <div class="flex flex-col min-w-0">
                    <div class="p-4 sm:p-5 rounded-2xl rounded-tl-xs bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xs text-sm leading-relaxed font-sans text-slate-800 dark:text-slate-100">
                      <!-- Markdown Content -->
                      <div class="prose dark:prose-invert max-w-none text-sm leading-relaxed" [innerHTML]="renderMarkdown(msg.content)"></div>

                      <!-- Citations -->
                      @if (msg.citations.length > 0) {
                        <div class="mt-4 pt-3 border-t border-slate-100 dark:border-slate-800/80">
                          <div class="text-[0.7rem] font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center gap-1.5">
                            <i class="pi pi-bookmark text-xs"></i> Sources Cited
                          </div>
                          <div class="flex flex-wrap gap-2">
                            @for (c of msg.citations; track c.documentChunkId) {
                              <button
                                type="button"
                                class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium border transition-colors cursor-pointer group"
                                [ngClass]="c.isOutdated 
                                  ? 'bg-amber-50 dark:bg-amber-950/40 border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-400 hover:bg-amber-100' 
                                  : 'bg-slate-50 dark:bg-slate-800 border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700'"
                                (click)="openPreview(c.documentChunkId)"
                              >
                                <i class="pi pi-file-pdf text-[0.7rem] text-slate-500 group-hover:text-primary-600 transition-colors"></i>
                                <span class="truncate max-w-[200px]">{{ c.fileName }}</span>
                                @if (c.isOutdated) {
                                  <span class="inline-flex items-center text-[0.65rem] px-1 py-0.2 rounded bg-amber-200 dark:bg-amber-900 text-amber-800 dark:text-amber-200 font-bold ml-1">
                                    Outdated
                                  </span>
                                }
                              </button>
                            }
                          </div>
                        </div>
                      }
                    </div>

                    <!-- Message Footer -->
                    <div class="flex items-center justify-between text-[0.7rem] text-slate-400 mt-1 px-1">
                      <span>{{ msg.createdAt | date:'shortTime' }}</span>
                      <button
                        type="button"
                        class="hover:text-slate-600 dark:hover:text-slate-200 transition-colors cursor-pointer flex items-center gap-1"
                        (click)="copyToClipboard(msg.content, msg.id)"
                      >
                        <i class="pi" [ngClass]="copiedId() === msg.id ? 'pi-check text-green-500' : 'pi-copy'"></i>
                        <span>{{ copiedId() === msg.id ? 'Copied' : 'Copy' }}</span>
                      </button>
                    </div>
                  </div>
                </div>
              }
            }
          }
        </div>
      </main>

      <!-- Bottom Floating Call-To-Action (Mobile) -->
      <footer class="sm:hidden p-3 bg-white dark:bg-slate-900 border-t border-slate-200 dark:border-slate-700 flex-none text-center">
        <button
          type="button"
          class="w-full inline-flex items-center justify-center gap-2 py-2.5 bg-primary-600 hover:bg-primary-700 text-white text-xs font-semibold rounded-lg shadow-sm transition-colors cursor-pointer"
          (click)="goToConversations()"
        >
          <i class="pi pi-plus text-xs"></i>
          <span>Start your own conversation</span>
        </button>
      </footer>

      <!-- Document Preview Drawer -->
      <p-drawer [(visible)]="showPreview" position="right" appendTo="body" [blockScroll]="false" styleClass="w-full md:w-[450px] bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-700" [showCloseIcon]="true">
        <ng-template pTemplate="header">
          <div class="font-bold text-lg flex items-center gap-2 text-slate-900 dark:text-white">
            <i class="pi pi-file-pdf text-primary-600"></i> Document Preview
          </div>
        </ng-template>
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
            <div class="text-sm leading-relaxed text-slate-700 dark:text-slate-300 whitespace-pre-wrap font-serif" [innerHTML]="renderMarkdown(previewData.text)">
            </div>
          </div>
        }
      </p-drawer>

    </div>
  `
})
export class SharedConversationComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private conversationService = inject(ConversationService);
  private documentService = inject(DocumentService);
  private messageService = inject(MessageService);
  private cdr = inject(ChangeDetectorRef);

  conversation = signal<SharedConversationDetailDto | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  copiedId = signal<string | null>(null);

  // Document preview drawer state
  showPreview = false;
  isLoadingPreview = false;
  previewError = '';
  previewData: DocumentChunkPreviewDto | null = null;

  private markdownCache = new Map<string, string>();

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      this.errorMessage.set('Invalid share link: no token provided.');
      this.isLoading.set(false);
      return;
    }

    this.loadSharedConversation(token);
  }

  loadSharedConversation(token: string): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.conversationService.getSharedConversation(token).subscribe({
      next: (data) => {
        this.conversation.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('This conversation link is either invalid, expired, or belongs to another workspace.');
        this.isLoading.set(false);
      }
    });
  }

  goToConversations(): void {
    const match = window.location.pathname.match(/\/app\/workspaces\/[^/]+/);
    const workspacePath = match ? match[0] : '';
    this.router.navigateByUrl(`${workspacePath}/employee/conversations`);
  }

  renderMarkdown(content: string): string {
    if (!content) return '';
    
    let html = this.markdownCache.get(content);
    if (!html) {
      const parsed = marked.parse(content, { async: false, breaks: true }) as string;
      html = DOMPurify.sanitize(parsed);
      this.markdownCache.set(content, html);
      if (this.markdownCache.size > 100) {
        const firstKey = this.markdownCache.keys().next().value;
        if (firstKey) this.markdownCache.delete(firstKey);
      }
    }
    return html;
  }

  copyToClipboard(text: string, id: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedId.set(id);
      setTimeout(() => this.copiedId.set(null), 2000);
    });
  }

  openPreview(chunkId: string): void {
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
        this.cdr.markForCheck();
      },
      error: () => {
        this.previewError = 'Failed to load document preview. You might not have permission to view it or it may have been deleted.';
        this.isLoadingPreview = false;
        this.cdr.markForCheck();
      }
    });
  }
}
