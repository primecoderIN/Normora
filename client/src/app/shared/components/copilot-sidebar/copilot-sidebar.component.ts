import {
  Component,
  inject,
  ViewChild,
  ElementRef,
  AfterViewChecked,
  HostListener
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DsTooltipDirective } from '@design-system';
import { CopilotService } from '@core/services/copilot.service';
import { ChunkPreviewDrawerComponent } from '../chunk-preview-drawer/chunk-preview-drawer.component';
import { ChatMessageBubbleComponent } from '../chat-message-bubble/chat-message-bubble.component';
import { CopilotHistoryComponent } from './components/copilot-history.component';
import { CopilotInputComponent } from './components/copilot-input.component';

/**
 * Omnipresent Copilot Sidebar component implementing the CopilotKit / AG-UI architecture.
 * Provides a floating, persistent AI copilot accessible across all employer and employee
 * views with page context perception, keyboard shortcuts (Ctrl+J), and real-time streaming.
 */
@Component({
  selector: 'app-copilot-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    DsTooltipDirective,
    ChunkPreviewDrawerComponent,
    ChatMessageBubbleComponent,
    CopilotHistoryComponent,
    CopilotInputComponent
  ],
  template: `
    <!-- Floating Copilot Launch Trigger Pill -->
    @if (!copilotService.isOpen()) {
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
    @if (copilotService.isOpen() && !copilotService.isExpanded()) {
      <div
        (click)="copilotService.close()"
        class="fixed inset-0 z-40 bg-slate-950/50 backdrop-blur-xs md:hidden transition-opacity"
        aria-hidden="true"
      ></div>
    }

    <!-- Slide-Out Copilot Drawer Panel / Fullscreen Workstation -->
    <aside
      class="fixed top-0 right-0 h-full z-50 bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl flex transition-all duration-300 ease-in-out"
      [class.w-full]="copilotService.isExpanded()"
      [class.inset-0]="copilotService.isExpanded()"
      [class.sm:w-[460px]]="!copilotService.isExpanded()"
      [class.md:w-[500px]]="!copilotService.isExpanded()"
      [class.translate-x-0]="copilotService.isOpen()"
      [class.translate-x-full]="!copilotService.isOpen()"
      [class.pointer-events-none]="!copilotService.isOpen()"
      aria-label="Copilot Assistant"
      [attr.aria-hidden]="!copilotService.isOpen()"
    >
      <!-- History Drawer (Always visible in expanded mode, toggleable in docked mode) -->
      @if (copilotService.isExpanded() || copilotService.showHistoryPane()) {
        <app-copilot-history
          [conversations]="copilotService.conversations()"
          [activeConversationId]="copilotService.activeConversationId()"
          [isLoadingHistory]="copilotService.isLoadingHistory()"
          (onSelectThread)="selectThread($event)"
          (onDeleteThread)="deleteThread($event)"
          (onNewThread)="resetThread()"
        />
      }

      <!-- Main Copilot Chat Area -->
      <div class="flex-1 flex flex-col h-full min-w-0">
        <!-- Header -->
        <div class="flex items-center justify-between p-3.5 border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shrink-0">
          <div class="flex items-center gap-2.5 min-w-0">
            <!-- History Toggle Button (Docked Mode) -->
            @if (!copilotService.isExpanded()) {
              <button
                type="button"
                (click)="copilotService.toggleHistoryPane()"
                [dsTooltip]="copilotService.showHistoryPane() ? 'Hide past threads' : 'Show past threads'"
                dsTooltipPosition="bottom"
                class="flex items-center justify-center w-8 h-8 text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors cursor-pointer shrink-0"
              >
                <i class="pi pi-history text-xs"></i>
              </button>
            }

            <div class="flex items-center justify-center w-8 h-8 rounded-xl bg-linear-to-br from-primary-600 to-indigo-600 text-white shrink-0 shadow-xs">
              <i class="pi pi-sparkles text-xs"></i>
            </div>

            <div class="min-w-0">
              <div class="flex items-center gap-1.5">
                <span class="text-xs font-bold text-slate-900 dark:text-white truncate">Normora Copilot</span>
                <span class="inline-flex items-center px-1.5 py-0.2 rounded text-[10px] font-bold bg-primary-50 dark:bg-primary-950/60 text-primary-600 dark:text-primary-400 border border-primary-200/60 dark:border-primary-800/60 shrink-0">
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
              [dsTooltip]="'New conversation'"
              dsTooltipPosition="bottom"
              aria-label="Reset Copilot conversation"
              class="flex items-center justify-center w-8 h-8 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-200/50 dark:hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
            >
              <i class="pi pi-plus text-xs"></i>
            </button>
            <button
              type="button"
              (click)="copilotService.toggleExpanded()"
              [dsTooltip]="copilotService.isExpanded() ? 'Dock to sidebar' : 'Expand to full screen'"
              dsTooltipPosition="bottom"
              [attr.aria-label]="copilotService.isExpanded() ? 'Dock Copilot to sidebar' : 'Expand Copilot to full screen'"
              class="flex items-center justify-center w-8 h-8 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-200/50 dark:hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
            >
              <i class="pi text-xs" [class.pi-window-minimize]="copilotService.isExpanded()" [class.pi-window-maximize]="!copilotService.isExpanded()"></i>
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

        <!-- Persona Quick Selector Strip (Multi-Agent Mesh) -->
        <div class="px-3.5 py-1.5 border-b border-slate-100 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-900/60 flex items-center gap-1.5 overflow-x-auto custom-scrollbar flex-none">
          @for (persona of copilotService.personas; track persona.id) {
            <button
              type="button"
              (click)="copilotService.setPersona(persona.id)"
              class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[10px] font-semibold border transition-all cursor-pointer shrink-0"
              [class.bg-white]="copilotService.activePersonaId() === persona.id"
              [class.dark:bg-slate-800]="copilotService.activePersonaId() === persona.id"
              [class.border-slate-300]="copilotService.activePersonaId() === persona.id"
              [class.dark:border-slate-600]="copilotService.activePersonaId() === persona.id"
              [class.shadow-2xs]="copilotService.activePersonaId() === persona.id"
              [class.text-slate-900]="copilotService.activePersonaId() === persona.id"
              [class.dark:text-white]="copilotService.activePersonaId() === persona.id"
              [class.border-transparent]="copilotService.activePersonaId() !== persona.id"
              [class.text-slate-500]="copilotService.activePersonaId() !== persona.id"
              [class.dark:text-slate-400]="copilotService.activePersonaId() !== persona.id"
              [class.hover:bg-slate-200/50]="copilotService.activePersonaId() !== persona.id"
              [class.dark:hover:bg-slate-800/50]="copilotService.activePersonaId() !== persona.id"
              [dsTooltip]="persona.description"
              dsTooltipPosition="bottom"
            >
              <i [class]="persona.icon + ' text-[10px] ' + persona.colorClass"></i>
              <span>{{ persona.name }}</span>
            </button>
          }
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

          <!-- Message Bubbles (Reusing unified ChatMessageBubbleComponent) -->
          @for (message of copilotService.messages(); track message.id) {
            <app-chat-message-bubble
              [message]="message"
              (onPreviewCitation)="openCitation($event)"
            />
          }

          <!-- Streaming Live Indicator -->
          @if (copilotService.isSending() && copilotService.currentLiveStatus()) {
            <div class="flex items-center gap-2.5 px-3.5 py-2 bg-white dark:bg-slate-800 rounded-xl border border-primary-200/80 dark:border-primary-800/60 w-fit text-xs text-slate-700 dark:text-slate-300 shadow-2xs animate-pulse">
              <i class="pi pi-spin pi-spinner text-primary-600 dark:text-primary-400"></i>
              <span class="font-medium">{{ copilotService.currentLiveStatus()?.message || 'Thinking . . .' }}</span>
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

        <!-- Input Composer -->
        <app-copilot-input
          [isSending]="copilotService.isSending()"
          (onSubmitPrompt)="submitPrompt($event)"
        />
      </div>

      <!-- Document Chunk Preview Drawer -->
      <app-chunk-preview-drawer #chunkDrawer />
    </aside>
  `
})
export class CopilotSidebarComponent implements AfterViewChecked {
  public copilotService = inject(CopilotService);

