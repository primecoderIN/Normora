import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  template: `
    @if (visible()) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm"
        (click)="onCancel()"
        aria-modal="true"
        role="dialog"
        [attr.aria-labelledby]="'confirm-dialog-title'"
      >
        <div
          class="w-full max-w-sm bg-white dark:bg-slate-900 rounded-2xl shadow-2xl shadow-slate-900/20 overflow-hidden flex flex-col animate-[fadeIn_0.15s_ease-out]"
          (click)="$event.stopPropagation()"
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-5 pt-5 pb-4">
            <h3 id="confirm-dialog-title" class="text-base font-bold text-slate-900 dark:text-white m-0">{{ title() }}</h3>
            <button
              type="button"
              class="flex items-center justify-center w-8 h-8 rounded-lg text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              (click)="onCancel()"
              aria-label="Close dialog"
            >
              <i class="pi pi-times text-base"></i>
            </button>
          </div>

          <!-- Divider -->
          <div class="h-px bg-slate-100 dark:bg-slate-800 mx-5"></div>

          <!-- Body -->
          <div class="px-5 pt-4 pb-5">
            <p class="text-sm text-slate-600 dark:text-slate-300 leading-relaxed m-0 mb-4">
              <ng-content></ng-content>
            </p>

            @if (error()) {
              <div class="flex items-start gap-2 p-3 mb-4 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-700 dark:text-red-400">
                <i class="pi pi-exclamation-circle mt-0.5 flex-none"></i>
                <span>{{ error() }}</span>
              </div>
            }

            <div class="flex items-center justify-end gap-2.5 pt-1">
              <button
                type="button"
                class="flex items-center justify-center h-9 px-4 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-200 font-semibold text-sm rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 active:scale-95 transition-all"
                (click)="onCancel()"
              >
                {{ cancelLabel() }}
              </button>
              <button
                type="button"
                class="flex items-center justify-center gap-2 h-9 px-4 font-semibold text-sm rounded-lg active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                [class]="confirmButtonClass()"
                [disabled]="isLoading()"
                (click)="confirm.emit()"
              >
                @if (isLoading()) {
                  <i class="pi pi-spin pi-spinner text-sm"></i>
                }
                {{ confirmLabel() }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfirmDialogComponent {
  visible = input.required<boolean>();
  title = input.required<string>();
  confirmLabel = input<string>('Confirm');
  cancelLabel = input<string>('Cancel');
  isLoading = input<boolean>(false);
  error = input<string | null | undefined>('');
  
  // By default styling for delete/danger, but can be overridden
  confirmButtonClass = input<string>('bg-red-600 border border-red-600 text-white hover:bg-red-700');

  confirm = output<void>();
  cancel = output<void>();

  onCancel() {
    this.cancel.emit();
  }
}
