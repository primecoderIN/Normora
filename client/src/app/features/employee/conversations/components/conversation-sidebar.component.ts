import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TooltipModule } from 'primeng/tooltip';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { ConfirmDialogComponent } from '@shared/components/confirm-dialog/confirm-dialog.component';
import { ConversationDto } from '@core/services/conversation.service';

@Component({
  selector: 'app-conversation-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule, TooltipModule, ScrollingModule, ConfirmDialogComponent],
  template: `
    <aside class="flex flex-col flex-none w-72 bg-slate-50 border-r border-slate-200 overflow-hidden shadow-[inset_-1px_0_0_rgba(0,0,0,0.02)]">
      <!-- Header -->
      <div class="flex flex-col p-4 pb-3 border-b border-slate-200 bg-white flex-none gap-3 z-10 shadow-sm relative">
        <div class="flex items-center justify-between gap-2">
          <div>
            <p class="text-[0.65rem] font-black tracking-[0.15em] text-primary-500 uppercase m-0 mb-1">Knowledge Assistant</p>
            <h1 class="text-xl font-black text-slate-900 m-0 tracking-tight">Conversations</h1>
          </div>
          <button
            type="button"
            class="flex items-center justify-center flex-none w-9 h-9 bg-primary-600 hover:bg-primary-700 rounded-xl text-white shadow-[0_2px_8px_rgba(79,70,229,0.3)] hover:shadow-[0_4px_12px_rgba(79,70,229,0.4)] hover:-translate-y-0.5 transition-all border-none cursor-pointer"
            pTooltip="New conversation"
            tooltipPosition="bottom"
            (click)="onNewConversation.emit()"
            aria-label="Start new conversation"
          >
            <i class="pi pi-pen-to-square text-sm"></i>
          </button>
        </div>

        <!-- Search Bar -->
        <div class="relative group">
          <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm group-focus-within:text-primary-500 transition-colors"></i>
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            (ngModelChange)="onSearchChange()"
            placeholder="Search history..." 
            class="w-full bg-slate-50 border border-slate-200 text-slate-900 text-[0.82rem] rounded-lg focus:ring-2 focus:ring-primary-100 focus:border-primary-400 block pl-8 p-2 transition-all outline-none"
          />
          @if (searchQuery()) {
            <button 
              type="button"
              class="absolute right-2 top-1/2 -translate-y-1/2 flex items-center justify-center w-5 h-5 rounded-full hover:bg-slate-200 text-slate-400 border-none cursor-pointer transition-colors"
              (click)="clearSearch()"
            >
              <i class="pi pi-times text-[0.6rem]"></i>
            </button>
          }
        </div>
      </div>

      <!-- List -->
      <div class="flex-1 overflow-hidden p-2 flex flex-col gap-1 relative bg-slate-50">
        @if (isLoading && conversations.length === 0) {
          <div class="flex flex-col gap-2 p-2">
            @for (n of [1,2,3,4,5]; track n) {
              <div class="flex flex-col gap-2 p-3 bg-white border border-slate-100 rounded-xl animate-pulse">
                <div class="h-3 bg-slate-200 rounded-full w-3/4"></div>
                <div class="h-2 bg-slate-100 rounded-full w-1/3"></div>
              </div>
            }
          </div>
        } @else if (error && conversations.length === 0) {
          <div class="flex flex-col items-center justify-center h-full gap-3 p-6 text-center text-red-500">
            <div class="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center">
              <i class="pi pi-exclamation-triangle text-xl text-red-500"></i>
            </div>
            <span class="text-[0.85rem] font-medium">{{ error }}</span>
          </div>
        } @else if (filteredConversations().length === 0) {
          <div class="flex flex-col items-center justify-center h-full gap-3 p-6 text-center text-slate-400">
            <div class="w-14 h-14 rounded-2xl bg-white border border-slate-200 flex items-center justify-center shadow-sm">
              <i class="pi pi-comments text-2xl text-primary-300"></i>
            </div>
            <span class="text-[0.85rem] font-medium">
              {{ searchQuery() ? 'No matching conversations' : 'No history yet' }}
            </span>
          </div>
        } @else {
          <cdk-virtual-scroll-viewport itemSize="68" class="w-full h-full custom-scrollbar" (scrolledIndexChange)="onScrolledIndexChange($event)">
            <button
              *cdkVirtualFor="let conv of filteredConversations(); trackBy: trackById"
              type="button"
              class="group relative flex items-center w-full bg-transparent border border-transparent rounded-xl cursor-pointer gap-3 px-3 py-3 text-left transition-all hover:bg-slate-100 mb-1.5 overflow-hidden"
              [class.!bg-primary-50]="activeId === conv.id"
              [class.!border-primary-100]="activeId === conv.id"
              (click)="onSelect.emit(conv.id)"
            >
              <!-- Active Indicator Strip -->
              @if (activeId === conv.id) {
                <div class="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-8 bg-primary-500 rounded-r-full"></div>
              }

              <div class="flex flex-col flex-1 min-w-0 gap-1 pl-1">
                <span class="text-[0.85rem] font-semibold truncate transition-colors"
                      [class.text-primary-700]="activeId === conv.id"
                      [class.text-slate-800]="activeId !== conv.id">
                  {{ conv.title || 'New conversation' }}
                </span>
                <span class="text-[0.7rem] font-medium"
                      [class.text-primary-400]="activeId === conv.id"
                      [class.text-slate-400]="activeId !== conv.id">
                  {{ getRelativeTime(conv.lastMessageAt) }}
                </span>
              </div>
              
              <button
                type="button"
                class="flex items-center justify-center flex-none w-7 h-7 bg-white border border-slate-200 rounded-md text-slate-400 opacity-0 group-hover:opacity-100 hover:bg-red-50 hover:text-red-600 hover:border-red-200 transition-all cursor-pointer shadow-sm z-10"
                (click)="confirmDelete(conv.id, $event)"
                pTooltip="Delete"
                tooltipPosition="left"
                aria-label="Delete conversation"
              >
                <i class="text-xs" [class.pi-spin]="deletingId === conv.id" [class.pi-spinner]="deletingId === conv.id" [class.pi-trash]="deletingId !== conv.id"></i>
              </button>
            </button>
            
            @if (isLoading && conversations.length > 0) {
              <div class="flex justify-center p-4">
                 <div class="w-5 h-5 border-2 border-slate-300 border-t-indigo-500 rounded-full animate-spin"></div>
              </div>
            }
          </cdk-virtual-scroll-viewport>
        }
      </div>
    </aside>

    <app-confirm-dialog
      [visible]="showDeleteConfirm()"
      title="Delete Conversation"
      confirmLabel="Delete"
      (confirm)="onConfirmDelete()"
      (cancel)="showDeleteConfirm.set(false)"
    >
      Are you sure you want to delete this conversation? This action cannot be undone.
    </app-confirm-dialog>
  `
})
export class ConversationSidebarComponent {
  @Input({ required: true }) conversations: ConversationDto[] = [];
  @Input({ required: true }) activeId: string | null = null;
  @Input({ required: true }) isLoading = false;
  @Input({ required: true }) error = '';
  @Input() deletingId: string | null = null;

