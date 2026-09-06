import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

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
}

export interface UpdateUserGroupRequest {
  name: string;
  description?: string;
}

export interface UpdateUserGroupAssignmentsRequest {
  memberUserIds: string[];
  departmentIds: string[];
}

@Injectable({ providedIn: 'root' })
export class UserGroupService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/user-groups`;

  getAll(): Observable<UserGroup[]> {
    return this.http
      .get<ApiResponse<UserGroup[]>>(this.apiUrl)
      .pipe(map(r => r.data));
  }

  getById(id: string): Observable<UserGroupDetail> {
    return this.http
      .get<ApiResponse<UserGroupDetail>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  create(req: CreateUserGroupRequest): Observable<UserGroup> {
    return this.http
      .post<ApiResponse<UserGroup>>(this.apiUrl, req)
      .pipe(map(r => r.data));
  }

  update(id: string, req: UpdateUserGroupRequest): Observable<UserGroup> {
    return this.http
      .put<ApiResponse<UserGroup>>(`${this.apiUrl}/${id}`, req)
      .pipe(map(r => r.data));
  }

  updateAssignments(id: string, req: UpdateUserGroupAssignmentsRequest): Observable<void> {
    return this.http
      .put<ApiResponse<void>>(`${this.apiUrl}/${id}/assignments`, req)
      .pipe(map(() => void 0));
  }

  delete(id: string): Observable<void> {
    return this.http
      .delete<ApiResponse<void>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }
}
