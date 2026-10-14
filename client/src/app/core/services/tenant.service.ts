import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/api-response.model';
import { PagedResult } from '../models/paged-result.model';

export interface CreateTenantPayload {
  name: string;
  slug: string;
}

export interface TenantDto {
  id: string;
  name: string;
  slug: string;
  status: number;
  createdAt: string;
}

export interface DepartmentSummaryDto {
  id: string;
  name: string;
}

export interface UserGroupSummaryDto {
  id: string;
  name: string;
}

export interface TenantEmployeeDto {
  membershipId: string;
  userId: string;
  email: string;
  displayName: string;
  role: string;
  joinedAt: string;
  departments: DepartmentSummaryDto[];
  userGroups: UserGroupSummaryDto[];
}

export interface TenantStatsDto {
  pendingInvites: number;
  adminSeats: number;
}

@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/tenants`;

  // Submit a new organization creation request to the API, establishing the user as its first Admin
  createTenant(payload: CreateTenantPayload): Observable<ApiResponse<TenantDto>> {
    return this.http.post<ApiResponse<TenantDto>>(this.apiUrl, payload);
  }

  // Retrieve a list of employees for the current tenant (paginated)
  getEmployees(
    page: number = 1,
    pageSize: number = 10,
    search?: string,
    departmentId?: string,
    userGroupId?: string
  ): Observable<ApiResponse<PagedResult<TenantEmployeeDto>>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    if (departmentId) {
      params = params.set('departmentId', departmentId);
    }
    if (userGroupId) {
      params = params.set('userGroupId', userGroupId);
    }

    return this.http.get<ApiResponse<PagedResult<TenantEmployeeDto>>>(`${this.apiUrl}/employees`, { params });
  }

  removeEmployee(membershipId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/employees/${membershipId}`);
  }

  getEmployeeStats(): Observable<ApiResponse<TenantStatsDto>> {
    return this.http.get<ApiResponse<TenantStatsDto>>(`${this.apiUrl}/employees/stats`);
  }
}
