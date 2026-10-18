import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  template: `
    <div class="group bg-white dark:bg-slate-900 rounded-xl p-5 border border-slate-200 dark:border-slate-800 shadow-xs hover:shadow-md hover:border-slate-300 dark:hover:border-slate-700 transition-all duration-200 flex items-start gap-4 min-h-28">
      <div
        class="w-11 h-11 rounded-xl flex items-center justify-center shrink-0 transition-transform duration-200 group-hover:scale-105 border border-slate-100 dark:border-slate-800"
        [class]="colorClass()"
      >
        <i class="pi text-xl" [class]="icon()"></i>
      </div>
      <div class="min-w-0 flex-1">
        <div class="text-[0.7rem] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">{{ title() }}</div>
        <div class="text-2xl font-bold text-slate-900 dark:text-white leading-tight tabular-nums">{{ value() ?? '—' }}</div>

        @if (trendText()) {
          <div class="mt-2.5 flex items-center">
            <span
              class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[0.68rem] font-semibold tracking-tight"
              [class]="trendPillClass()"
            >
              @if (trend() === 'up') {
                <i class="pi pi-arrow-up text-[0.65rem]"></i>
              } @else if (trend() === 'down') {
                <i class="pi pi-arrow-down text-[0.65rem]"></i>
              } @else {
                <span>—</span>
              }
              {{ trendText() }}
            </span>
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
  colorClass = input<string>('bg-primary-50 dark:bg-primary-950/60 text-primary-600 dark:text-primary-400');

  trend = input<'up' | 'down' | 'neutral'>('neutral');
  trendText = input<string>('');

  trendPillClass = computed(() => {
    switch (this.trend()) {
      case 'up':
        return 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60';
      case 'down':
        return 'bg-rose-50 dark:bg-rose-950/50 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60';
      default:
        return 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-200 dark:border-slate-700';
    }
  });
}

