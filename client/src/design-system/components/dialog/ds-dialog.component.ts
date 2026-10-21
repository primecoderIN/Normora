import {
  Component,
  input,
  model,
  output,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  RdxDialogRoot,
  RdxDialogPortal,
  RdxDialogBackdrop,
  RdxDialogPopup,
  RdxDialogTitle,
  RdxDialogDescription,
  RdxDialogClose,
} from '@radix-ng/primitives/dialog';

/**
 * Enterprise Design System Modal / Dialog component.
 * Built directly on top of @radix-ng/primitives/dialog headless primitives.
 * Provides accessible focus-trapping, ARIA labeling, backdrop dimming, and clean template slots.
 */
@Component({
  selector: 'ds-dialog',
  standalone: true,
  imports: [
    CommonModule,
    RdxDialogRoot,
    RdxDialogPortal,
    RdxDialogBackdrop,
    RdxDialogPopup,
    RdxDialogTitle,
    RdxDialogDescription,
    RdxDialogClose,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div rdxDialogRoot [open]="visible()" (openChange)="onOpenChange($event)">
      <ng-template rdxDialogPortal>
        <div
          rdxDialogBackdrop
          (click)="close()"
          class="fixed inset-0 z-modal bg-slate-950/75 backdrop-blur-xs transition-opacity duration-200 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0"
        ></div>

        <div class="fixed inset-0 z-modal flex items-center justify-center p-4 pointer-events-none">
          <div
            rdxDialogPopup
            class="pointer-events-auto w-full bg-white dark:bg-slate-900 rounded-xl shadow-2xl border border-(--border) flex flex-col max-h-[90vh] overflow-hidden transition-all duration-200 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95"
            [class]="computedWidthClass()"
          >
            <!-- Header -->
            <div
              class="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800 shrink-0"
            >
              <div>
                @if (title()) {
                  <h3
                    rdxDialogTitle
                    class="text-base font-semibold text-slate-900 dark:text-slate-100"
                  >
                    {{ title() }}
                  </h3>
                }
                @if (description()) {
                  <p
                    rdxDialogDescription
                    class="text-xs text-slate-500 dark:text-slate-400 mt-0.5"
                  >
                    {{ description() }}
                  </p>
                }
              </div>

              @if (closable()) {
                <button
                  rdxDialogClose
                  type="button"
                  class="w-8 h-8 rounded-full flex items-center justify-center text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors focus:outline-none cursor-pointer"
                  aria-label="Close dialog"
                >
                  <i class="pi pi-times text-sm" aria-hidden="true"></i>
                </button>
              }
            </div>

            <!-- Body -->
            <div class="px-6 py-5 overflow-y-auto custom-scrollbar flex-1">
              <ng-content />
            </div>

            <!-- Footer (optional slot) -->
            <div class="empty:hidden px-6 py-3.5 bg-slate-50 dark:bg-slate-900/50 border-t border-slate-100 dark:border-slate-800 shrink-0 flex items-center justify-end gap-2.5">
              <ng-content select="[dsDialogFooter]" />
            </div>
          </div>
        </div>
      </ng-template>
    </div>
  `,
})
export class DsDialogComponent {
  readonly visible = model<boolean>(false);
  readonly title = input<string | undefined>(undefined);
  readonly description = input<string | undefined>(undefined);
  readonly closable = input<boolean>(true);
  readonly maxWidthClass = input<string>('max-w-lg');
  readonly widthClass = input<string | undefined>(undefined);

  readonly closed = output<void>();

  protected readonly computedWidthClass = computed(() => this.widthClass() || this.maxWidthClass());

  protected onOpenChange(open: boolean): void {
    this.visible.set(open);
    if (!open) {
      this.closed.emit();
    }
  }

  close(): void {
    this.visible.set(false);
    this.closed.emit();
  }
}
