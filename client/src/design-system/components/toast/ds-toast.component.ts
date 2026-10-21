import { Component, input, inject, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DsToastService, DsActiveToast, DsToastSeverity } from './ds-toast.service';

export type DsToastPosition =
  | 'top-right'
  | 'top-left'
  | 'bottom-right'
  | 'bottom-left'
  | 'top-center'
  | 'bottom-center';

/**
 * Enterprise Toast Container component.
 * Displays real-time notification alerts with accessibility attributes,
 * auto-dismiss, and dark-mode styling.
 */
@Component({
  selector: 'ds-toast',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="containerClasses()" aria-live="assertive" role="region">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          [class]="toastClasses(toast.severity)"
          role="alert"
          class="pointer-events-auto w-full max-w-sm rounded-xl p-4 shadow-xl border backdrop-blur-md flex items-start gap-3 transition-all duration-200 animate-in fade-in slide-in-from-bottom-2"
        >
          <div [class]="iconClasses(toast.severity)" class="shrink-0 mt-0.5" aria-hidden="true">
            <i [class]="severityIcon(toast.severity)" class="text-lg"></i>
          </div>

          <div class="flex-1 min-w-0">
            @if (toast.summary) {
              <h5 class="text-sm font-semibold text-slate-900 dark:text-slate-100 leading-tight">
                {{ toast.summary }}
              </h5>
            }
            @if (toast.detail) {
              <p class="text-xs text-slate-600 dark:text-slate-300 mt-1 leading-relaxed">
                {{ toast.detail }}
              </p>
            }
          </div>

          <button
            type="button"
            (click)="toastService.remove(toast.id)"
            class="shrink-0 p-1 rounded-lg text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors focus:outline-none"
            aria-label="Close notification"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>
      }
    </div>
  `,
})
export class DsToastComponent {
  protected readonly toastService = inject(DsToastService);

  readonly position = input<DsToastPosition>('bottom-right');

  protected readonly containerClasses = computed(() => {
    const base = 'fixed z-1600 pointer-events-none flex flex-col gap-2 p-4 max-w-full';
    switch (this.position()) {
      case 'top-left':
        return `${base} top-0 left-0`;
      case 'top-center':
        return `${base} top-0 left-1/2 -translate-x-1/2`;
      case 'top-right':
        return `${base} top-0 right-0`;
      case 'bottom-left':
        return `${base} bottom-0 left-0`;
      case 'bottom-center':
        return `${base} bottom-0 left-1/2 -translate-x-1/2`;
      case 'bottom-right':
      default:
        return `${base} bottom-0 right-0`;
    }
  });

  protected toastClasses(severity: DsToastSeverity): string {
    switch (severity) {
      case 'success':
        return 'bg-white/95 dark:bg-slate-900/95 border-emerald-500/30 text-emerald-950 dark:text-emerald-50';
      case 'error':
        return 'bg-white/95 dark:bg-slate-900/95 border-red-500/30 text-red-950 dark:text-red-50';
      case 'warn':
      case 'warning':
        return 'bg-white/95 dark:bg-slate-900/95 border-amber-500/30 text-amber-950 dark:text-amber-50';
      case 'info':
      default:
        return 'bg-white/95 dark:bg-slate-900/95 border-blue-500/30 text-blue-950 dark:text-blue-50';
    }
  }

  protected iconClasses(severity: DsToastSeverity): string {
    switch (severity) {
      case 'success':
        return 'text-emerald-500 dark:text-emerald-400';
      case 'error':
        return 'text-red-500 dark:text-red-400';
      case 'warn':
      case 'warning':
        return 'text-amber-500 dark:text-amber-400';
      case 'info':
      default:
        return 'text-blue-500 dark:text-blue-400';
    }
  }

  protected severityIcon(severity: DsToastSeverity): string {
    switch (severity) {
      case 'success':
        return 'pi pi-check-circle';
      case 'error':
        return 'pi pi-times-circle';
      case 'warn':
      case 'warning':
        return 'pi pi-exclamation-triangle';
      case 'info':
      default:
        return 'pi pi-info-circle';
    }
  }
}
