import { Component, input } from '@angular/core';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  template: `
    <div class="bg-white rounded-2xl p-5 border border-surface-200 shadow-[0_2px_10px_-4px_rgba(0,0,0,0.05)] flex items-start gap-4">
      <div 
        class="w-12 h-12 rounded-xl flex items-center justify-center shrink-0" 
        [class]="colorClass()"
      >
        <i class="pi text-xl" [class]="icon()"></i>
      </div>
      <div>
        <div class="text-xs font-semibold text-surface-500 mb-0.5">{{ title() }}</div>
        <div class="text-2xl font-bold text-surface-900">{{ value() }}</div>
        
        @if (trendText()) {
          <div 
            class="text-xs font-medium mt-1.5 flex items-center gap-1"
            [class]="trendColorClass"
          >
            @if (trend() === 'up') {
              <i class="pi pi-arrow-up text-[10px]"></i>
            } @else if (trend() === 'down') {
              <i class="pi pi-arrow-down text-[10px]"></i>
            } @else {
              <span class="text-[10px]">—</span>
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
  value = input.required<string | number>();
  icon = input.required<string>();
  colorClass = input<string>('bg-indigo-50 text-indigo-600');
  
  trend = input<'up' | 'down' | 'neutral'>('neutral');
  trendText = input<string>('');

  get trendColorClass() {
    switch (this.trend()) {
      case 'up': return 'text-emerald-500';
      case 'down': return 'text-red-500';
      default: return 'text-surface-400';
    }
  }
}
