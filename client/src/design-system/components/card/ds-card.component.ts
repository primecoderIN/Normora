import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Enterprise Design System Card component.
 * Provides unified surface containers with consistent borders, radii, and shadows.
 */
@Component({
  selector: 'ds-card',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      [class]="'bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl shadow-xs overflow-hidden ' + styleClass()"
    >
      <ng-content />
    </div>
  `,
})
export class DsCardComponent {
  readonly styleClass = input<string>('');
}
