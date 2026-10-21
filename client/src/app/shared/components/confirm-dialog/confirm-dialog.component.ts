import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DsDialogComponent } from '@design-system';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, DsDialogComponent],
  template: `
    <ds-dialog
      [visible]="visible()"
      (visibleChange)="onCancel()"
      [title]="title()"
      maxWidthClass="max-w-sm"
    >
      <div class="flex flex-col gap-4">
        <p class="text-sm text-slate-600 dark:text-slate-300 leading-relaxed m-0">
          <ng-content></ng-content>
        </p>

        @if (error()) {
          <div class="flex items-start gap-2 p-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-700 dark:text-red-400">
            <i class="pi pi-exclamation-circle mt-0.5 flex-none"></i>
            <span>{{ error() }}</span>
          </div>
        }

        <div class="flex items-center justify-end gap-2.5 pt-3 border-t border-slate-100 dark:border-slate-800">
          <button
            type="button"
            class="flex items-center justify-center h-9 px-4 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 text-slate-700 dark:text-slate-200 font-semibold text-sm rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 active:scale-95 transition-all cursor-pointer"
            (click)="onCancel()"
          >
            {{ cancelLabel() }}
          </button>
          <button
            type="button"
            class="flex items-center justify-center gap-2 h-9 px-4 font-semibold text-sm rounded-lg active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
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
    </ds-dialog>
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
