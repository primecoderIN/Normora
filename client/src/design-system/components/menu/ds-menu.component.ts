import {
  Component,
  input,
  signal,
  ChangeDetectionStrategy,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  RdxMenuRoot,
  RdxMenuPortal,
  RdxMenuPositioner,
  RdxMenuPopup,
  RdxMenuItem,
  RdxMenuSeparator,
} from '@radix-ng/primitives/menu';

export interface DsMenuItem {
  label?: string;
  icon?: string;
  command?: (event?: any) => void;
  disabled?: boolean;
  separator?: boolean;
  styleClass?: string;
}

/**
 * Enterprise Design System Context / Popup Menu component.
 * Built directly on top of @radix-ng/primitives/menu headless primitives.
 * Replaces manual coordinate math, manual focus traps, and fixed backdrops with
 * accessible Radix NG roving focus, popper positioning, and keyboard navigation.
 */
@Component({
  selector: 'ds-menu',
  standalone: true,
  imports: [
    CommonModule,
    RdxMenuRoot,
    RdxMenuPortal,
    RdxMenuPositioner,
    RdxMenuPopup,
    RdxMenuItem,
    RdxMenuSeparator,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div rdxMenuRoot [(open)]="isOpen">
      <ng-template rdxMenuPortal>
        <div
          rdxMenuPositioner
          side="bottom"
          align="end"
          [sideOffset]="4"
          class="z-dropdown invisible data-side:visible"
        >
          <div
            rdxMenuPopup
            class="min-w-40 rounded-(--radius) bg-white dark:bg-slate-900 border border-(--border) shadow-xl py-1.5 select-none"
            [class]="styleClass()"
          >
            @for (item of model(); track $index) {
              @if (item.separator) {
                <div rdxMenuSeparator class="my-1 border-b border-(--border)"></div>
              } @else {
                <button
                  rdxMenuItem
                  type="button"
                  [disabled]="item.disabled"
                  (onSelect)="handleItemClick(item, $event)"
                  class="w-full px-3.5 py-2 text-xs font-medium text-left flex items-center gap-2.5 transition-colors cursor-pointer text-slate-700 dark:text-slate-200 hover:bg-(--accent) dark:hover:bg-(--accent) data-highlighted:bg-(--accent) data-highlighted:text-(--accent-foreground) data-disabled:opacity-50 data-disabled:cursor-not-allowed focus:outline-none"
                  [class]="item.styleClass || ''"
                >
                  @if (item.icon) {
                    <i [class]="item.icon" class="text-xs text-slate-400 dark:text-slate-500 shrink-0" aria-hidden="true"></i>
                  }
                  <span class="truncate flex-1">{{ item.label }}</span>
                </button>
              }
            }
          </div>
        </div>
      </ng-template>
    </div>
  `,
})
export class DsMenuComponent {
  readonly model = input<DsMenuItem[]>([]);
  readonly popup = input<boolean>(true);
  readonly styleClass = input<string>('');

  protected readonly isOpen = signal<boolean>(false);
  private currentPayload: any = undefined;

  private readonly menuRoot = viewChild(RdxMenuRoot);

  toggle(event: MouseEvent, payload?: any): void {
    event.stopPropagation();
    if (this.isOpen()) {
      this.hide();
    } else {
      this.show(event, payload);
    }
  }

  show(event: MouseEvent, payload?: any): void {
    this.currentPayload = payload;
    const target = event.currentTarget as HTMLElement;
    if (target) {
      const root = this.menuRoot();
      if (root) {
        root.registerTrigger(target);
      }
    }
    this.isOpen.set(true);
  }

  hide(): void {
    this.isOpen.set(false);
  }

  protected handleItemClick(item: DsMenuItem, event?: any): void {
    if (item.disabled) return;
    this.hide();
    if (item.command) {
      item.command({ item, originalEvent: event, payload: this.currentPayload });
    }
  }
}
