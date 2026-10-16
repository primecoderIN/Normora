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

  /**
   * Retrieves current workspace usage metrics and quota thresholds.
   * If sessionId is provided (upon redirect return from Stripe Checkout),
   * the backend performs synchronous verification and instant quota provisioning.
   */
  getUsage(sessionId?: string): Observable<SubscriptionUsageDto> {
    const params = sessionId ? { sessionId } : undefined;
    return this.http
      .get<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/usage`, { params })
      .pipe(map(res => res.data));
  }

  /**
   * Initiates a Stripe-hosted checkout session for upgrading to a paid tier.
   * Returns the hosted checkout URL to which the browser will redirect.
   */
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

  /**
   * Opens the Stripe Billing Customer Portal session for managing payment methods,
   * viewing invoices, or scheduling plan cancellations without orphan subscriptions.
   */
  createCustomerPortalSession(returnUrl?: string): Observable<CustomerPortalResult> {
    return this.http
      .post<ApiResponse<CustomerPortalResult>>(`${this.apiUrl}/portal`, { returnUrl })
      .pipe(map(res => res.data));
  }

  /**
   * Updates workspace plan tier directly (used for manual or free tier management).
   */
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
