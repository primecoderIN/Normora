import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/api-response.model';

/**
 * Summary representation of a user group including member and department counts.
 */
export interface UserGroup {
  id: string;
  name: string;
  description?: string;
  memberCount: number;
  departmentCount: number;
  createdAt: string;
}

export interface UserGroupDetail {
  id: string;
  name: string;
  description?: string;
  memberUserIds: string[];
  departmentIds: string[];
  createdAt: string;
}

export interface CreateUserGroupRequest {
  name: string;
  description?: string;
  departmentIds?: string[];
}

export interface UpdateUserGroupRequest {
  name: string;
  description?: string;
  departmentIds?: string[];
}

export interface UpdateUserGroupAssignmentsRequest {
  memberUserIds: string[];
  departmentIds: string[];
}

/**
 * Manages user groups, member assignments, and department mappings for RBAC.
 */
@Injectable({ providedIn: 'root' })
export class UserGroupService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/user-groups`;

  /**
   * Fetches all user groups for the current tenant.
   */
  getAll(): Observable<UserGroup[]> {
    return this.http
      .get<ApiResponse<UserGroup[]>>(this.apiUrl)
      .pipe(map(r => r.data));
  }

  /**
   * Fetches detailed information including user and department membership IDs.
   */
  getById(id: string): Observable<UserGroupDetail> {
    return this.http
      .get<ApiResponse<UserGroupDetail>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  /**
   * Creates a new user group.
   */
  create(req: CreateUserGroupRequest): Observable<string> {
    return this.http
      .post<ApiResponse<string>>(this.apiUrl, req)
      .pipe(map(r => r.data));
  }

  /**
   * Updates user group metadata.
   */
  update(id: string, req: UpdateUserGroupRequest): Observable<void> {
    return this.http
      .put<ApiResponse<void>>(`${this.apiUrl}/${id}`, req)
      .pipe(map(() => void 0));
  }

  /**
   * Updates members and department assignments for a user group.
   */
  updateAssignments(id: string, req: UpdateUserGroupAssignmentsRequest): Observable<void> {
    return this.http
      .put<ApiResponse<void>>(`${this.apiUrl}/${id}/assignments`, req)
      .pipe(map(() => void 0));
  }

  /**
   * Deletes a user group by ID.
   */
  delete(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }
}

