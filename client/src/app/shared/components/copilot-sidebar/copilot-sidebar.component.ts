import {
  Component,
  inject,
  signal,
  ViewChild,
  ElementRef,
  AfterViewChecked,
  HostListener
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DsTooltipDirective } from '@design-system';
import { CopilotService } from '@core/services/copilot.service';
import { MarkdownSanitizerService } from '@core/services/markdown-sanitizer.service';
import { ActionCardDto } from '@core/services/conversation.service';

/**
 * Omnipresent Copilot Sidebar component implementing the CopilotKit / AG-UI architecture.
 * Provides a floating, persistent AI copilot accessible across all employer and employee
 * views with page context perception, keyboard shortcuts (Ctrl+J), and real-time streaming.
 */
@Component({
  selector: 'app-copilot-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule, DsTooltipDirective],
  template: `
    <!-- Floating Copilot Launch Trigger Pill -->
    @if (!copilotService.isOpen() && !copilotService.isFullPageConversation()) {
      <button
        type="button"
        (click)="copilotService.open()"
        [dsTooltip]="'Normora Copilot (Ctrl+J)'"
        dsTooltipPosition="left"
        aria-label="Open Normora Copilot"
        class="fixed bottom-6 right-6 z-40 flex items-center gap-2.5 px-4 py-3 bg-linear-to-r from-primary-600 via-indigo-600 to-purple-600 hover:from-primary-500 hover:to-indigo-500 text-white rounded-full shadow-lg shadow-primary-500/30 hover:shadow-primary-500/50 hover:scale-105 active:scale-95 transition-all duration-200 cursor-pointer border border-white/20 group"
      >
        <div class="relative flex items-center justify-center">
          <i class="pi pi-sparkles text-base animate-pulse"></i>
          <span class="absolute -top-1 -right-1 w-2 h-2 rounded-full bg-emerald-400 ring-2 ring-white"></span>
        </div>
        <span class="text-xs font-bold tracking-wide">Copilot</span>
        <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 text-[10px] font-mono font-medium bg-white/20 rounded text-white/90">
          Ctrl+J
        </kbd>
      </button>
    }

    <!-- Mobile Drawer Backdrop -->
    @if (copilotService.isOpen()) {
      <div
        (click)="copilotService.close()"
        class="fixed inset-0 z-40 bg-slate-950/50 backdrop-blur-xs md:hidden transition-opacity"
        aria-hidden="true"
      ></div>
    }

    <!-- Slide-Out Copilot Drawer Panel -->
    <aside
      class="fixed top-0 right-0 h-full w-full sm:w-[420px] md:w-[460px] z-50 bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl flex flex-col transition-transform duration-300 ease-in-out"
      [class.translate-x-0]="copilotService.isOpen()"
      [class.translate-x-full]="!copilotService.isOpen()"
      [class.pointer-events-none]="!copilotService.isOpen()"
      aria-label="Copilot Assistant"
      [attr.aria-hidden]="!copilotService.isOpen()"
    >
      <!-- Header -->
      <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-900/90 backdrop-blur-sm shrink-0">
        <div class="flex items-center gap-2.5 min-w-0">
          <div class="flex items-center justify-center w-8 h-8 rounded-xl bg-linear-to-br from-primary-500 to-indigo-600 text-white shadow-sm shrink-0">
            <i class="pi pi-sparkles text-sm"></i>
          </div>
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <span class="text-sm font-bold text-slate-900 dark:text-white leading-tight">Normora Copilot</span>
              <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                <span class="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
                AI
              </span>
            </div>
            <!-- Context Perception Pill -->
            <div class="text-[11px] text-slate-500 dark:text-slate-400 flex items-center gap-1 truncate mt-0.5">
              <i class="pi pi-compass text-[10px] text-primary-500"></i>
              <span class="font-medium text-slate-700 dark:text-slate-300 truncate">{{ copilotService.activeContext().pageTitle }}</span>
            </div>
          </div>
        </div>

        <!-- Header Action Buttons -->
        <div class="flex items-center gap-1 shrink-0">
          <button
            type="button"
            (click)="resetThread()"
            [dsTooltip]="'New thread'"
            dsTooltipPosition="bottom"
            aria-label="Reset Copilot conversation"
            class="flex items-center justify-center w-8 h-8 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-200/50 dark:hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
          >
            <i class="pi pi-plus text-xs"></i>
          </button>
          <button
            type="button"
            (click)="copilotService.close()"
            [dsTooltip]="'Close (Esc)'"
            dsTooltipPosition="bottom"
            aria-label="Close Copilot"
            class="flex items-center justify-center w-8 h-8 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-200/50 dark:hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>
      </div>

      <!-- Messages Scroll Area -->
      <div
        #messageFeed
        class="flex-1 overflow-y-auto p-4 space-y-4 custom-scrollbar bg-slate-50/40 dark:bg-slate-950/30"
      >
        <!-- Empty State with Contextual Suggestions -->
        @if (copilotService.messages().length === 0) {
          <div class="flex flex-col items-center justify-center text-center py-8 px-2 space-y-4">
            <div class="w-12 h-12 rounded-2xl bg-linear-to-br from-primary-500/20 to-indigo-500/20 border border-primary-500/30 flex items-center justify-center text-primary-600 dark:text-primary-400 shadow-inner">
              <i class="pi pi-bolt text-xl"></i>
            </div>
            <div>
              <h3 class="text-sm font-bold text-slate-900 dark:text-white">Workspace Intelligence</h3>
              <p class="text-xs text-slate-500 dark:text-slate-400 max-w-xs mt-1">
                Ask questions grounded in company policies, documents, or current page context.
              </p>
            </div>

            <!-- Contextual Quick-Prompt Chips -->
            <div class="w-full space-y-2 text-left pt-2">
              <div class="text-[11px] font-bold uppercase tracking-wider text-slate-400 px-1">
                Suggested for {{ copilotService.activeContext().pageTitle }}
              </div>
              @for (prompt of copilotService.activeContext().suggestedPrompts; track prompt) {
                <button
                  type="button"
                  (click)="submitPrompt(prompt)"
                  class="w-full text-left p-2.5 rounded-xl text-xs font-medium bg-white dark:bg-slate-800/80 hover:bg-primary-50 dark:hover:bg-primary-950/40 border border-slate-200 dark:border-slate-700/70 hover:border-primary-300 dark:hover:border-primary-700 text-slate-700 dark:text-slate-200 transition-all cursor-pointer shadow-2xs flex items-center justify-between group"
                >
                  <span class="truncate pr-2">{{ prompt }}</span>
                  <i class="pi pi-arrow-right text-[10px] text-slate-400 group-hover:text-primary-600 dark:group-hover:text-primary-400 group-hover:translate-x-0.5 transition-all shrink-0"></i>
                </button>
              }
            </div>
          </div>
        }

        <!-- Message Bubbles -->
        @for (message of copilotService.messages(); track message.id) {
          <div
            class="flex flex-col gap-1.5"
            [class.items-end]="message.role === 'User'"
            [class.items-start]="message.role === 'Assistant'"
          >
            <!-- Bubble Content -->
            <div
              class="px-3.5 py-2.5 rounded-2xl text-xs leading-relaxed wrap-break-word shadow-2xs"
              [class.bg-primary-600]="message.role === 'User'"
              [class.text-white]="message.role === 'User'"
              [class.rounded-br-xs]="message.role === 'User'"
              [class.max-w-[85%]]="message.role === 'User'"
              [class.bg-white]="message.role === 'Assistant'"
              [class.dark:bg-slate-800]="message.role === 'Assistant'"
              [class.text-slate-800]="message.role === 'Assistant'"
              [class.dark:text-slate-100]="message.role === 'Assistant'"
              [class.border]="message.role === 'Assistant'"
              [class.border-slate-200]="message.role === 'Assistant'"
              [class.dark:border-slate-700]="message.role === 'Assistant'"
              [class.rounded-bl-xs]="message.role === 'Assistant'"
              [class.max-w-[92%]]="message.role === 'Assistant'"
            >
              @if (message.role === 'User') {
                <p class="m-0 whitespace-pre-wrap">{{ message.content }}</p>
              } @else {
                <div class="prose-normora text-xs leading-relaxed" [innerHTML]="sanitizer.render(message.content, false)"></div>
              }
            </div>

            <!-- Autonomous Agent Tool Chips -->
            @if (message.role === 'Assistant' && message.tools && message.tools.length > 0) {
              <div class="flex flex-wrap gap-1.5 px-1 max-w-[92%]">
                @for (tool of message.tools; track tool.toolCallId) {
                  <div
                    class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[10px] font-medium border shadow-2xs transition-all"
                    [class.bg-primary-50]="tool.status === 'running'"
                    [class.dark:bg-primary-950/40]="tool.status === 'running'"
                    [class.text-primary-700]="tool.status === 'running'"
                    [class.dark:text-primary-300]="tool.status === 'running'"
                    [class.border-primary-200]="tool.status === 'running'"
                    [class.dark:border-primary-800]="tool.status === 'running'"
                    [class.bg-emerald-50]="tool.status === 'completed'"
                    [class.dark:bg-emerald-950/30]="tool.status === 'completed'"
                    [class.text-emerald-700]="tool.status === 'completed'"
                    [class.dark:text-emerald-300]="tool.status === 'completed'"
                    [class.border-emerald-200]="tool.status === 'completed'"
                    [class.dark:border-emerald-800]="tool.status === 'completed'"
                    [class.bg-amber-50]="tool.status === 'failed'"
                    [class.dark:bg-amber-950/30]="tool.status === 'failed'"
                    [class.text-amber-700]="tool.status === 'failed'"
                    [class.dark:text-amber-300]="tool.status === 'failed'"
                    [class.border-amber-200]="tool.status === 'failed'"
                    [class.dark:border-amber-800]="tool.status === 'failed'"
                  >
                    @if (tool.status === 'running') {
                      <i class="pi pi-spin pi-spinner text-[10px] text-primary-500"></i>
                      <span>{{ getToolLabel(tool.toolName) }}...</span>
                    } @else if (tool.status === 'completed') {
                      <i class="pi pi-check text-[10px] text-emerald-500"></i>
                      <span>{{ tool.summary || getToolLabel(tool.toolName) }}</span>
                    } @else {
                      <i class="pi pi-exclamation-triangle text-[10px] text-amber-500"></i>
                      <span>{{ tool.summary || 'Tool execution failed' }}</span>
                    }
                  </div>
                }
              </div>
            }

            <!-- Interactive A2UI Action Card -->
            @if (message.role === 'Assistant' && message.actionCard && !message.actionCard.isDismissed) {
              <div class="p-3 rounded-2xl bg-white dark:bg-slate-800/90 border border-slate-200 dark:border-slate-700 max-w-[92%] shadow-xs space-y-2">
                <div class="flex items-center justify-between gap-2">
                  <div class="flex items-center gap-1.5 min-w-0">
                    <div class="flex items-center justify-center w-5 h-5 rounded-md bg-primary-100 dark:bg-primary-950/60 text-primary-600 dark:text-primary-400 shrink-0">
                      <i class="pi pi-bolt text-[10px]"></i>
                    </div>
                    <span class="text-xs font-bold text-slate-900 dark:text-white truncate">{{ message.actionCard.title }}</span>
                  </div>
                  <span class="inline-flex items-center px-1.5 py-0.5 rounded text-[9px] font-bold uppercase tracking-wider bg-primary-50 dark:bg-primary-950/40 text-primary-600 dark:text-primary-400 border border-primary-200/60 dark:border-primary-800/60 shrink-0">
                    Action
                  </span>
                </div>

                <p class="text-[11px] text-slate-600 dark:text-slate-300 m-0 leading-relaxed">{{ message.actionCard.description }}</p>

                @if (getParamEntries(message.actionCard.parameters).length > 0) {
                  <div class="grid grid-cols-1 sm:grid-cols-2 gap-1.5 pt-0.5">
                    @for (param of getParamEntries(message.actionCard.parameters); track param.key) {
                      <div class="p-1.5 rounded-lg bg-slate-50 dark:bg-slate-900/60 border border-slate-100 dark:border-slate-800 text-[10px]">
                        <span class="text-slate-400 dark:text-slate-500 block font-medium truncate">{{ param.key }}</span>
                        <span class="text-slate-800 dark:text-slate-200 font-semibold break-all">{{ param.value }}</span>
                      </div>
                    }
                  </div>
                }

                <div class="flex items-center gap-2 pt-1 border-t border-slate-100 dark:border-slate-700/60">
                  @if (message.actionCard.isConfirmed) {
                    <div class="flex items-center gap-1 text-[11px] font-semibold text-emerald-600 dark:text-emerald-400">
                      <i class="pi pi-check-circle text-xs"></i>
                      <span>Confirmed</span>
                    </div>
                  } @else {
                    @if (message.actionCard.confirmButtonText) {
                      <button
                        type="button"
                        (click)="confirmAction(message.actionCard)"
                        class="px-2.5 py-1 rounded-lg text-[10px] font-bold bg-primary-600 hover:bg-primary-500 text-white transition-all cursor-pointer shadow-2xs"
                      >
                        {{ message.actionCard.confirmButtonText }}
                      </button>
                    }
                    @if (message.actionCard.cancelButtonText) {
                      <button
                        type="button"
                        (click)="dismissAction(message.actionCard)"
                        class="px-2 py-1 rounded-lg text-[10px] font-medium text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 transition-all cursor-pointer"
                      >
                        {{ message.actionCard.cancelButtonText }}
                      </button>
                    }
                  }
                </div>
              </div>
            }

            <!-- Citations / Sources -->
            @if (message.role === 'Assistant' && message.citations.length > 0) {
              <div class="flex flex-wrap items-center gap-1.5 px-1 max-w-[92%]">
                <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Sources:</span>
                @for (cite of message.citations; track cite.documentId + cite.fileName) {
                  <span
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 shadow-2xs max-w-40 truncate"
                    [dsTooltip]="cite.fileName"
                    dsTooltipPosition="top"
                  >
                    <i class="pi pi-file-pdf text-[10px] text-slate-400 flex-none"></i>
                    <span class="truncate">{{ cite.fileName }}</span>
                    <span class="text-primary-600 dark:text-primary-400 font-bold flex-none">{{ (cite.score * 100).toFixed(0) }}%</span>
                  </span>
                }
              </div>
            }
          </div>
        }

        <!-- Streaming Indicator -->
        @if (copilotService.isSending() && !isReceivingTokens()) {
          <div class="flex items-center gap-2 px-3 py-2 bg-white dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 w-fit text-xs text-slate-500 shadow-2xs">
            <i class="pi pi-spin pi-spinner text-primary-600"></i>
            <span>Consulting company documents...</span>
          </div>
        }

        <!-- Error Notification -->
        @if (copilotService.error()) {
          <div class="p-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/60 rounded-xl text-xs text-red-700 dark:text-red-400 flex items-start gap-2 shadow-2xs">
            <i class="pi pi-exclamation-circle text-sm shrink-0 mt-0.5"></i>
            <span class="flex-1">{{ copilotService.error() }}</span>
          </div>
        }
      </div>

      <!-- Footer / Chat Input -->
      <div class="p-3 border-t border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900 shrink-0">
        <form (ngSubmit)="handleSend()" class="flex flex-col gap-2">
          <div class="relative flex items-center rounded-xl bg-slate-50 dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700 focus-within:border-primary-500 focus-within:ring-2 focus-within:ring-primary-500/20 transition-all">
            <textarea
              #inputArea
              [(ngModel)]="userInput"
              name="copilotInput"
              rows="2"
              (keydown.enter)="handleKeyDown($event)"
              placeholder="Ask anything about company policies..."
              [disabled]="copilotService.isSending()"
              class="w-full px-3.5 py-2.5 text-xs bg-transparent text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 resize-none border-none! outline-none! shadow-none!"
            ></textarea>
            
            <button
              type="submit"
              [disabled]="!userInput.trim() || copilotService.isSending()"
              aria-label="Send query"
              class="absolute right-2.5 bottom-2.5 flex items-center justify-center w-7 h-7 rounded-lg bg-primary-600 hover:bg-primary-500 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-all cursor-pointer shadow-sm"
            >
              @if (copilotService.isSending()) {
                <i class="pi pi-spin pi-spinner text-xs"></i>
              } @else {
                <i class="pi pi-arrow-up text-xs"></i>
              }
            </button>
          </div>

          <div class="flex items-center justify-between px-1 text-[10px] text-slate-400">
            <span>Enter to send • Shift+Enter for newline</span>
            <span>Esc to close</span>
          </div>
        </form>
      </div>
    </aside>
  `
})
export class CopilotSidebarComponent implements AfterViewChecked {
  public copilotService = inject(CopilotService);
  public sanitizer = inject(MarkdownSanitizerService);

