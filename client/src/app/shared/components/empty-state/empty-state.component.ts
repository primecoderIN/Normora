import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center p-10 text-center bg-white dark:bg-slate-900 rounded-xl border border-dashed border-slate-200 dark:border-slate-700 min-h-72 gap-1">
      <div 
        class="w-14 h-14 rounded-2xl flex items-center justify-center mb-3 shadow-sm"
        [class]="iconBgClass()"
      >
        <i class="pi text-2xl" [class]="iconClasses"></i>
      </div>
      
      <h3 class="text-base font-bold text-slate-900 dark:text-white m-0 mb-1">{{ title() }}</h3>
      <p class="text-sm text-slate-500 dark:text-slate-400 max-w-xs leading-relaxed m-0 mb-5">{{ description() }}</p>
      
      @if (actionLabel()) {
        <button 
          type="button"
          (click)="action.emit()"
          class="flex items-center gap-2 px-5 py-2.5 rounded-lg bg-primary-600 text-white font-semibold text-sm hover:bg-primary-700 active:scale-95 transition-all shadow-sm shadow-primary-500/20"
        >
          @if (actionIcon()) {
            <i class="pi" [class]="actionIcon()"></i>
          }
          {{ actionLabel() }}
        </button>
      }
    </div>
  `
})
export class EmptyStateComponent {
  icon = input.required<string>();
  title = input.required<string>();
  description = input.required<string>();
  
  iconBgClass = input<string>('bg-slate-100 dark:bg-slate-800');
  iconColorClass = input<string>('');
  
  actionLabel = input<string>();
  actionIcon = input<string>();
  
  action = output<void>();

  get iconClasses() {
    return `${this.icon()} ${this.iconColorClass() || 'text-slate-400 dark:text-slate-500'}`;
  }
}
