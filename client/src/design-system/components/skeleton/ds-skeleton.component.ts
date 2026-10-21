import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Enterprise Design System Skeleton component.
 * Replaces PrimeNG Skeleton with accessible, smooth pulse placeholders.
 */
@Component({
  selector: 'ds-skeleton',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      [class]="computedClasses()"
      [style.width]="shape() === 'circle' ? size() : width()"
      [style.height]="shape() === 'circle' ? size() : height()"
      [style.border-radius]="shape() === 'circle' ? '9999px' : borderRadius()"
      aria-hidden="true"
    ></div>
  `,
})
export class DsSkeletonComponent {
  readonly width = input<string>('100%');
  readonly height = input<string>('1rem');
  readonly size = input<string>('2.5rem');
  readonly borderRadius = input<string>('var(--ds-control-radius)');
  readonly shape = input<'rectangle' | 'circle'>('rectangle');
  readonly styleClass = input<string>('');

  protected readonly computedClasses = computed(() => {
    const classes = [
      'animate-pulse bg-slate-200 dark:bg-slate-800/80 shrink-0 inline-block',
    ];
    if (this.styleClass()) {
      classes.push(this.styleClass());
    }
    return classes.join(' ');
  });
}