  @ViewChild('messageFeed') private messageFeed?: ElementRef<HTMLDivElement>;
  @ViewChild('inputArea') private inputArea?: ElementRef<HTMLTextAreaElement>;

  userInput = '';
  private shouldScrollBottom = false;

  @HostListener('window:keydown', ['$event'])
  handleGlobalShortcuts(event: KeyboardEvent): void {
    // Ctrl + J or Cmd + J opens/closes Copilot
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'j') {
      event.preventDefault();
      this.copilotService.toggle();
      if (this.copilotService.isOpen()) {
        setTimeout(() => this.inputArea?.nativeElement?.focus(), 150);
      }
      return;
    }

    // Esc closes Copilot when open
    if (event.key === 'Escape' && this.copilotService.isOpen()) {
      this.copilotService.close();
    }
  }

  isReceivingTokens(): boolean {
    const list = this.copilotService.messages();
    if (list.length === 0) return false;
    const last = list[list.length - 1];
    return last.role === 'Assistant' && last.content.length > 0;
  }

  handleKeyDown(event: Event): void {
    const keyEvent = event as KeyboardEvent;
    if (!keyEvent.shiftKey) {
      keyEvent.preventDefault();
      this.handleSend();
    }
  }

  handleSend(): void {
    const query = this.userInput.trim();
    if (!query || this.copilotService.isSending()) return;

    this.userInput = '';
    this.shouldScrollBottom = true;
    this.copilotService.sendMessage(query);
  }

  submitPrompt(prompt: string): void {
    this.shouldScrollBottom = true;
    this.copilotService.sendMessage(prompt);
  }

  resetThread(): void {
    this.copilotService.resetConversation();
    setTimeout(() => this.inputArea?.nativeElement?.focus(), 100);
  }

  getToolLabel(toolName: string): string {
    switch (toolName) {
      case 'calculate_policy_limit':
        return 'Calculating Policy Limit';
      case 'escalate_hr_inquiry':
        return 'Dispatching HR Escalation';
      case 'search_company_documents':
        return 'Retrieving Policy Sources';
      default:
        return toolName.replace(/_/g, ' ');
    }
  }

  getParamEntries(params?: Record<string, string>): { key: string; value: string }[] {
    if (!params) return [];
    return Object.entries(params).map(([key, value]) => ({ key, value }));
  }

  confirmAction(card: ActionCardDto): void {
    card.isConfirmed = true;
  }

  dismissAction(card: ActionCardDto): void {
    card.isDismissed = true;
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollBottom || this.copilotService.isSending()) {
      this.scrollToBottom();
      this.shouldScrollBottom = false;
    }
  }

  private scrollToBottom(): void {
    try {
      if (this.messageFeed) {
        this.messageFeed.nativeElement.scrollTop = this.messageFeed.nativeElement.scrollHeight;
      }
    } catch {}
  }
}

