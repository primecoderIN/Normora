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
  autoRenewEnabled?: boolean;
  hasMandate?: boolean;
}

export interface CheckoutSessionResult {
  actionUrl: string;
  params: Record<string, string>;
}

export interface TenantInvoiceDto {
  id: string;
  invoiceNumber: string;
  date: string;
  tier: string;
  amount: string;
  currency: string;
  cycle: string;
  status: 'Paid' | 'Pending' | 'Failed';
  transactionId?: string;
}

export interface TenantBillingDetailsDto {
  billingEmail: string;
  companyLegalName: string;
  taxId: string;
  billingAddress: string;
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
   * Updates workspace default billing currency preference.
   */
  updateCurrency(currency: string): Observable<SubscriptionUsageDto> {
    return this.http
      .put<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/currency`, {
        currency: currency.toLowerCase()
      })
      .pipe(map(res => res.data));
  }

  /**
   * Initiates a PayU hosted checkout session for upgrading to a paid tier in specified currency and billing cycle.
   * Returns action URL and parameters signed with SHA-512 cryptographic hash.
   */
  createCheckoutSession(
    tier: PlanTierType,
    currency: string = 'inr',
    billingCycle: string = 'monthly',
    enableAutoRenew: boolean = true
  ): Observable<CheckoutSessionResult> {
    const tierEnumMap: Record<PlanTierType, number> = {
      Free: 0,
      Pro: 1,
      Enterprise: 2
    };

    return this.http
      .post<ApiResponse<CheckoutSessionResult>>(`${this.apiUrl}/checkout`, {
        tier: tierEnumMap[tier],
        currency: currency.toLowerCase(),
        billingCycle: billingCycle.toLowerCase(),
        enableAutoRenew
      })
      .pipe(map(res => res.data));
  }

  /**
   * Toggles automated recurring renewal (Standing Instructions / UPI AutoPay) for the workspace.
   */
  toggleAutoRenew(enabled: boolean): Observable<SubscriptionUsageDto> {
    return this.http
      .post<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/auto-renew`, { enabled })
      .pipe(map(res => res.data));
  }

  /**
   * Automatically creates and submits a hidden HTML form to redirect to the hosted checkout gateway.
   */
  submitPaymentForm(session: CheckoutSessionResult): void {
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
   * Backward-compatibility alias for submitPaymentForm.
   */
  submitPayUForm(session: CheckoutSessionResult): void {
    this.submitPaymentForm(session);
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

  /**
   * Cancels the active subscription, scheduling plan reversion to Free tier at period end.
   */
  cancelSubscription(immediate: boolean = false): Observable<SubscriptionUsageDto> {
    return this.http
      .post<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/cancel`, { immediate })
      .pipe(map(res => res.data));
  }

  /**
   * Resumes or reactivates a subscription previously scheduled for cancellation.
   */
  reactivateSubscription(): Observable<SubscriptionUsageDto> {
    return this.http
      .post<ApiResponse<SubscriptionUsageDto>>(`${this.apiUrl}/reactivate`, {})
      .pipe(map(res => res.data));
  }

  /**
   * Retrieves official itemized billing invoices for the current tenant.
   */
  getInvoices(): Observable<TenantInvoiceDto[]> {
    return this.http
      .get<ApiResponse<TenantInvoiceDto[]>>(`${this.apiUrl}/invoices`)
      .pipe(map(res => res.data));
  }

  /**
   * Downloads an official tax invoice document (HTML or PDF) as a binary Blob.
   */
  downloadInvoiceFile(invoiceId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/invoices/${invoiceId}/download`, {
      responseType: 'blob'
    });
  }

  /**
   * Retrieves workspace billing contact, company legal name, and tax ID (GSTIN).
   */
  getBillingDetails(): Observable<TenantBillingDetailsDto> {
    return this.http
      .get<ApiResponse<TenantBillingDetailsDto>>(`${this.apiUrl}/billing-details`)
      .pipe(map(res => res.data));
  }

  /**
   * Updates workspace billing contact and tax details.
   */
  updateBillingDetails(details: TenantBillingDetailsDto): Observable<TenantBillingDetailsDto> {
    return this.http
      .put<ApiResponse<TenantBillingDetailsDto>>(`${this.apiUrl}/billing-details`, details)
      .pipe(map(res => res.data));
  }
}
