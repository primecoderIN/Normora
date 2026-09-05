import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';

import { InputText } from 'primeng/inputtext';
import { ChartModule } from 'primeng/chart';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, InputText, ChartModule, StatCardComponent],
  styleUrl: './dashboard.css',
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  public userService = inject(UserService);
  documentChartData: any;
  documentChartOptions: any;

  activityChartData: any;
  activityChartOptions: any;

  recentDocuments = signal([
    { name: 'Travel Policy v2.1', category: 'HR Policies', status: 'Published', type: 'PDF', date: new Date(2025, 4, 20) },
    { name: 'Employee Handbook', category: 'HR Policies', status: 'Published', type: 'DOC', date: new Date(2025, 4, 18) },
    { name: 'Code of Conduct', category: 'Compliance', status: 'Published', type: 'PDF', date: new Date(2025, 4, 15) },
    { name: 'Leave Policy v1.2', category: 'HR Policies', status: 'Published', type: 'XLS', date: new Date(2025, 4, 12) },
    { name: 'IT Security Guidelines', category: 'IT Policies', status: 'Draft', type: 'PPT', date: new Date(2025, 4, 10) },
  ]);

  topQuestions = signal([
    { question: 'What is the policy for travel reimbursement?', count: 128 },
    { question: 'How many leaves can I take in a year?', count: 98 },
    { question: 'What is the process for expense approval?', count: 76 },
    { question: 'How to claim medical reimbursement?', count: 64 },
    { question: 'What is the work from home policy?', count: 52 },
  ]);

  ngOnInit() {
    this.initDocumentChart();
    this.initActivityChart();
  }

  initDocumentChart() {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--p-text-color') || '#334155';

    this.documentChartData = {
      labels: ['HR Policies', 'IT Policies', 'Finance', 'Compliance', 'Others'],
      datasets: [
        {
          data: [45, 32, 24, 17, 10],
          backgroundColor: ['#6366f1', '#3b82f6', '#10b981', '#f59e0b', '#94a3b8'],
          hoverBackgroundColor: ['#4f46e5', '#2563eb', '#059669', '#d97706', '#64748b'],
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

  initActivityChart() {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--p-text-color') || '#64748b';
    const textColorSecondary = documentStyle.getPropertyValue('--p-text-muted-color') || '#94a3b8';
    const surfaceBorder = documentStyle.getPropertyValue('--p-content-border-color') || '#e2e8f0';

    this.activityChartData = {
      labels: ['May 15', 'May 16', 'May 17', 'May 18', 'May 19', 'May 20', 'May 21'],
      datasets: [
        {
          label: 'Conversations',
          data: [120, 210, 180, 290, 240, 310, 380],
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
