import { Component, input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActionCardDto } from '@core/services/conversation.service';

/**
 * Interactive A2UI Expense & Policy Limit Simulator Card.
 * Allows employees to dynamically calculate reimbursement caps, per-diem allowances,
 * and travel hotel ceilings based on location tiers and trip duration directly in chat.
 */
@Component({
  selector: 'app-expense-simulator-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-3.5 rounded-2xl bg-white dark:bg-slate-800/95 border border-slate-200 dark:border-slate-700 max-w-[95%] shadow-sm space-y-3">
      <!-- Card Header -->
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2 min-w-0">
          <div class="flex items-center justify-center w-6 h-6 rounded-lg bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 shrink-0">
            <i class="pi pi-calculator text-xs"></i>
          </div>
          <div>
            <h4 class="text-xs font-bold text-slate-900 dark:text-white m-0 truncate leading-tight">
              {{ card().title || 'Expense & Travel Policy Calculator' }}
            </h4>
            <p class="text-[10px] text-slate-500 dark:text-slate-400 m-0">Interactive corporate allowance estimator</p>
          </div>
        </div>
        <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[9px] font-bold uppercase tracking-wider bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 border border-emerald-200/60 dark:border-emerald-800/60 shrink-0">
          A2UI Simulator
        </span>
      </div>

      <!-- Simulator Interactive Controls -->
      <div class="p-2.5 rounded-xl bg-slate-50 dark:bg-slate-900/60 border border-slate-100 dark:border-slate-800 space-y-2.5">
        <!-- Tier Selection -->
        <div>
          <label class="block text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-1.5">
            Destination Location Tier
          </label>
          <div class="grid grid-cols-3 gap-1.5">
            @for (tierOption of tiers; track tierOption.id) {
              <button
                type="button"
                (click)="selectedTier.set(tierOption.id)"
                class="px-2 py-1 rounded-lg text-[10px] font-medium border text-center transition-all cursor-pointer truncate"
                [class.bg-emerald-600]="selectedTier() === tierOption.id"
                [class.text-white]="selectedTier() === tierOption.id"
                [class.border-emerald-600]="selectedTier() === tierOption.id"
                [class.shadow-2xs]="selectedTier() === tierOption.id"
                [class.bg-white]="selectedTier() !== tierOption.id"
                [class.dark:bg-slate-800]="selectedTier() !== tierOption.id"
                [class.text-slate-700]="selectedTier() !== tierOption.id"
                [class.dark:text-slate-300]="selectedTier() !== tierOption.id"
                [class.border-slate-200]="selectedTier() !== tierOption.id"
                [class.dark:border-slate-700]="selectedTier() !== tierOption.id"
              >
                {{ tierOption.label }}
              </button>
            }
          </div>
        </div>

        <!-- Nights Counter & Stepper -->
        <div class="flex items-center justify-between gap-3 pt-1">
          <div>
            <span class="block text-[10px] font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              Hotel Nights
            </span>
            <span class="text-xs text-slate-700 dark:text-slate-300 font-medium">
              {{ nights() }} {{ nights() === 1 ? 'night' : 'nights' }} ({{ nights() + 1 }} per-diem days)
            </span>
          </div>
          <div class="flex items-center gap-1.5">
            <button
              type="button"
              (click)="decrementNights()"
              [disabled]="nights() <= 1"
              class="w-6 h-6 rounded-md bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed flex items-center justify-center text-xs transition-colors cursor-pointer"
              aria-label="Decrease nights"
            >
              -
            </button>
            <span class="w-6 text-center text-xs font-bold text-slate-900 dark:text-white tabular-nums">{{ nights() }}</span>
            <button
              type="button"
              (click)="incrementNights()"
              [disabled]="nights() >= 30"
              class="w-6 h-6 rounded-md bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 disabled:opacity-40 disabled:cursor-not-allowed flex items-center justify-center text-xs transition-colors cursor-pointer"
              aria-label="Increase nights"
            >
              +
            </button>
          </div>
        </div>
      </div>

      <!-- Real-time Allowance Dynamic Calculation Output -->
      <div class="p-2.5 rounded-xl bg-emerald-50/70 dark:bg-emerald-950/30 border border-emerald-200/70 dark:border-emerald-800/60">
        <div class="flex items-center justify-between text-xs">
          <span class="font-medium text-slate-600 dark:text-slate-300">Total Max Allowance</span>
          <span class="text-base font-extrabold text-emerald-700 dark:text-emerald-400 tabular-nums">
            \${{ totalAllowance() }}
          </span>
        </div>
        <div class="mt-1.5 pt-1.5 border-t border-emerald-200/50 dark:border-emerald-900/50 grid grid-cols-2 gap-2 text-[10px] text-slate-500 dark:text-slate-400">
          <div>
            <span>Lodging cap:</span>
            <strong class="text-slate-700 dark:text-slate-300 ml-1">\${{ nightlyCap() }}/night</strong>
          </div>
          <div>
            <span>Per diem:</span>
            <strong class="text-slate-700 dark:text-slate-300 ml-1">\${{ dailyPerDiem() }}/day</strong>
          </div>
        </div>
      </div>

      <!-- Interactive Actions -->
      <div class="flex items-center justify-between pt-1 border-t border-slate-100 dark:border-slate-700/60">
        <button
          type="button"
          (click)="copySummary()"
          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[10px] font-semibold border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 transition-all cursor-pointer shadow-2xs"
        >
          <i class="pi text-[10px]" [class.pi-check]="isCopied()" [class.text-emerald-600]="isCopied()" [class.pi-copy]="!isCopied()"></i>
          <span>{{ isCopied() ? 'Summary Copied!' : 'Copy Summary' }}</span>
        </button>

        @if (card().isConfirmed) {
          <span class="inline-flex items-center gap-1 text-[10px] font-bold text-emerald-600 dark:text-emerald-400">
            <i class="pi pi-check-circle text-xs"></i> Limit Applied
          </span>
        } @else {
          <button
            type="button"
            (click)="card().isConfirmed = true"
            class="px-3 py-1 rounded-lg text-[10px] font-bold bg-emerald-600 hover:bg-emerald-500 text-white transition-all cursor-pointer shadow-2xs"
          >
            Apply Calculation
          </button>
        }
      </div>
    </div>
  `
})
export class ExpenseSimulatorCardComponent {
  readonly card = input.required<ActionCardDto>();

  readonly tiers = [
    { id: 'Tier 1' as const, label: 'Tier 1 (Metro)' },
    { id: 'Tier 2' as const, label: 'Tier 2 (Regional)' },
    { id: 'Tier 3' as const, label: 'Tier 3 (Standard)' }
  ];

  selectedTier = signal<'Tier 1' | 'Tier 2' | 'Tier 3'>('Tier 1');
  nights = signal<number>(3);
  isCopied = signal<boolean>(false);

  readonly nightlyCap = computed(() => {
    switch (this.selectedTier()) {
      case 'Tier 1': return 250;
      case 'Tier 2': return 180;
      case 'Tier 3': return 130;
    }
  });

  readonly dailyPerDiem = computed(() => {
    switch (this.selectedTier()) {
      case 'Tier 1': return 85;
      case 'Tier 2': return 65;
      case 'Tier 3': return 50;
    }
  });

  readonly totalAllowance = computed(() => {
    const hotel = this.nights() * this.nightlyCap();
    const meals = (this.nights() + 1) * this.dailyPerDiem();
    return hotel + meals;
  });

  incrementNights(): void {
    if (this.nights() < 30) this.nights.update(n => n + 1);
  }

  decrementNights(): void {
    if (this.nights() > 1) this.nights.update(n => n - 1);
  }

  copySummary(): void {
    const summary = `Expense Policy Pre-Calculation:
Destination: ${this.selectedTier()}
Duration: ${this.nights()} nights (${this.nights() + 1} per-diem days)
Hotel Cap: \$${this.nightlyCap()}/night (\$${this.nights() * this.nightlyCap()} total)
Per Diem: \$${this.dailyPerDiem()}/day (\$${(this.nights() + 1) * this.dailyPerDiem()} total)
Estimated Max Allowance: \$${this.totalAllowance()}`;

    void navigator.clipboard.writeText(summary);
    this.isCopied.set(true);
    setTimeout(() => this.isCopied.set(false), 2500);
  }
}

