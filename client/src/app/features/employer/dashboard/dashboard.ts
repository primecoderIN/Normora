import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ChartData, ChartOptions } from 'chart.js';

import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { UserService } from '@core/services/user.service';
import { DashboardService, DashboardSummaryDto } from '@core/services/dashboard.service';
import { CopilotService } from '@core/services/copilot.service';
import { ApiMessages } from '@core/constants/api-messages';
import {
  DsChartComponent,
  DsButtonComponent,
  DsSkeletonComponent,
  DsToastService,
  DsInputComponent,
} from '@design-system';

/**
 * Dashboard component displaying high-level tenant metrics:
 * document coverage breakdown (donut chart), 30-day chat conversation activity (line chart),
 * and aggregate KPI stat cards.
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    RouterLink,
    DsChartComponent,
    StatCardComponent,
    DsButtonComponent,
    DsSkeletonComponent,
    DsInputComponent,
  ],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  public userService = inject(UserService);
  private dashboardService = inject(DashboardService);
  private messageService = inject(DsToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  public copilotService = inject(CopilotService);
  
  summary = signal<DashboardSummaryDto | null>(null);
  isLoading = signal(true);
  searchWorkspaceQuery = signal('');

  documentChartData: ChartData<'doughnut'> | null = null;
  documentChartOptions: ChartOptions<'doughnut'> | null = null;

  activityChartData: ChartData<'line'> | null = null;
  activityChartOptions: ChartOptions<'line'> | null = null;

  ngOnInit() {
    // Executive Workspace Metrics Aggregation:
    // Dispatches GET /api/dashboard/summary to load:
    // 1. KPI stat cards: active members, document count, conversation turns, storage consumption.
    // 2. Chart.js Donut chart: Departmental document coverage distribution across organizational units.
    // 3. Chart.js Line chart: 30-day temporal conversational query volume.
    // Backend leverages Redis distributed caching (`dashboard:{tenantId}`) with automatic cache eviction
    // whenever documents or memberships are added or removed.
    this.dashboardService.getSummary().subscribe({
      next: (res) => {
        if (res?.data) {
          this.summary.set(res.data);
          this.initDocumentChart(res.data.documentCoverage);
          this.initActivityChart(res.data.chatActivity);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  /**
   * Initializes the document coverage donut chart using active theme CSS variables.
   */
  initDocumentChart(coverage: DashboardSummaryDto['documentCoverage']) {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--p-text-color') || '#334155';
    const items = coverage && coverage.length > 0
      ? coverage
      : [{ label: 'No Documents', value: 1, color: '#e2e8f0', hoverColor: '#cbd5e1' }];

    this.documentChartData = {
      labels: items.map(c => c.label),
      datasets: [
        {
          data: items.map(c => c.value),
          backgroundColor: items.map(c => c.color),
          hoverBackgroundColor: items.map(c => c.hoverColor),
          borderWidth: 0
        }
      ]
    };

    this.documentChartOptions = {
      cutout: '75%',
      plugins: {
        legend: {
          position: 'right',
          labels: {
            color: textColor,
            usePointStyle: true,
            boxWidth: 8,
            boxHeight: 8,
            padding: 20,
            font: {
              size: 12,
              family: 'Inter, sans-serif'
            }
          }
        }
      },
      maintainAspectRatio: false
    };
  }

  initActivityChart(activity: DashboardSummaryDto['chatActivity']) {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColorSecondary = documentStyle.getPropertyValue('--p-text-muted-color') || '#94a3b8';
    const surfaceBorder = documentStyle.getPropertyValue('--p-content-border-color') || '#e2e8f0';

    const labels = activity?.labels ?? [];
    const data = activity?.data ?? [];
    const maxVal = data.length > 0 ? Math.max(...data, 10) : 10;

    this.activityChartData = {
      labels: labels,
      datasets: [
        {
          label: 'Conversations',
          data: data,
          fill: true,
          borderColor: '#6366f1',
          tension: 0.4,
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          pointBackgroundColor: '#ffffff',
          pointBorderColor: '#6366f1',
          pointBorderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6
        }
      ]
    };

    this.activityChartOptions = {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false
        }
      },
      scales: {
        x: {
          ticks: {
            color: textColorSecondary,
            font: {
              size: 11,
              family: 'Inter, sans-serif'
            }
          },
          grid: {
            color: 'transparent'
          },
          border: {
            display: false
          }
        },
        y: {
          ticks: {
            color: textColorSecondary,
            stepSize: 100,
            font: {
              size: 11,
              family: 'Inter, sans-serif'
            }
          },
          grid: {
            color: surfaceBorder
          },
          border: {
            display: false
          },
          min: 0,
          max: Math.ceil(maxVal * 1.2)
        }
      }
    };
  }

  onSearchWorkspace(): void {
    const q = this.searchWorkspaceQuery().trim();
    if (q) {
      this.router.navigate(['../documents'], { relativeTo: this.route, queryParams: { search: q } });
    }
  }

  goToAuditLogs(): void {
    this.router.navigate(['../settings/audit-logs'], { relativeTo: this.route });
  }

  reviewKnowledgeBase(): void {
    const desc = this.summary()?.kbReview?.description?.toLowerCase() || '';
    if (desc.includes('attention') || desc.includes('error')) {
      this.router.navigate(['../documents'], { relativeTo: this.route, queryParams: { status: 'Failed' } });
    } else if (desc.includes('processed') || desc.includes('processing')) {
      this.router.navigate(['../documents'], { relativeTo: this.route, queryParams: { status: 'Processing' } });
    } else {
      this.copilotService.open();
    }
  }

  getDocumentTypeClass(type: string): string {
    const t = (type || '').toLowerCase();
    if (t.includes('pdf')) return 'bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/20';
    if (t.includes('doc') || t.includes('docx') || t.includes('word')) return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border border-blue-500/20';
    if (t.includes('xls') || t.includes('csv') || t.includes('sheet')) return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20';
    return 'bg-purple-500/10 text-primary-600 dark:text-primary-400 border border-primary-500/20';
  }

  showRollingFilterNotice() {
    this.messageService.add({
      severity: 'info',
      summary: 'Trailing Rolling Activity',
      detail: 'Conversational metrics are automatically aggregated over the trailing 7-day rolling window.'
    });
  }
}
