import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

export type DsBadgeVariant =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'warning'
  | 'error'
  | 'info'
  | 'neutral';
export type DsBadgeSize = 'sm' | 'md';

/**
 * Enterprise Design System Badge component.
 * Displays compact metadata, statuses, and counts with semantic coloring.
 */
@Component({
  selector: 'ds-badge',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="computedClasses()">
      @if (dot()) {
        <span [class]="computedDotClasses()" aria-hidden="true"></span>
      }
      <ng-content />
    </span>
  `,
})
export class DsBadgeComponent {
  readonly variant = input<DsBadgeVariant>('neutral');
  readonly size = input<DsBadgeSize>('md');
  readonly dot = input<boolean>(false);
  readonly styleClass = input<string>('');

  protected readonly computedClasses = computed(() => {
    const classes = [
      'inline-flex items-center gap-1.5 font-medium rounded-full leading-none select-none',
    ];

    switch (this.size()) {
      case 'sm':
        classes.push('px-2 py-0.5 text-[11px]');
        break;
      case 'md':
      default:
        classes.push('px-2.5 py-1 text-xs');
        break;
    }

    switch (this.variant()) {
      case 'primary':
        classes.push(
          'bg-indigo-50 dark:bg-indigo-950/50 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800'
        );
        break;
      case 'secondary':
        classes.push(
          'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800'
        );
        break;
      case 'success':
        classes.push(
          'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800'
        );
        break;
      case 'warning':
        classes.push(
          'bg-amber-50 dark:bg-amber-950/50 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800'
        );
        break;
      case 'error':
        classes.push(
          'bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800'
        );
        break;
      case 'info':
        classes.push(
          'bg-blue-50 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800'
        );
        break;
      case 'neutral':
      default:
        classes.push(
          'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 border border-slate-200 dark:border-slate-700'
        );
        break;
    }

    if (this.styleClass()) {
      classes.push(this.styleClass());
    }

    return classes.join(' ');
  });

  protected readonly computedDotClasses = computed(() => {
    const base = 'w-1.5 h-1.5 rounded-full shrink-0';
    switch (this.variant()) {
      case 'primary':
        return `${base} bg-indigo-500`;
      case 'secondary':
      case 'success':
        return `${base} bg-emerald-500`;
      case 'warning':
        return `${base} bg-amber-500`;
      case 'error':
        return `${base} bg-red-500`;
      case 'info':
        return `${base} bg-blue-500`;
      case 'neutral':
      default:
        return `${base} bg-slate-400`;
    }
  });
}
