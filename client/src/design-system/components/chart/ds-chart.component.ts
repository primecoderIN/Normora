import {
  Component,
  input,
  ElementRef,
  viewChild,
  effect,
  OnDestroy,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  Chart,
  ChartConfiguration,
  ChartType,
  registerables,
} from 'chart.js';

// Register Chart.js components
Chart.register(...registerables);

/**
 * Enterprise Design System Chart component.
 * Replaces PrimeNG Chart with a direct, performant Chart.js integration.
 * Reactively re-renders when data or options change, and cleans up on destroy.
 */
@Component({
  selector: 'ds-chart',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative w-full h-full" [class]="styleClass()">
      <canvas #chartCanvas></canvas>
    </div>
  `,
})
export class DsChartComponent implements OnDestroy {
  readonly type = input<ChartType>('line');
  readonly data = input<any>(undefined);
  readonly options = input<any>(undefined);
  readonly styleClass = input<string>('w-full h-full');

  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chartInstance: Chart | null = null;

  constructor() {
    effect(() => {
      const canvas = this.canvasRef()?.nativeElement;
      const type = this.type();
      const data = this.data();
      const options = this.options();

      if (!canvas || !data) return;

      this.destroyChart();

      const config: ChartConfiguration = {
        type,
        data,
        options: options || {},
      };

      this.chartInstance = new Chart(canvas, config);
    });
  }

  private destroyChart(): void {
    if (this.chartInstance) {
      this.chartInstance.destroy();
      this.chartInstance = null;
    }
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }
}
