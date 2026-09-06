import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface Department {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string;
}

export interface UpdateDepartmentRequest {
  name: string;
  description?: string;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/departments`;

  getAll(): Observable<Department[]> {
    return this.http
      .get<ApiResponse<Department[]>>(this.apiUrl)
      .pipe(map(r => r.data));
  }

  create(req: CreateDepartmentRequest): Observable<Department> {
    return this.http
      .post<ApiResponse<Department>>(this.apiUrl, req)
      .pipe(map(r => r.data));
  }

  update(id: string, req: UpdateDepartmentRequest): Observable<Department> {
    return this.http
      .put<ApiResponse<Department>>(`${this.apiUrl}/${id}`, req)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }
}
