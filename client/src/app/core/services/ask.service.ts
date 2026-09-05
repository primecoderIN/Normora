import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { UserService } from './user.service';

export interface AskCitation {
  documentId: string;
  fileName: string;
  chunkIndex: number;
  similarity: number;
}

export interface AskResult {
  answer: string;
  sources: AskCitation[];
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class AskService {
  private http = inject(HttpClient);
  private userService = inject(UserService);
  private apiUrl = `${environment.apiUrl}/api/ask`;

  ask(question: string): Observable<AskResult> {
    // A direct refresh can render Ask before the root auth flow has populated the
    // current user. Load the profile first so the tenant interceptor can attach
    // the validated membership ID to the protected request.
    const profile = this.userService.currentUser()
      ? of({ success: true, message: 'Loaded', data: this.userService.currentUser() })
      : this.userService.getMe();

    return profile.pipe(
      switchMap(response => response.success && response.data
        ? this.http.post<ApiResponse<AskResult>>(this.apiUrl, { question, limit: 5 })
        : throwError(() => new Error('Your tenant profile could not be loaded.'))),
      map(response => response.data)
    );
  }
}