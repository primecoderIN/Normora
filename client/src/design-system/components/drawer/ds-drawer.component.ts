import {
  Component,
  input,
  model,
  output,
  ChangeDetectionStrategy,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  RdxDrawerRoot,
  RdxDrawerPortal,
  RdxDrawerBackdrop,
  RdxDrawerPopup,
  RdxDrawerClose,
} from '@radix-ng/primitives/drawer';

export type DsDrawerPosition = 'right' | 'left' | 'top' | 'bottom';

/**
 * Enterprise Design System Drawer / Slide-Over component.
 * Powered directly by @radix-ng/primitives/drawer headless primitive.
 * Provides accessible modal slide-over interactions, focus-trapping, and gesture support.
 */
@Component({
  selector: 'ds-drawer',
  standalone: true,
  imports: [
    CommonModule,
    RdxDrawerRoot,
    RdxDrawerPortal,
    RdxDrawerBackdrop,
    RdxDrawerPopup,
    RdxDrawerClose,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div rdxDrawerRoot [open]="visible()" (openChange)="onOpenChange($event)">
      <ng-template rdxDrawerPortal>
        <div
          rdxDrawerBackdrop
          (click)="close()"
          class="fixed inset-0 z-overlay bg-slate-950/75 backdrop-blur-xs transition-opacity duration-300 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0"
        ></div>

        <div
          class="fixed inset-0 z-overlay flex pointer-events-none"
          [class]="containerPlacementClasses()"
        >
          <div
            rdxDrawerPopup
            class="pointer-events-auto bg-white dark:bg-slate-900 shadow-2xl border-(--border) flex flex-col h-full overflow-hidden transition-transform duration-300 transform"
            [class]="panelClasses()"
          >
            <!-- Header -->
            <div
              class="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800 shrink-0"
            >
              <div>
                @if (title()) {
                  <h3 class="text-base font-semibold text-slate-900 dark:text-slate-100">
                    {{ title() }}
                  </h3>
                }
                @if (description()) {
                  <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                    {{ description() }}
                  </p>
                }
              </div>

              @if (closable()) {
                <button
                  rdxDrawerClose
                  type="button"
                  class="w-8 h-8 rounded-full flex items-center justify-center text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors focus:outline-none"
                  aria-label="Close drawer"
                >
                  <i class="pi pi-times text-sm" aria-hidden="true"></i>
                </button>
              }
            </div>

            <!-- Content -->
            <div class="p-6 overflow-y-auto custom-scrollbar flex-1">
              <ng-content />
            </div>

            <!-- Footer (optional slot) -->
            <div class="px-6 py-3.5 bg-slate-50 dark:bg-slate-900/50 border-t border-slate-100 dark:border-slate-800 shrink-0 flex items-center justify-end gap-2.5">
              <ng-content select="[dsDrawerFooter]" />
            </div>
          </div>
        </div>
      </ng-template>
    </div>
  `,
})
export class DsDrawerComponent {
  readonly visible = model<boolean>(false);
  readonly position = input<DsDrawerPosition>('right');
  readonly title = input<string | undefined>(undefined);
  readonly description = input<string | undefined>(undefined);
  readonly closable = input<boolean>(true);
  readonly widthClass = input<string>('max-w-md w-full');

  readonly closed = output<void>();

  protected onOpenChange(open: boolean): void {
    this.visible.set(open);
    if (!open) {
      this.closed.emit();
    }
  }

  protected readonly containerPlacementClasses = computed(() => {
    switch (this.position()) {
      case 'left':
        return 'justify-start';
      case 'top':
        return 'items-start justify-center';
      case 'bottom':
        return 'items-end justify-center';
      case 'right':
      default:
        return 'justify-end';
    }
  });

  protected readonly panelClasses = computed(() => {
    const list = [this.widthClass()];
    switch (this.position()) {
      case 'left':
        list.push('border-r animate-in slide-in-from-left duration-300');
        break;
      case 'top':
        list.push('border-b w-full max-h-[80vh] animate-in slide-in-from-top duration-300');
        break;
      case 'bottom':
        list.push('border-t w-full max-h-[80vh] animate-in slide-in-from-bottom duration-300');
        break;
      case 'right':
      default:
        list.push('border-l animate-in slide-in-from-right duration-300');
        break;
    }
    return list.join(' ');
  });

  close(): void {
    this.visible.set(false);
    this.closed.emit();
  }
}
