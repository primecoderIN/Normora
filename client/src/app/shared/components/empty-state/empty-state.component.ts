import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex flex-col items-center justify-center p-8 text-center bg-white rounded-xl border border-surface-200 border-dashed">
      <div 
        class="w-16 h-16 rounded-full flex items-center justify-center mb-4"
        [class]="iconBgClass()"
      >
        <i class="pi text-2xl" [class]="icon()" [class.text-surface-400]="!iconColorClass()" [class]="iconColorClass()"></i>
      </div>
      
      <h3 class="text-lg font-bold text-surface-900 mb-1">{{ title() }}</h3>
      <p class="text-sm text-surface-500 max-w-sm mb-6">{{ description() }}</p>
      
      @if (actionLabel()) {
        <button 
          (click)="action.emit()"
          class="px-4 py-2 rounded-xl bg-indigo-600 text-white font-medium text-sm hover:bg-indigo-700 transition-colors flex items-center gap-2 shadow-sm"
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
  
  iconBgClass = input<string>('bg-surface-50');
  iconColorClass = input<string>('');
  
  actionLabel = input<string>();
  actionIcon = input<string>();
  
  action = output<void>();
}