  @ViewChild('messageFeed') private messageFeed?: ElementRef<HTMLDivElement>;
  @ViewChild('chunkDrawer') private chunkDrawer?: ChunkPreviewDrawerComponent;

  private shouldScrollBottom = false;

  openCitation(chunkId: string): void {
    this.chunkDrawer?.open(chunkId);
  }

  selectThread(id: string): void {
    this.shouldScrollBottom = true;
    this.copilotService.selectConversation(id);
    if (!this.copilotService.isExpanded()) {
      this.copilotService.showHistoryPane.set(false);
    }
  }

  deleteThread(id: string): void {
    this.copilotService.deleteConversation(id);
  }

  resetThread(): void {
    this.copilotService.resetConversation();
  }

  submitPrompt(prompt: string): void {
    this.shouldScrollBottom = true;
    this.copilotService.sendMessage(prompt);
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollBottom && this.messageFeed) {
      this.messageFeed.nativeElement.scrollTop = this.messageFeed.nativeElement.scrollHeight;
      this.shouldScrollBottom = false;
    }
  }

  @HostListener('window:keydown', ['$event'])
  handleGlobalShortcuts(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'j') {
      event.preventDefault();
      this.copilotService.toggle();
    } else if (event.key === 'Escape' && this.copilotService.isOpen()) {
      this.copilotService.close();
    }
  }
}
