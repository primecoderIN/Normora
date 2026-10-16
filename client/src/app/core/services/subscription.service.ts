import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@env/environment';
import { ApiResponse } from '../models/api-response.model';

export type PlanTierType = 'Free' | 'Pro' | 'Enterprise';

export interface SubscriptionUsageDto {
  planTier: PlanTierType;
  maxDocuments: number;
  currentDocuments: number;
  maxEmployees: number;
  currentEmployees: number;
  maxStorageBytes: number;
  currentStorageBytes: number;
  validUntil: string | null;
  isDocumentLimitReached: boolean;
  isEmployeeLimitReached: boolean;
  isStorageLimitReached: boolean;
  hasActiveSubscription: boolean;
  subscriptionStatus: string | null;
  currentPeriodEndUtc: string | null;
  cancelAtPeriodEnd: boolean;
}

export interface CheckoutSessionResult {
  checkoutUrl: string;
}

export interface CustomerPortalResult {
  portalUrl: string;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/subscription`;

  getUsage(): Observable<SubscriptionUsageDto> {
    return this.http
      .get<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/usage`)
      .pipe(map(res => res.data));
  }

  createCheckoutSession(tier: PlanTierType): Observable<CheckoutSessionResult> {
    const tierEnumMap: Record<PlanTierType, number> = {
      Free: 0,
      Pro: 1,
      Enterprise: 2
    };

    return this.http
      .post<ApiResponse<CheckoutSessionResult>>(`${this.apiUrl}/checkout`, { tier: tierEnumMap[tier] })
      .pipe(map(res => res.data));
  }

  createCustomerPortalSession(returnUrl?: string): Observable<CustomerPortalResult> {
    return this.http
      .post<ApiResponse<CustomerPortalResult>>(`${this.apiUrl}/portal`, { returnUrl })
      .pipe(map(res => res.data));
  }

  updateTier(tier: PlanTierType): Observable<SubscriptionUsageDto> {
    const tierEnumMap: Record<PlanTierType, number> = {
      Free: 0,
      Pro: 1,
      Enterprise: 2
    };

    return this.http
      .post<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/tier`, { tier: tierEnumMap[tier] })
      .pipe(map(res => res.data));
  }
}