  @Output() onSelect = new EventEmitter<string>();
  @Output() onNewConversation = new EventEmitter<void>();
  @Output() onDelete = new EventEmitter<string>();
  @Output() onLoadMore = new EventEmitter<void>();

  searchQuery = signal('');
  showDeleteConfirm = signal(false);
  private conversationToDelete: string | null = null;

  filteredConversations = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return this.conversations;
    return this.conversations.filter(c => 
      c.title.toLowerCase().includes(query) || 
      (c.summary && c.summary.toLowerCase().includes(query))
    );
  });

  onSearchChange() {
    // Angular handles the model binding, computed signal automatically updates
  }

  clearSearch() {
    this.searchQuery.set('');
  }

  trackById(index: number, item: ConversationDto) {
    return item.id;
  }

  onScrolledIndexChange(index: number) {
    if (this.conversations.length > 0 && index + 10 >= this.conversations.length) {
      this.onLoadMore.emit();
    }
  }

  confirmDelete(id: string, event: Event) {
    event.stopPropagation();
    this.conversationToDelete = id;
    this.showDeleteConfirm.set(true);
  }

  onConfirmDelete() {
    if (this.conversationToDelete) {
      this.onDelete.emit(this.conversationToDelete);
      this.conversationToDelete = null;
    }
    this.showDeleteConfirm.set(false);
  }

  getRelativeTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.round(diffMs / 1000);
    const diffMin = Math.round(diffSec / 60);
    const diffHour = Math.round(diffMin / 60);
    const diffDay = Math.round(diffHour / 24);

    if (diffSec < 60) return 'Just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    if (diffHour < 24) return `${diffHour}h ago`;
    if (diffDay === 1) return 'Yesterday';
    if (diffDay < 7) return `${diffDay}d ago`;
    
    // Fallback to absolute date
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}
