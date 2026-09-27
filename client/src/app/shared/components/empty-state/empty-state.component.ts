import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center p-8 text-center bg-white dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-700 border-dashed min-h-72">
      <div 
        class="w-12 h-12 rounded-lg flex items-center justify-center mb-4"
        [class]="iconBgClass()"
      >
        <i class="pi text-xl" [class]="iconClasses"></i>
      </div>
      
      <h3 class="text-base font-bold text-slate-900 dark:text-white mb-1">{{ title() }}</h3>
      <p class="text-sm text-slate-500 dark:text-slate-400 max-w-sm mb-6">{{ description() }}</p>
      
      @if (actionLabel()) {
        <button 
          (click)="action.emit()"
          class="px-4 py-2 rounded-lg bg-primary-600 text-white font-medium text-sm hover:bg-primary-700 transition-colors flex items-center gap-2 shadow-sm"
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
  
  iconBgClass = input<string>('bg-slate-50 dark:bg-slate-950');
  iconColorClass = input<string>('');
  
  actionLabel = input<string>();
  actionIcon = input<string>();
  
  action = output<void>();

  get iconClasses() {
    return `${this.icon()} ${this.iconColorClass() || 'text-slate-400'}`;
  }
}
