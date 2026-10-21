import {
  Directive,
  input,
  ElementRef,
  HostListener,
  inject,
  OnDestroy,
  Renderer2,
} from '@angular/core';
import { computePosition, flip, shift, offset, Placement } from '@floating-ui/dom';

export type DsTooltipPosition = 'top' | 'bottom' | 'left' | 'right';

let tooltipIdCounter = 0;

/**
 * Enterprise Design System Tooltip directive.
 * Powered by Floating UI (Radix NG popper infrastructure) for accessible positioning,
 * collision avoidance, viewport boundary detection, and automatic ARIA tagging.
 */
@Directive({
  selector: '[dsTooltip]',
  standalone: true,
})
export class DsTooltipDirective implements OnDestroy {
  readonly dsTooltip = input<string | undefined>(undefined);
  readonly tooltipPosition = input<DsTooltipPosition>('top');
  readonly dsTooltipPosition = input<DsTooltipPosition>('top');
  readonly tooltipDisabled = input<boolean>(false);

  private readonly elementRef = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private tooltipElement: HTMLElement | null = null;
  private showTimeout: any = null;
  private readonly tooltipId = `ds-tooltip-${++tooltipIdCounter}`;

  @HostListener('mouseenter')
  @HostListener('focusin')
  onMouseEnter(): void {
    if (this.tooltipDisabled() || !this.dsTooltip()) return;
    if (this.showTimeout) {
      clearTimeout(this.showTimeout);
    }
    this.showTimeout = setTimeout(() => {
      this.show();
    }, 150);
  }

  @HostListener('mouseleave')
  @HostListener('focusout')
  onMouseLeave(): void {
    this.hide();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.hide();
  }

  private async show(): Promise<void> {
    if (this.tooltipElement || !this.dsTooltip() || typeof document === 'undefined') return;

    const host = this.elementRef.nativeElement as HTMLElement;

    const tooltip = this.renderer.createElement('div') as HTMLElement;
    this.renderer.setAttribute(tooltip, 'id', this.tooltipId);
    this.renderer.setAttribute(tooltip, 'role', 'tooltip');
    tooltip.className =
      'fixed z-[1500] z-tooltip px-2.5 py-1 text-[11px] font-medium text-white bg-slate-900 dark:bg-slate-800 border border-slate-700/50 rounded-md shadow-lg pointer-events-none transition-opacity duration-150 opacity-0 whitespace-nowrap leading-tight';
    tooltip.innerText = this.dsTooltip()!;

    this.renderer.appendChild(document.body, tooltip);
    this.tooltipElement = tooltip;
    this.renderer.setAttribute(host, 'aria-describedby', this.tooltipId);

    const placement: Placement = (this.dsTooltipPosition() || this.tooltipPosition() || 'top') as Placement;

    const { x, y } = await computePosition(host, tooltip, {
      placement,
      middleware: [offset(6), flip(), shift({ padding: 8 })],
    });

    if (this.tooltipElement) {
      this.renderer.setStyle(tooltip, 'left', `${x}px`);
      this.renderer.setStyle(tooltip, 'top', `${y}px`);
      this.renderer.setStyle(tooltip, 'opacity', '1');
    }
  }

  private hide(): void {
    if (this.showTimeout) {
      clearTimeout(this.showTimeout);
      this.showTimeout = null;
    }
    if (this.tooltipElement) {
      const host = this.elementRef.nativeElement as HTMLElement;
      this.renderer.removeAttribute(host, 'aria-describedby');
      this.renderer.removeChild(document.body, this.tooltipElement);
      this.tooltipElement = null;
    }
  }

  ngOnDestroy(): void {
    this.hide();
  }
}
