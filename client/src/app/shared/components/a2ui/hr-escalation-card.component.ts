import { Component, input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActionCardDto } from '@core/services/conversation.service';
import { DsTooltipDirective } from '@design-system';

/**
 * Interactive A2UI HR Escalation Stepper Card.
 * Displays live end-to-end escalation lifecycle tracking across Dispatched, Review, and Resolution
 * stages directly inside the chat message feed.
 */
@Component({
  selector: 'app-hr-escalation-card',
  standalone: true,
  imports: [CommonModule, DsTooltipDirective],
  template: `
    <div class="p-3.5 rounded-2xl bg-white dark:bg-slate-800/95 border border-slate-200 dark:border-slate-700 max-w-[95%] shadow-sm space-y-3">
      <!-- Card Header -->
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2 min-w-0">
          <div class="flex items-center justify-center w-6 h-6 rounded-lg bg-indigo-100 dark:bg-indigo-950/60 text-indigo-600 dark:text-indigo-400 shrink-0">
            <i class="pi pi-shield text-xs"></i>
          </div>
          <div>
            <h4 class="text-xs font-bold text-slate-900 dark:text-white m-0 truncate leading-tight">
              {{ card().title || 'HR Administration Escalation' }}
            </h4>
            <span class="text-[10px] text-slate-500 dark:text-slate-400">Reference: #HR-{{ referenceId() }}</span>
          </div>
        </div>
        <span
          class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[9px] font-bold uppercase tracking-wider"
          [class.bg-rose-50]="isUrgent()"
          [class.dark:bg-rose-950/40]="isUrgent()"
          [class.text-rose-600]="isUrgent()"
          [class.dark:text-rose-400]="isUrgent()"
          [class.border-rose-200/60]="isUrgent()"
          [class.dark:border-rose-800/60]="isUrgent()"
          [class.bg-indigo-50]="!isUrgent()"
          [class.dark:bg-indigo-950/40]="!isUrgent()"
          [class.text-indigo-600]="!isUrgent()"
          [class.dark:text-indigo-400]="!isUrgent()"
          [class.border-indigo-200/60]="!isUrgent()"
          [class.dark:border-indigo-800/60]="!isUrgent()"
        >
          <span class="w-1.5 h-1.5 rounded-full" [class.bg-rose-500]="isUrgent()" [class.bg-indigo-500]="!isUrgent()"></span>
          {{ urgencyLevel() }}
        </span>
      </div>

      <!-- Live Lifecycle Progress Stepper -->
      <div class="p-3 rounded-xl bg-slate-50 dark:bg-slate-900/60 border border-slate-100 dark:border-slate-800 space-y-3">
        <span class="block text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">
          Resolution Lifecycle
        </span>

        <div class="relative flex items-center justify-between">
          <!-- Step 1: Dispatched -->
          <div class="flex flex-col items-center gap-1 z-10">
            <div class="w-6 h-6 rounded-full bg-emerald-500 text-white flex items-center justify-center text-[10px] shadow-xs">
              <i class="pi pi-check"></i>
            </div>
            <span class="text-[10px] font-semibold text-slate-800 dark:text-slate-200">Dispatched</span>
            <span class="text-[9px] text-slate-400">Just now</span>
          </div>

          <!-- Connector 1-2 -->
          <div class="absolute left-6 right-1/2 top-3 -translate-y-1/2 h-0.5 bg-indigo-500 z-0"></div>

          <!-- Step 2: Under Review -->
          <div class="flex flex-col items-center gap-1 z-10">
            <div class="w-6 h-6 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px] shadow-xs animate-pulse ring-4 ring-indigo-500/20">
              <i class="pi pi-spin pi-spinner"></i>
            </div>
            <span class="text-[10px] font-semibold text-indigo-600 dark:text-indigo-400">Under Review</span>
            <span class="text-[9px] text-slate-400">People Ops</span>
          </div>

          <!-- Connector 2-3 -->
          <div class="absolute left-1/2 right-6 top-3 -translate-y-1/2 h-0.5 bg-slate-200 dark:bg-slate-700 z-0"></div>

          <!-- Step 3: Resolution -->
          <div class="flex flex-col items-center gap-1 z-10">
            <div class="w-6 h-6 rounded-full bg-slate-200 dark:bg-slate-700 text-slate-400 dark:text-slate-500 flex items-center justify-center text-[10px]">
              <i class="pi pi-clock"></i>
            </div>
            <span class="text-[10px] font-medium text-slate-400 dark:text-slate-500">Resolution</span>
            <span class="text-[9px] text-slate-400">&lt; 1 business day</span>
          </div>
        </div>
      </div>

      <!-- Escalation Summary Metadata -->
      @if (paramList().length > 0) {
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-1.5 text-[10px]">
          @for (item of paramList(); track item.key) {
            <div class="p-1.5 rounded-lg bg-slate-50 dark:bg-slate-900/40 border border-slate-100 dark:border-slate-800">
              <span class="text-slate-400 dark:text-slate-500 block font-medium truncate">{{ item.key }}</span>
              <span class="text-slate-800 dark:text-slate-200 font-semibold break-all">{{ item.value }}</span>
            </div>
          }
        </div>
      }

      <!-- Interactive Actions -->
      <div class="flex items-center justify-between pt-1 border-t border-slate-100 dark:border-slate-700/60">
        <button
          type="button"
          (click)="copyTicketId()"
          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[10px] font-semibold border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 transition-all cursor-pointer shadow-2xs"
        >
          <i class="pi text-[10px]" [class.pi-check]="isCopied()" [class.text-emerald-600]="isCopied()" [class.pi-copy]="!isCopied()"></i>
          <span>{{ isCopied() ? 'ID Copied!' : 'Copy Ticket #HR-' + referenceId() }}</span>
        </button>

        @if (card().isConfirmed) {
          <span class="inline-flex items-center gap-1 text-[10px] font-bold text-emerald-600 dark:text-emerald-400">
            <i class="pi pi-check-circle text-xs"></i> Escalation Acknowledged
          </span>
        } @else {
          <button
            type="button"
            (click)="card().isConfirmed = true"
            class="px-3 py-1 rounded-lg text-[10px] font-bold bg-indigo-600 hover:bg-indigo-500 text-white transition-all cursor-pointer shadow-2xs"
          >
            Acknowledge Escalation
          </button>
        }
      </div>
    </div>
  `
})
export class HrEscalationCardComponent {
  readonly card = input.required<ActionCardDto>();

