import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { DialogModule } from 'primeng/dialog';
import { SkeletonModule } from 'primeng/skeleton';
import { AuditLogService, AuditLogDto } from '@core/services/audit-log.service';

interface ActionOption {
  label: string;
  value: string | null;
}

interface DateFilterOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    ButtonModule,
    SelectModule,
    PaginatorModule,
    DialogModule,
    SkeletonModule
  ],
  templateUrl: './audit-logs.component.html'
})
export class AuditLogsComponent implements OnInit {
  private auditLogService = inject(AuditLogService);

  // Data & State
  logs = signal<AuditLogDto[]>([]);
  isLoading = signal(true);
  errorMessage = signal<string | null>(null);

  // Pagination & Filtering
  currentPage = signal(1);
  pageSize = signal(20);
  totalRecords = signal(0);
  searchQuery = signal('');
  selectedAction = signal<string | null>(null);
  selectedDateRange = signal<string>('all');

  // Inspection Drawer
  selectedLog = signal<AuditLogDto | null>(null);
  isDetailDialogOpen = signal(false);

  // Filter Dropdown Options
  actionOptions: ActionOption[] = [
    { label: 'All Actions', value: null },
    { label: 'Document Uploaded', value: 'Document.Uploaded' },
    { label: 'Document Version Created', value: 'Document.VersionCreated' },
    { label: 'Document Deleted', value: 'Document.Deleted' },
    { label: 'Invitation Sent', value: 'Invitation.Sent' },
    { label: 'Invitation Accepted', value: 'Invitation.Accepted' },
    { label: 'Employee Removed', value: 'Employee.Removed' },
    { label: 'Department Created', value: 'Department.Created' },
    { label: 'Department Updated', value: 'Department.Updated' },
    { label: 'Department Deleted', value: 'Department.Deleted' },
    { label: 'User Group Created', value: 'UserGroup.Created' },
    { label: 'User Group Updated', value: 'UserGroup.Updated' },
    { label: 'User Group Deleted', value: 'UserGroup.Deleted' },
    { label: 'Branding Updated', value: 'Branding.Updated' },
    { label: 'Tenant Suspended', value: 'Tenant.Suspended' }
  ];

  dateRangeOptions: DateFilterOption[] = [
    { label: 'All Time', value: 'all' },
    { label: 'Past 24 Hours', value: '24h' },
    { label: 'Past 7 Days', value: '7d' },
    { label: 'Past 30 Days', value: '30d' }
  ];

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    let from: string | undefined;
    const now = new Date();

    if (this.selectedDateRange() === '24h') {
      const past24h = new Date(now.getTime() - 24 * 60 * 60 * 1000);
      from = past24h.toISOString();
    } else if (this.selectedDateRange() === '7d') {
      const past7d = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
      from = past7d.toISOString();
    } else if (this.selectedDateRange() === '30d') {
      const past30d = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
      from = past30d.toISOString();
    }

    this.auditLogService.getAuditLogs({
      page: this.currentPage(),
      pageSize: this.pageSize(),
      action: this.selectedAction() ?? undefined,
      search: this.searchQuery().trim() || undefined,
      from
    }).subscribe({
      next: (result) => {
        this.logs.set(result.items);
        this.totalRecords.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load audit logs. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  onSearch(): void {
    this.currentPage.set(1);
    this.loadLogs();
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadLogs();
  }

  onPageChange(event: PaginatorState): void {
    this.currentPage.set((event.page ?? 0) + 1);
    this.pageSize.set(event.rows ?? 20);
    this.loadLogs();
  }

  resetFilters(): void {
    this.searchQuery.set('');
    this.selectedAction.set(null);
    this.selectedDateRange.set('all');
    this.currentPage.set(1);
    this.loadLogs();
  }

  viewDetails(log: AuditLogDto): void {
    this.selectedLog.set(log);
    this.isDetailDialogOpen.set(true);
  }

  closeDetails(): void {
    this.isDetailDialogOpen.set(false);
    this.selectedLog.set(null);
  }

  formatMetadata(metadata: string | null): string {
    if (!metadata) return 'No additional metadata';
    try {
      const parsed = JSON.parse(metadata);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return metadata;
    }
  }

  getActionBadgeClass(action: string): string {
    if (action.includes('Deleted') || action.includes('Removed') || action.includes('Suspended')) {
      return 'bg-rose-500/10 text-rose-600 dark:text-rose-400 border-rose-500/20';
    }
    if (action.includes('Uploaded') || action.includes('Created') || action.includes('Accepted')) {
      return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/20';
    }
    if (action.includes('Updated') || action.includes('VersionCreated')) {
      return 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20';
    }
    return 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/20';
  }

  getActionIcon(action: string): string {
    if (action.startsWith('Document.')) return 'pi pi-file';
    if (action.startsWith('Employee.') || action.startsWith('Invitation.')) return 'pi pi-user';
    if (action.startsWith('Department.')) return 'pi pi-building';
    if (action.startsWith('UserGroup.')) return 'pi pi-users';
    if (action.startsWith('Branding.')) return 'pi pi-palette';
    return 'pi pi-shield';
  }
}
