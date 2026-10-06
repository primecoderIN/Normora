import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

export interface CreateTenantPayload {
  name: string;
  slug: string;
}

export interface TenantEmployeeDto {
  membershipId: string;
  userId: string;
  email: string;
  displayName: string;
  role: string;
  joinedAt: string;
  departments: any[];
}

@Injectable({
  providedIn: 'root',
})
export class TenantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/tenants`;

  // Submit a new organization creation request to the API, establishing the user as its first Admin
  createTenant(payload: CreateTenantPayload): Observable<any> {
    return this.http.post<any>(this.apiUrl, payload);
  }

  // Retrieve a list of employees for the current tenant (paginated)
  getEmployees(page: number = 1, pageSize: number = 10, search?: string, departmentId?: string, userGroupId?: string): Observable<any> {
    let url = `${this.apiUrl}/employees?page=${page}&pageSize=${pageSize}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    if (departmentId) url += `&departmentId=${departmentId}`;
    if (userGroupId) url += `&userGroupId=${userGroupId}`;
    
    return this.http.get<any>(url);
  }

  removeEmployee(membershipId: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/employees/${membershipId}`);
  }

  getEmployeeStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/employees/stats`);
  }
}

