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
  currency?: string;
}

export interface CheckoutSessionResult {
  actionUrl: string;
  params: Record<string, string>;
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
   */
  getUsage(): Observable<SubscriptionUsageDto> {
    return this.http
      .get<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/usage`)
      .pipe(map(res => res.data));
  }

  /**
   * Initiates a PayU hosted checkout session for upgrading to a paid tier in specified currency.
   * Returns action URL and parameters signed with SHA-512 cryptographic hash.
   */
  createCheckoutSession(tier: PlanTierType, currency: string = 'inr'): Observable<CheckoutSessionResult> {
    const tierEnumMap: Record<PlanTierType, number> = {
      Free: 0,
      Pro: 1,
      Enterprise: 2
    };

    return this.http
      .post<ApiResponse<CheckoutSessionResult>>(`${this.apiUrl}/checkout`, {
        tier: tierEnumMap[tier],
        currency: currency.toLowerCase()
      })
      .pipe(map(res => res.data));
  }

  /**
   * Automatically creates and submits a hidden HTML form to redirect to PayU hosted checkout.
   */
  submitPayUForm(session: CheckoutSessionResult): void {
    if (!session?.actionUrl) return;

    const form = document.createElement('form');
    form.method = 'POST';
    form.action = session.actionUrl;
    form.style.display = 'none';

    if (session.params) {
      for (const [key, value] of Object.entries(session.params)) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = key;
        input.value = value;
        form.appendChild(input);
      }
    }

    document.body.appendChild(form);
    form.submit();
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
