import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/api-response.model';

/**
 * Organizational department entity used for document access control and user categorization.
 */
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

/**
 * Manages tenant departments (CRUD) for document isolation and team organizational boundaries.
 */
@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/departments`;

  /**
   * Fetches all departments for the active tenant.
   */
  getAll(): Observable<Department[]> {
    return this.http
      .get<ApiResponse<Department[]>>(this.apiUrl)
      .pipe(map(r => r.data));
  }

  /**
   * Creates a new organizational department.
   */
  create(req: CreateDepartmentRequest): Observable<string> {
    return this.http
      .post<ApiResponse<string>>(this.apiUrl, req)
      .pipe(map(r => r.data));
  }

  /**
   * Updates an existing department's name and description.
   */
  update(id: string, req: UpdateDepartmentRequest): Observable<void> {
    return this.http
      .put<ApiResponse<void>>(`${this.apiUrl}/${id}`, req)
      .pipe(map(() => void 0));
  }

  /**
   * Deletes a department by ID.
   */
  delete(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }
}

