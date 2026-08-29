import { Component } from '@angular/core';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CardModule, TableModule, TagModule, DatePipe],
  templateUrl: './dashboard.html',
})
export class Dashboard {
  metrics = [
    { title: 'Total Documents', value: 124, icon: 'pi-file', color: 'text-blue-500' },
    { title: 'Active Employees', value: 45, icon: 'pi-users', color: 'text-emerald-500' },
    { title: 'Questions Asked', value: 892, icon: 'pi-comments', color: 'text-purple-500' }
  ];

  recentDocuments = [
    { id: '1', name: 'Employee_Handbook_2024.pdf', status: 'PROCESSED', date: new Date(Date.now() - 86400000) },
    { id: '2', name: 'Q3_Financial_Report.xlsx', status: 'PROCESSING', date: new Date(Date.now() - 3600000) },
    { id: '3', name: 'Health_Benefits_Summary.pdf', status: 'PROCESSED', date: new Date(Date.now() - 172800000) },
    { id: '4', name: 'Compliance_Guidelines_v2.docx', status: 'ERROR', date: new Date(Date.now() - 259200000) }
  ];

  getSeverity(status: string) {
    switch (status) {
      case 'PROCESSED': return 'success';
      case 'PROCESSING': return 'warn';
      case 'ERROR': return 'danger';
      default: return 'info';
    }
  }
}
