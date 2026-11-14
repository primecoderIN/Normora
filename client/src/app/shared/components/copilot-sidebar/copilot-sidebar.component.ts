import {
  Component,
  inject,
  signal,
  computed,
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
import { ActionCardDto, ConversationService } from '@core/services/conversation.service';
import { ChunkPreviewDrawerComponent } from '../chunk-preview-drawer/chunk-preview-drawer.component';
import { ExpenseSimulatorCardComponent } from '../a2ui/expense-simulator-card.component';
import { HrEscalationCardComponent } from '../a2ui/hr-escalation-card.component';
import { IntakeFormCardComponent } from '../a2ui/intake-form-card.component';

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
    FormsModule, 
    DsTooltipDirective, 
    ChunkPreviewDrawerComponent,
    ExpenseSimulatorCardComponent,
    HrEscalationCardComponent,
    IntakeFormCardComponent
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
        <div class="w-64 sm:w-72 shrink-0 border-r border-slate-200 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-950/80 flex flex-col h-full transition-all">
          <!-- History Header -->
          <div class="flex items-center justify-between p-3.5 border-b border-slate-200/80 dark:border-slate-800">
            <div class="flex items-center gap-2">
              <i class="pi pi-history text-primary-600 text-xs"></i>
              <span class="text-xs font-bold text-slate-800 dark:text-white">Past Threads</span>
            </div>
            <button
              type="button"
              (click)="resetThread()"
              class="flex items-center gap-1 px-2.5 py-1 text-[11px] font-semibold bg-primary-600 hover:bg-primary-500 text-white rounded-lg shadow-2xs cursor-pointer transition-all"
            >
              <i class="pi pi-plus text-[10px]"></i>
              <span>New</span>
            </button>
          </div>

          <!-- Search Threads -->
          <div class="p-2.5 border-b border-slate-200/60 dark:border-slate-800/80">
            <div class="relative flex items-center">
              <i class="pi pi-search absolute left-2.5 text-[11px] text-slate-400"></i>
              <input
                type="text"
                [ngModel]="searchHistoryTerm()"
                (ngModelChange)="searchHistoryTerm.set($event)"
                placeholder="Search threads..."
                class="w-full pl-7 pr-2.5 py-1.5 text-xs bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700/80 rounded-lg text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-primary-500 transition-all"
              />
            </div>
          </div>

          <!-- Threads List -->
          <div class="flex-1 overflow-y-auto p-2 space-y-1 custom-scrollbar">
            @if (copilotService.isLoadingHistory() && copilotService.conversations().length === 0) {
              <div class="flex items-center justify-center py-8 text-xs text-slate-400 gap-2">
                <i class="pi pi-spin pi-spinner text-primary-500"></i>
                <span>Loading threads...</span>
              </div>
            } @else if (filteredConversations().length === 0) {
              <div class="text-center py-8 px-4 text-xs text-slate-400">
                <i class="pi pi-comments text-lg block mb-1 text-slate-300 dark:text-slate-600"></i>
                <span>No conversation threads found.</span>
              </div>
            } @else {
              @for (conv of filteredConversations(); track conv.id) {
                <div
                  (click)="selectThread(conv.id)"
                  class="group flex items-center justify-between p-2.5 rounded-xl text-xs transition-all cursor-pointer border"
                  [class.bg-white]="copilotService.activeConversationId() === conv.id"
                  [class.dark:bg-slate-900]="copilotService.activeConversationId() === conv.id"
                  [class.border-primary-500]="copilotService.activeConversationId() === conv.id"
                  [class.shadow-2xs]="copilotService.activeConversationId() === conv.id"
                  [class.text-primary-700]="copilotService.activeConversationId() === conv.id"
                  [class.dark:text-primary-300]="copilotService.activeConversationId() === conv.id"
                  [class.border-transparent]="copilotService.activeConversationId() !== conv.id"
                  [class.hover:bg-slate-100]="copilotService.activeConversationId() !== conv.id"
                  [class.dark:hover:bg-slate-800/60]="copilotService.activeConversationId() !== conv.id"
                  [class.text-slate-700]="copilotService.activeConversationId() !== conv.id"
                  [class.dark:text-slate-300]="copilotService.activeConversationId() !== conv.id"
                >
                  <div class="min-w-0 flex-1 pr-1.5">
                    <p class="font-medium truncate m-0">{{ conv.title || 'Conversation' }}</p>
                    <span class="text-[10px] text-slate-400 block mt-0.5">{{ formatRelativeTime(conv.lastMessageAt || conv.createdAt) }}</span>
                  </div>
                  <button
                    type="button"
                    (click)="deleteThread($event, conv.id)"
                    [dsTooltip]="'Delete thread'"
                    dsTooltipPosition="left"
                    class="opacity-0 group-hover:opacity-100 p-1 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/40 rounded transition-all cursor-pointer shrink-0"
                  >
                    <i class="pi pi-trash text-[11px]"></i>
                  </button>
                </div>
              }
            }
          </div>
        </div>
      }

      <!-- Main Chat Experience Pane -->
      <div class="flex-1 flex flex-col h-full min-w-0 bg-white dark:bg-slate-900">
        <!-- Header -->
        <div class="flex items-center justify-between px-5 py-3.5 border-b border-slate-100 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-900/90 backdrop-blur-sm shrink-0">
          <div class="flex items-center gap-2.5 min-w-0">
            <!-- History Toggle Button in Docked Mode -->
            @if (!copilotService.isExpanded()) {
              <button
                type="button"
                (click)="copilotService.toggleHistoryPane()"
                [dsTooltip]="copilotService.showHistoryPane() ? 'Hide past threads' : 'Past threads'"
                dsTooltipPosition="bottom"
                class="flex items-center justify-center w-8 h-8 rounded-lg transition-colors cursor-pointer"
                [class.text-primary-600]="copilotService.showHistoryPane()"
                [class.bg-primary-50]="copilotService.showHistoryPane()"
                [class.dark:bg-primary-950/40]="copilotService.showHistoryPane()"
                [class.text-slate-400]="!copilotService.showHistoryPane()"
                [class.hover:text-slate-700]="!copilotService.showHistoryPane()"
                [class.dark:hover:text-slate-200]="!copilotService.showHistoryPane()"
                [class.hover:bg-slate-200/50]="!copilotService.showHistoryPane()"
                [class.dark:hover:bg-slate-800]="!copilotService.showHistoryPane()"
              >
                <i class="pi pi-history text-xs"></i>
              </button>
            }

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
              @if (message.actionCard.actionType === 'policy_limit_card' || message.actionCard.cardType === 'policy_limit_card') {
                <app-expense-simulator-card [card]="message.actionCard" />
              } @else if (message.actionCard.actionType === 'hr_escalation_card' || message.actionCard.cardType === 'hr_escalation_card') {
                <app-hr-escalation-card [card]="message.actionCard" />
              } @else if (message.actionCard.actionType === 'intake_form_card') {
                <app-intake-form-card [card]="message.actionCard" />
              } @else {
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
                        <span>Confirmed & Executed</span>
                      </div>
                    } @else if (message.actionCard.isDismissed) {
                      <div class="flex items-center gap-1 text-[11px] font-medium text-slate-500 dark:text-slate-400">
                        <i class="pi pi-times-circle text-xs"></i>
                        <span>Dismissed</span>
                      </div>
                    } @else {
                      @if (message.actionCard.confirmButtonText) {
                        <button
                          type="button"
                          [disabled]="message.actionCard.isExecuting"
                          (click)="confirmAction(message.actionCard)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-[10px] font-bold bg-primary-600 hover:bg-primary-500 text-white transition-all cursor-pointer shadow-2xs disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          @if (message.actionCard.isExecuting) {
                            <i class="pi pi-spin pi-spinner text-[10px]"></i>
                          }
                          <span>{{ message.actionCard.confirmButtonText }}</span>
                        </button>
                      }
                      @if (message.actionCard.cancelButtonText) {
                        <button
                          type="button"
                          [disabled]="message.actionCard.isExecuting"
                          (click)="dismissAction(message.actionCard)"
                          class="px-2 py-1 rounded-lg text-[10px] font-medium text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 transition-all cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          {{ message.actionCard.cancelButtonText }}
                        </button>
                      }
                      @if (message.actionCard.executionError) {
                        <span class="text-[10px] text-red-500 dark:text-red-400 font-medium">
                          {{ message.actionCard.executionError }}
                        </span>
                      }
                    }
                  </div>
                </div>
              }
            }

            <!-- Citations / Sources -->
            @if (message.role === 'Assistant' && message.citations.length > 0) {
              <div class="flex flex-wrap items-center gap-1.5 px-1 max-w-[92%]">
                <span class="text-[10px] font-bold text-slate-400 uppercase tracking-wider">Sources:</span>
                @for (cite of message.citations; track cite.documentId + cite.fileName) {
                  <button
                    type="button"
                    (click)="openCitation(cite.documentChunkId)"
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-medium border bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-700 hover:border-primary-400 dark:hover:border-primary-500 text-slate-600 dark:text-slate-300 shadow-2xs max-w-44 truncate cursor-pointer transition-colors"
                    [dsTooltip]="'Preview source chunk from ' + cite.fileName"
                    dsTooltipPosition="top"
                  >
                    <i class="pi pi-file-pdf text-[10px] text-primary-500 flex-none"></i>
                    <span class="truncate">{{ cite.fileName }}</span>
                    <span class="text-primary-600 dark:text-primary-400 font-bold flex-none">{{ (cite.score * 100).toFixed(0) }}%</span>
                  </button>
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
      </div>

      <!-- Document Chunk Preview Drawer -->
      <app-chunk-preview-drawer #chunkDrawer />
    </aside>
  `
})
export class CopilotSidebarComponent implements AfterViewChecked {
  public copilotService = inject(CopilotService);
  public conversationService = inject(ConversationService);
  public sanitizer = inject(MarkdownSanitizerService);

  @ViewChild('messageFeed') private messageFeed?: ElementRef<HTMLDivElement>;
  @ViewChild('inputArea') private inputArea?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('chunkDrawer') private chunkDrawer?: ChunkPreviewDrawerComponent;

  userInput = '';
  searchHistoryTerm = signal<string>('');
  private shouldScrollBottom = false;

  openCitation(chunkId: string): void {
    this.chunkDrawer?.open(chunkId);
  }

  filteredConversations = computed(() => {
    const term = this.searchHistoryTerm().trim().toLowerCase();
    const list = this.copilotService.conversations();
    if (!term) return list;
    return list.filter(c => (c.title || '').toLowerCase().includes(term));
  });

  selectThread(id: string): void {
    this.shouldScrollBottom = true;
    this.copilotService.selectConversation(id);
    if (!this.copilotService.isExpanded()) {
      this.copilotService.showHistoryPane.set(false);
    }
  }

  deleteThread(event: Event, id: string): void {
    event.stopPropagation();
    this.copilotService.deleteConversation(id);
  }

  formatRelativeTime(isoDate?: string): string {
    if (!isoDate) return '';
    try {
      const now = new Date().getTime();
      const past = new Date(isoDate).getTime();
      const diffMs = now - past;
      const diffMins = Math.floor(diffMs / (1000 * 60));
      if (diffMins < 1) return 'Just now';
      if (diffMins < 60) return `${diffMins}m ago`;
      const diffHours = Math.floor(diffMins / 60);
      if (diffHours < 24) return `${diffHours}h ago`;
      const diffDays = Math.floor(diffHours / 24);
      if (diffDays < 7) return `${diffDays}d ago`;
      return new Date(isoDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
    } catch {
      return '';
    }
  }

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
    if (card.approvalId) {
      card.isExecuting = true;
      card.executionError = undefined;
      this.conversationService.confirmApproval(card.approvalId).subscribe({
        next: (res) => {
          card.isExecuting = false;
          if (res.success) {
            card.isConfirmed = true;
            this.handleActuationTriggers(card);
          } else {
            card.executionError = res.message || 'Execution failed.';
          }
        },
        error: (err) => {
          card.isExecuting = false;
          card.executionError = err?.error?.message || 'Authorization failed.';
        }
      });
    } else {
      card.isConfirmed = true;
      this.handleActuationTriggers(card);
    }
  }

  private handleActuationTriggers(card: ActionCardDto): void {
    if (card.parameters && card.parameters['route']) {
      this.copilotService.navigateActuation(card.parameters['route']);
    } else if (card.parameters && card.parameters['modal']) {
      this.copilotService.triggerModal(card.parameters['modal']);
    }
  }

  dismissAction(card: ActionCardDto): void {
    if (card.approvalId) {
      card.isExecuting = true;
      this.conversationService.dismissApproval(card.approvalId).subscribe({
        next: () => {
          card.isExecuting = false;
          card.isDismissed = true;
        },
        error: () => {
          card.isExecuting = false;
          card.isDismissed = true;
        }
      });
    } else {
      card.isDismissed = true;
    }
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

