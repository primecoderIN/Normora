import { Component, input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  template: `
    <div class="group bg-white dark:bg-slate-900 rounded-xl p-5 border border-slate-200 dark:border-slate-700 shadow-sm hover:shadow-md hover:border-slate-300 dark:hover:border-slate-600 transition-all duration-200 flex items-start gap-4 min-h-28">
      <div
        class="w-11 h-11 rounded-xl flex items-center justify-center shrink-0 transition-transform duration-200 group-hover:scale-105"
        [class]="colorClass()"
      >
        <i class="pi text-xl" [class]="icon()"></i>
      </div>
      <div class="min-w-0 flex-1">
        <div class="text-xs font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">{{ title() }}</div>
        <div class="text-2xl font-bold text-slate-900 dark:text-white leading-tight tabular-nums">{{ value() ?? '—' }}</div>

        @if (trendText()) {
          <div
            class="text-xs font-medium mt-2 flex items-center gap-1"
            [class]="trendColorClass"
          >
            @if (trend() === 'up') {
              <i class="pi pi-arrow-up text-xs"></i>
            } @else if (trend() === 'down') {
              <i class="pi pi-arrow-down text-xs"></i>
            } @else {
              <span>—</span>
            }
            {{ trendText() }}
          </div>
        }
      </div>
    </div>
  `
})
export class StatCardComponent {
  title = input.required<string>();
  value = input.required<string | number | null>();
  icon = input.required<string>();
  colorClass = input<string>('bg-primary-50 text-primary-600');

  trend = input<'up' | 'down' | 'neutral'>('neutral');
  trendText = input<string>('');

  get trendColorClass() {
    switch (this.trend()) {
      case 'up':   return 'text-emerald-500';
      case 'down': return 'text-red-500';
      default:     return 'text-slate-400';
    }
  }
}
