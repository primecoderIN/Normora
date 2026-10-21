import { Component, input, output, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RdxButtonDirective } from '@radix-ng/primitives/button';

export type DsButtonVariant = 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
export type DsButtonSize = 'sm' | 'md' | 'lg';

/**
 * Enterprise Design System Button component.
 * Built directly on top of @radix-ng/primitives/button headless directive.
 * Provides accessible, keyboard-friendly buttons with consistent brand styling,
 * interactive states, dark mode variants, and loading indicators.
 */
@Component({
  selector: 'ds-button',
  standalone: true,
  imports: [CommonModule, RdxButtonDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      rdxButton
      [type]="type()"
      [disabled]="isDisabled()"
      [attr.aria-label]="ariaLabel() || null"
      [attr.aria-busy]="loading() ? 'true' : null"
      (click)="handleClick($event)"
      [class]="computedClasses()"
    >
      @if (loading()) {
        <svg
          class="animate-spin -ml-0.5 mr-2 h-4 w-4 shrink-0 text-current"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      } @else if (icon() && iconPos() === 'left') {
        <i [class]="icon()" class="mr-2 text-current text-sm shrink-0" aria-hidden="true"></i>
      }

      <span class="inline-flex items-center gap-1.5 leading-none">
        <ng-content />
      </span>

      @if (!loading() && icon() && iconPos() === 'right') {
        <i [class]="icon()" class="ml-2 text-current text-sm shrink-0" aria-hidden="true"></i>
      }
    </button>
  `,
})
export class DsButtonComponent {
  readonly variant = input<DsButtonVariant>('primary');
  readonly size = input<DsButtonSize>('md');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly icon = input<string | undefined>(undefined);
  readonly iconPos = input<'left' | 'right'>('left');
  readonly fullWidth = input<boolean>(false);
  readonly ariaLabel = input<string | undefined>(undefined);
  readonly styleClass = input<string>('');

  readonly clicked = output<MouseEvent>();

  protected readonly isDisabled = computed(() => this.disabled() || this.loading());

  protected readonly computedClasses = computed(() => {
    const base = [
      'inline-flex items-center justify-center font-medium select-none',
      'rounded-(--radius) transition-all duration-150 ease-in-out',
      'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
      'focus-visible:ring-(--ring) dark:focus-visible:ring-offset-slate-900',
      'disabled:cursor-not-allowed disabled:opacity-50 disabled:shadow-none',
      'data-disabled:cursor-not-allowed data-disabled:opacity-50 data-disabled:shadow-none',
      'cursor-pointer',
    ];

    if (this.fullWidth()) {
      base.push('w-full');
    }

    // Size styles
    switch (this.size()) {
      case 'sm':
        base.push('h-(--ds-control-height-sm) px-3 text-xs');
        break;
      case 'lg':
        base.push('h-(--ds-control-height-lg) px-5 text-base');
        break;
      case 'md':
      default:
        base.push('h-(--ds-control-height-md) px-4 text-sm');
        break;
    }

    // Variant styles
    switch (this.variant()) {
      case 'secondary':
        base.push(
          'bg-slate-100 dark:bg-slate-800 text-slate-800 dark:text-slate-200',
          'hover:bg-slate-200 dark:hover:bg-slate-700 active:bg-slate-300 dark:active:bg-slate-600',
          'border border-slate-200 dark:border-slate-700 shadow-xs'
        );
        break;

      case 'outline':
        base.push(
          'bg-transparent text-slate-700 dark:text-slate-200',
          'border border-(--input) hover:border-(--ds-input-border-hover)',
          'hover:bg-slate-50 dark:hover:bg-slate-800/60 active:bg-slate-100 dark:active:bg-slate-800'
        );
        break;

      case 'ghost':
        base.push(
          'bg-transparent text-slate-600 dark:text-slate-300 border-transparent',
          'hover:bg-slate-100 dark:hover:bg-slate-800 hover:text-slate-900 dark:hover:text-slate-100',
          'active:bg-slate-200 dark:active:bg-slate-700'
        );
        break;

      case 'danger':
        base.push(
          'bg-(--destructive) text-(--destructive-foreground) border-transparent',
          'hover:bg-red-700 active:bg-red-800 shadow-xs',
          'focus-visible:ring-(--destructive)'
        );
        break;

      case 'primary':
      default:
        base.push(
          'bg-(--primary) text-(--primary-foreground) border-transparent',
          'hover:bg-(--primary-hover) active:brightness-90 shadow-xs',
          'focus-visible:ring-(--ring)'
        );
        break;
    }

    if (this.styleClass()) {
      base.push(this.styleClass());
    }

    return base.join(' ');
  });

  protected handleClick(event: MouseEvent): void {
    if (this.isDisabled()) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    this.clicked.emit(event);
  }
}
