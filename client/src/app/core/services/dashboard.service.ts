import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

export interface DashboardSummaryDto {
  stats: {
    totalDocuments: number;
    documentsTrend: number;
    totalEmployees: number;
    employeesTrend: number;
    totalQuestions: number;
    questionsTrend: number;
    answerQuality: number;
    answerQualityTrend: number;
  };
  documentCoverage: { label: string; value: number; color: string; hoverColor: string; }[];
  recentDocuments: { name: string; category: string; status: string; type: string; date: string; }[];
  topQuestions: { question: string; count: number; }[];
  chatActivity: { labels: string[]; data: number[]; };
  kbReview: { title: string; description: string; };
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/dashboard`;

  getSummary(): Observable<{ data: DashboardSummaryDto, success: boolean, message: string }> {
    return this.http.get<{ data: DashboardSummaryDto, success: boolean, message: string }>(this.baseUrl);
  }
}
