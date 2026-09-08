import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  template: `
    <header class="flex flex-col sm:flex-row sm:items-start justify-between gap-4 mb-8">
      <div>
        @if (eyebrow()) {
          <p class="text-[0.72rem] font-extrabold uppercase tracking-wider text-indigo-600 m-0 mb-1">{{ eyebrow() }}</p>
        }
        <h1 class="text-[1.75rem] font-bold text-surface-900 leading-[1.15] m-0">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="text-[0.9rem] text-surface-500 m-0 mt-1.5">{{ subtitle() }}</p>
        }
      </div>
      @if (actionLabel()) {
        <button type="button" class="flex items-center justify-center gap-2 h-10 px-4 bg-indigo-600 text-white border border-indigo-600 rounded-lg font-bold text-sm hover:bg-indigo-700 transition-colors whitespace-nowrap" (click)="action.emit()">
          @if (actionIcon()) {
            <i [class]="'pi ' + actionIcon()"></i>
          }
          {{ actionLabel() }}
        </button>
      }
    </header>
  `,
})
export class PageHeaderComponent {
  eyebrow = input<string>('');
  title = input.required<string>();
  subtitle = input<string>('');
  actionLabel = input<string>('');
  actionIcon = input<string>('');
  action = output<void>();
}
