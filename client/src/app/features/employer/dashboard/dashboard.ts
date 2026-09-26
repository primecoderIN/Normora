import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';

import { InputText } from 'primeng/inputtext';
import { ChartModule } from 'primeng/chart';
import { ButtonModule } from 'primeng/button';
import { StatCardComponent } from '@shared/components/stat-card/stat-card.component';
import { UserService } from '@core/services/user.service';
import { DashboardService, DashboardSummaryDto } from '@core/services/dashboard.service';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, InputText, ChartModule, StatCardComponent, ButtonModule, SkeletonModule],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  public userService = inject(UserService);
  private dashboardService = inject(DashboardService);
  
  summary = signal<DashboardSummaryDto | null>(null);
  isLoading = signal(true);

  documentChartData: any;
  documentChartOptions: any;

  activityChartData: any;
  activityChartOptions: any;

  ngOnInit() {
    this.dashboardService.getSummary().subscribe({
      next: (res) => {
        this.summary.set(res.data);
        this.initDocumentChart(res.data.documentCoverage);
        this.initActivityChart(res.data.chatActivity);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  initDocumentChart(coverage: DashboardSummaryDto['documentCoverage']) {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--p-text-color') || '#334155';

    this.documentChartData = {
      labels: coverage.map(c => c.label),
      datasets: [
        {
          data: coverage.map(c => c.value),
          backgroundColor: coverage.map(c => c.color),
          hoverBackgroundColor: coverage.map(c => c.hoverColor),
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
    const textColor = documentStyle.getPropertyValue('--p-text-color') || '#64748b';
    const textColorSecondary = documentStyle.getPropertyValue('--p-text-muted-color') || '#94a3b8';
    const surfaceBorder = documentStyle.getPropertyValue('--p-content-border-color') || '#e2e8f0';

    this.activityChartData = {
      labels: activity.labels,
      datasets: [
        {
          label: 'Conversations',
          data: activity.data,
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
            color: 'transparent',
            drawBorder: false
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
            color: surfaceBorder,
            drawBorder: false,
            borderDash: [4, 4]
          },
          min: 0,
          max: 400
        }
      }
    };
  }
}
