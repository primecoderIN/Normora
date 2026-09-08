import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  template: `
    @if (visible()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-surface-900/40 backdrop-blur-sm" (click)="onCancel()">
        <div class="w-full max-w-sm bg-white rounded-xl shadow-2xl overflow-hidden flex flex-col" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-5 border-b border-surface-100">
            <h3 class="text-lg font-bold text-surface-900 m-0">{{ title() }}</h3>
            <p-button icon="pi pi-times" (onClick)="onCancel()" styleClass="!w-8 !h-8 !p-0 flex items-center justify-center !bg-transparent !border-transparent !text-surface-400 hover:!bg-surface-100 hover:!text-surface-700 rounded-full transition-colors"></p-button>
          </div>
          <div class="p-5">
            <p class="text-[0.95rem] text-surface-700 leading-relaxed m-0 mb-4">
              <ng-content></ng-content>
            </p>
            @if (error()) {
              <div class="flex items-start gap-2 p-3 bg-red-50 border border-red-200 rounded-md text-sm text-red-700 mb-4">
                <i class="pi pi-exclamation-circle mt-0.5"></i>
                <span>{{ error() }}</span>
              </div>
            }
            <div class="flex items-center justify-end gap-3 pt-2">
              <p-button [label]="cancelLabel()" (onClick)="onCancel()" styleClass="!bg-white !border-surface-300 !text-surface-700 hover:!bg-surface-50 font-bold !px-4 !py-2 !h-9 !text-sm transition-colors"></p-button>
              <p-button [label]="confirmLabel()" (onClick)="confirm.emit()" [disabled]="isLoading()" [icon]="isLoading() ? 'pi pi-spin pi-spinner' : ''" [styleClass]="confirmButtonClass() + ' font-bold !px-4 !py-2 !h-9 !text-sm transition-colors disabled:!bg-surface-300 disabled:!border-surface-300 disabled:!text-surface-500'"></p-button>
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
  confirmButtonClass = input<string>('!bg-red-600 !border-red-600 !text-white hover:!bg-red-700');

  confirm = output<void>();
  cancel = output<void>();

  onCancel() {
    this.cancel.emit();
  }
}
