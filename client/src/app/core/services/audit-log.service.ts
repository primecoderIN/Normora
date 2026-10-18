import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/api-response.model';

/**
 * Historical security/audit record payload.
 */
export interface AuditLogDto {
  id: string;
  actorUserId: string | null;
  actorEmail: string | null;
  actorName: string | null;
  action: string;
  targetType: string | null;
  targetId: string | null;
  metadata: string | null;
  occurredAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditLogFilterParams {
  page?: number;
  pageSize?: number;
  action?: string;
  search?: string;
  from?: string;
  to?: string;
}

/**
 * Handles fetching tenant security audit logs with pagination and search filtering.
 */
@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/audit-logs`;

  /**
   * Fetches paginated audit log entries matching provided filters.
   */
  getAuditLogs(filters: AuditLogFilterParams = {}): Observable<PagedResult<AuditLogDto>> {
    // Construct query parameters
    let params = new HttpParams()
      .set('page', (filters.page ?? 1).toString())
      .set('pageSize', (filters.pageSize ?? 20).toString());

    if (filters.action) {
      params = params.set('action', filters.action);
    }
    if (filters.search) {
      params = params.set('search', filters.search);
    }
    if (filters.from) {
      params = params.set('from', filters.from);
    }
    if (filters.to) {
      params = params.set('to', filters.to);
    }

    return this.http
      .get<ApiResponse<PagedResult<AuditLogDto>>>(this.apiUrl, { params })
      .pipe(map(res => res.data));
  }
}

