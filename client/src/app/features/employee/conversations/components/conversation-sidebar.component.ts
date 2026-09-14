import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { ConversationDto } from '@core/services/conversation.service';

@Component({
  selector: 'app-conversation-sidebar',
  standalone: true,
  imports: [CommonModule, TooltipModule, DatePipe],
  template: `
    <aside class="flex flex-col flex-none w-65 bg-white border-r border-surface-200 overflow-hidden">
      <!-- Header -->
      <div class="flex items-center justify-between p-4 pb-3 border-b border-surface-200 flex-none gap-2">
        <div>
          <p class="text-[0.68rem] font-extrabold tracking-widest text-indigo-600 uppercase m-0 mb-1">Knowledge assistant</p>
          <h1 class="text-lg font-extrabold text-surface-900 m-0">Conversations</h1>
        </div>
        <button
          type="button"
          class="flex items-center justify-center flex-none w-8 h-8 bg-indigo-50 hover:bg-indigo-100 rounded-lg text-indigo-600 hover:scale-105 transition-all border-none cursor-pointer"
          pTooltip="New conversation"
          tooltipPosition="right"
          (click)="onNewConversation.emit()"
          aria-label="Start new conversation"
        >
          <i class="pi pi-plus text-sm"></i>
        </button>
      </div>

      <!-- List -->
      <div class="flex-1 overflow-y-auto p-2 flex flex-col gap-1 custom-scrollbar">
        @if (isLoading) {
          <div class="flex flex-col gap-2 p-2">
            @for (n of [1,2,3,4]; track n) {
              <div class="flex flex-col gap-1.5 p-3 bg-surface-50 rounded-lg animate-pulse">
                <div class="h-3 bg-surface-200 rounded w-4/5"></div>
                <div class="h-2.5 bg-surface-200 rounded w-1/2"></div>
              </div>
            }
          </div>
        } @else if (error) {
          <div class="flex flex-col items-center gap-2 m-auto p-8 text-center text-red-500">
            <i class="pi pi-exclamation-circle text-2xl text-red-300"></i>
            <span class="text-sm">{{ error }}</span>
          </div>
        } @else if (conversations.length === 0) {
          <div class="flex flex-col items-center gap-2 m-auto p-8 text-center text-surface-400">
            <i class="pi pi-comments text-2xl text-indigo-200"></i>
            <span class="text-sm">No conversations yet.<br>Click <strong class="text-surface-600">+</strong> to start one.</span>
          </div>
        } @else {
          @for (conv of conversations; track conv.id) {
            <button
              type="button"
              class="group flex items-center w-full bg-transparent border border-transparent rounded-lg cursor-pointer gap-2 px-2.5 py-2.5 text-left transition-all hover:bg-surface-50 hover:border-surface-200"
              [class.!bg-indigo-50]="activeId === conv.id"
              [class.!border-indigo-200]="activeId === conv.id"
              (click)="onSelect.emit(conv.id)"
            >
              <div class="flex flex-col flex-1 min-w-0 gap-0.5">
                <span class="text-[0.82rem] font-semibold truncate transition-colors"
                      [class.text-indigo-700]="activeId === conv.id"
                      [class.text-surface-800]="activeId !== conv.id">
                  {{ conv.title }}
                </span>
                <span class="text-[0.7rem] text-surface-400">{{ conv.lastMessageAt | date:'MMM d' }}</span>
              </div>
              <button
                type="button"
                class="flex items-center justify-center flex-none w-6 h-6 bg-transparent border-none rounded text-surface-300 opacity-0 group-hover:opacity-100 hover:bg-red-50 hover:text-red-500 transition-all cursor-pointer"
                (click)="onDelete.emit(conv.id); $event.stopPropagation()"
                pTooltip="Delete"
                tooltipPosition="right"
                aria-label="Delete conversation"
              >
                <i class="text-xs" [class.pi-spin]="deletingId === conv.id" [class.pi-spinner]="deletingId === conv.id" [class.pi-trash]="deletingId !== conv.id"></i>
              </button>
            </button>
          }
        }
      </div>
    </aside>
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
}
