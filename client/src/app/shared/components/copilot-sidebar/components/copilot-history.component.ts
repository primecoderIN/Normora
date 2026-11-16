import { Component, input, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DsTooltipDirective } from '@design-system';
import { ConversationDto } from '@core/services/conversation.service';

@Component({
  selector: 'app-copilot-history',
  standalone: true,
  imports: [CommonModule, FormsModule, DsTooltipDirective],
  template: `
    <div class="w-64 sm:w-72 shrink-0 border-r border-slate-200 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-950/80 flex flex-col h-full transition-all">
      <!-- History Header -->
      <div class="flex items-center justify-between p-3.5 border-b border-slate-200/80 dark:border-slate-800">
        <div class="flex items-center gap-2">
          <i class="pi pi-history text-primary-600 text-xs"></i>
          <span class="text-xs font-bold text-slate-800 dark:text-white">Past Threads</span>
        </div>
        <button
          type="button"
          (click)="onNewThread.emit()"
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
        @if (isLoadingHistory() && conversations().length === 0) {
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
              (click)="onSelectThread.emit(conv.id)"
              class="group flex items-center justify-between p-2.5 rounded-xl text-xs transition-all cursor-pointer border"
              [class.bg-white]="activeConversationId() === conv.id"
              [class.dark:bg-slate-900]="activeConversationId() === conv.id"
              [class.border-primary-500]="activeConversationId() === conv.id"
              [class.shadow-2xs]="activeConversationId() === conv.id"
              [class.text-primary-700]="activeConversationId() === conv.id"
              [class.dark:text-primary-300]="activeConversationId() === conv.id"
              [class.border-transparent]="activeConversationId() !== conv.id"
              [class.hover:bg-slate-100]="activeConversationId() !== conv.id"
              [class.dark:hover:bg-slate-800/60]="activeConversationId() !== conv.id"
              [class.text-slate-700]="activeConversationId() !== conv.id"
              [class.dark:text-slate-300]="activeConversationId() !== conv.id"
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
  `
})
export class CopilotHistoryComponent {
  conversations = input<ConversationDto[]>([]);
  activeConversationId = input<string | null>(null);
  isLoadingHistory = input<boolean>(false);

  onSelectThread = output<string>();
  onDeleteThread = output<string>();
  onNewThread = output<void>();

  searchHistoryTerm = signal<string>('');

  filteredConversations = computed(() => {
    const term = this.searchHistoryTerm().trim().toLowerCase();
    const list = this.conversations();
    if (!term) return list;
    return list.filter(c => (c.title || '').toLowerCase().includes(term));
  });

  deleteThread(event: Event, id: string): void {
    event.stopPropagation();
    this.onDeleteThread.emit(id);
  }

  formatRelativeTime(isoDate?: string): string {
    if (!isoDate) return '';
    try {
      const date = new Date(isoDate);
      const diffMs = Date.now() - date.getTime();
      const diffMins = Math.floor(diffMs / 60000);
      if (diffMins < 1) return 'Just now';
      if (diffMins < 60) return `${diffMins}m ago`;
      const diffHours = Math.floor(diffMins / 60);
      if (diffHours < 24) return `${diffHours}h ago`;
      const diffDays = Math.floor(diffHours / 24);
      if (diffDays < 7) return `${diffDays}d ago`;
      return date.toLocaleDateString();
    } catch {
      return '';
    }
  }
}