  isCopied = signal<boolean>(false);

  readonly referenceId = computed(() => {
    const raw = this.card().parameters['Inquiry Subject'] || 'HR';
    let hash = 0;
    for (let i = 0; i < raw.length; i++) {
      hash = (hash << 5) - hash + raw.charCodeAt(i);
      hash |= 0;
    }
    return Math.abs(hash % 9000 + 1000).toString();
  });

  readonly urgencyLevel = computed(() => {
    return this.card().parameters['Urgency Level'] || 'Normal';
  });

  readonly isUrgent = computed(() => {
    return this.urgencyLevel().toLowerCase().includes('urgent') || this.urgencyLevel().toLowerCase().includes('high');
  });

  readonly paramList = computed(() => {
    const params = this.card().parameters;
    if (!params) return [];
    return Object.entries(params)
      .filter(([key]) => key !== 'Dispatch Status')
      .map(([key, value]) => ({ key, value }));
  });

  copyTicketId(): void {
    const text = `HR Escalation Ticket #HR-${this.referenceId()}:
Inquiry: ${this.card().parameters['Inquiry Subject'] || 'General inquiry'}
Urgency: ${this.urgencyLevel()}
Category: ${this.card().parameters['Department Category'] || 'General'}
Status: Under Review (People Ops Administration)`;

    void navigator.clipboard.writeText(text);
    this.isCopied.set(true);
    setTimeout(() => this.isCopied.set(false), 2500);
  }
}
