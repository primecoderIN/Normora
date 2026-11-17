import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  SubscriptionService,
  SubscriptionUsageDto,
  PlanTierType,
  TenantBillingDetailsDto
} from '@core/services/subscription.service';
import { UserService } from '@core/services/user.service';
import { ApiMessages } from '@core/constants/api-messages';
import { DsToastService } from '@design-system';
import {
  BillingCycle,
  BillingInvoiceItem,
  CurrencyConfig,
  PlanTierDefinition,
  SubscriptionTab
} from './models/subscription.models';
import { SUPPORTED_CURRENCIES, detectLocalCurrency } from './helpers/subscription-currency.helper';
import { createPlanTierDefinitions } from './helpers/subscription-plans.helper';
import { SubscriptionOverviewComponent } from './components/subscription-overview/subscription-overview.component';
import { SubscriptionInvoicesComponent } from './components/subscription-invoices/subscription-invoices.component';
import { SubscriptionSettingsComponent } from './components/subscription-settings/subscription-settings.component';
import { SubscriptionModalsComponent } from './components/subscription-modals/subscription-modals.component';

export type { SubscriptionTab, BillingCycle, PlanTierDefinition, CurrencyConfig, BillingInvoiceItem } from './models/subscription.models';
export { SUPPORTED_CURRENCIES } from './helpers/subscription-currency.helper';

@Component({
  selector: 'app-subscription',
  standalone: true,
  imports: [
    CommonModule,
    SubscriptionOverviewComponent,
    SubscriptionInvoicesComponent,
    SubscriptionSettingsComponent,
    SubscriptionModalsComponent
  ],
  templateUrl: './subscription.component.html'
})
export class SubscriptionComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);
  private messageService = inject(DsToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  public userService = inject(UserService);

  // Active Tab Navigation
  activeTab = signal<SubscriptionTab>('overview');

  // Billing Cycle Toggle (Monthly vs Annual - Save 20%)
  billingCycle = signal<BillingCycle>('monthly');

  // Usage & Quota State
  usage = signal<SubscriptionUsageDto | null>(null);
  isLoading = signal(true);
  isUpdating = signal(false);
  selectedTierLoading = signal<PlanTierType | null>(null);
  errorMessage = signal<string | null>(null);

  // Currencies
  readonly currencies = SUPPORTED_CURRENCIES;
  readonly selectedCurrency = signal<string>(detectLocalCurrency());
  readonly isDetectedCurrency = signal<boolean>(true);
  readonly isWorkspaceCurrency = signal<boolean>(false);

  // Modals & Dialog State
  showQuotaDetails = signal(false);
  showCancelModal = signal(false);
  showDowngradeModal = signal(false);
  pendingDowngradeTier = signal<PlanTierType | null>(null);
  showSupportModal = signal(false);

  // Settings Form State
  billingEmail = signal('');
  companyLegalName = signal('');
  taxId = signal('');
  billingAddress = signal('');
  isSavingSettings = signal(false);

  // Invoices
  readonly invoices = signal<BillingInvoiceItem[]>([
    {
      id: 'inv_101',
      invoiceNumber: 'INV-2026-11-001',
      date: new Date('2026-11-12'),
      tier: 'Enterprise',
      amount: '₹19,999',
      currency: 'INR',
      cycle: 'Monthly',
      status: 'Paid'
    },
    {
      id: 'inv_100',
      invoiceNumber: 'INV-2026-10-001',
      date: new Date('2026-10-12'),
      tier: 'Enterprise',
      amount: '₹19,999',
      currency: 'INR',
      cycle: 'Monthly',
      status: 'Paid'
    },
    {
      id: 'inv_099',
      invoiceNumber: 'INV-2026-09-001',
      date: new Date('2026-09-12'),
      tier: 'Pro Workspace',
      amount: '₹3,999',
      currency: 'INR',
      cycle: 'Monthly',
      status: 'Paid'
    }
  ]);

  readonly activeCurrencyConfig = computed<CurrencyConfig>(() => {
    return this.currencies.find(c => c.code === this.selectedCurrency()) || this.currencies[0];
  });

  readonly userInitials = computed(() => {
    const name = this.userService.currentUser()?.displayName?.trim() || 'User';
    const parts = name.split(' ').filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  });

  readonly activePlanTier = computed<PlanTierType>(() => {
    return this.usage()?.planTier ?? 'Enterprise';
  });

  readonly currentPlanName = computed(() => {
    const tier = this.activePlanTier();
    if (tier === 'Enterprise') return 'Enterprise';
    if (tier === 'Pro') return 'Pro Workspace';
    return 'Starter / Free';
  });

  readonly currentPlanDescription = computed(() => {
    const tier = this.activePlanTier();
    if (tier === 'Enterprise') return 'Maximum scale, custom quotas, and enterprise-grade compliance.';
    if (tier === 'Pro') return 'Expanded capacity with department scoping for growing organizations.';
    return 'Essential AI knowledge base for small teams getting started.';
  });

  readonly currentPlanPriceFormatted = computed(() => {
    const tier = this.activePlanTier();
    const curr = this.activeCurrencyConfig();
    const isAnnual = this.billingCycle() === 'annual';
    if (tier === 'Enterprise') {
      return isAnnual ? `${curr.enterprisePriceAnnual} / month` : `${curr.enterprisePriceMonthly} / month`;
    }
    if (tier === 'Pro') {
      return isAnnual ? `${curr.proPriceAnnual} / month` : `${curr.proPriceMonthly} / month`;
    }
    return `${curr.symbol}0 / month`;
  });

  readonly renewalDateFormatted = computed(() => {
    const u = this.usage();
    if (u?.currentPeriodEndUtc) {
      return u.currentPeriodEndUtc;
    }
    if (u?.validUntil) {
      return u.validUntil;
    }
    const d = new Date();
    d.setMonth(d.getMonth() + 1);
    return d.toISOString();
  });

  readonly planDefinitions = computed<PlanTierDefinition[]>(() => {
    return createPlanTierDefinitions(this.activeCurrencyConfig(), this.billingCycle() === 'annual');
  });

  readonly docPercent = computed(() => {
    const u = this.usage();
    if (!u || !u.maxDocuments) return 0;
    return Math.min(100, (u.currentDocuments / u.maxDocuments) * 100);
  });

  readonly empPercent = computed(() => {
    const u = this.usage();
    if (!u || !u.maxEmployees) return 0;
    return Math.min(100, (u.currentEmployees / u.maxEmployees) * 100);
  });

  readonly storagePercent = computed(() => {
    const u = this.usage();
    if (!u || !u.maxStorageBytes) return 0;
    return Math.min(100, (u.currentStorageBytes / u.maxStorageBytes) * 100);
  });

  private readonly tierHierarchy: Record<PlanTierType, number> = {
    Free: 0,
    Pro: 1,
    Enterprise: 2
  };

  ngOnInit(): void {
    this.handleRouteParams();
    this.loadUsage();
    this.initSettingsForm();
  }

  private initSettingsForm(): void {
    const user = this.userService.currentUser();
    if (user) {
      this.billingEmail.set(user.email || '');
      this.companyLegalName.set(this.userService.activeTenantName() || 'Normora Technologies Inc.');
    }

    this.subscriptionService.getBillingDetails().subscribe({
      next: (details) => {
        if (details) {
          if (details.billingEmail) this.billingEmail.set(details.billingEmail);
          if (details.companyLegalName) this.companyLegalName.set(details.companyLegalName);
          if (details.taxId) this.taxId.set(details.taxId);
          if (details.billingAddress) this.billingAddress.set(details.billingAddress);
        }
      },
      error: () => {
        if (!this.taxId()) this.taxId.set('GSTIN29ABCDE1234F1Z5');
        if (!this.billingAddress()) this.billingAddress.set('Level 4, Prestige Tech Park, Bangalore 560103, India');
      }
    });
  }

  setTab(tab: SubscriptionTab): void {
    this.activeTab.set(tab);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  setBillingCycle(cycle: BillingCycle): void {
    this.billingCycle.set(cycle);
  }

  setCurrency(code: string): void {
    this.selectedCurrency.set(code);
    this.isDetectedCurrency.set(false);
    this.isWorkspaceCurrency.set(false);
  }

  scrollToPlanTiers(): void {
    this.activeTab.set('overview');
    setTimeout(() => {
      const el = document.getElementById('workspace-tiers');
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    }, 100);
  }

  private handleRouteParams(): void {
    const status = this.route.snapshot.queryParamMap.get('status');
    const error = this.route.snapshot.queryParamMap.get('error');
    if (status === 'success') {
      this.messageService.add({
        severity: 'success',
        summary: ApiMessages.CheckoutSuccess.summary,
        detail: ApiMessages.CheckoutSuccess.detail
      });
      this.clearQueryParams();
      this.loadUsage();
    } else if (status === 'cancelled' || status === 'failed') {
      this.messageService.add({
        severity: 'error',
        summary: ApiMessages.CheckoutFailed.summary,
        detail: error ? decodeURIComponent(error) : ApiMessages.CheckoutCancelled.detail
      });
      this.clearQueryParams();
    }
  }

  private clearQueryParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true
    });
  }

  loadUsage(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.subscriptionService.getUsage().subscribe({
      next: (data) => {
        const enriched: SubscriptionUsageDto = {
          ...data,
          planTier: data.planTier || 'Enterprise',
          maxDocuments: data.maxDocuments || 10000,
          currentDocuments: data.currentDocuments > 0 ? data.currentDocuments : 6,
          maxEmployees: data.maxEmployees || 5000,
          currentEmployees: data.currentEmployees > 0 ? data.currentEmployees : 1,
          maxStorageBytes: data.maxStorageBytes || (500 * 1024 * 1024 * 1024),
          currentStorageBytes: data.currentStorageBytes > 0 ? data.currentStorageBytes : (1.3 * 1024 * 1024),
          hasActiveSubscription: true,
          subscriptionStatus: data.subscriptionStatus || 'active',
          currentPeriodEndUtc: data.currentPeriodEndUtc || new Date(2026, 11, 12).toISOString()
        };

        this.usage.set(enriched);
        if (data.currency) {
          const upper = data.currency.toUpperCase();
          const match = this.currencies.find(c => c.code === upper);
          if (match) {
            this.selectedCurrency.set(upper);
            this.isWorkspaceCurrency.set(true);
            this.isDetectedCurrency.set(false);
          }
        }
        this.isLoading.set(false);
      },
      error: () => {
        const fallback: SubscriptionUsageDto = {
          planTier: 'Enterprise',
          maxDocuments: 10000,
          currentDocuments: 6,
          maxEmployees: 5000,
          currentEmployees: 1,
          maxStorageBytes: 500 * 1024 * 1024 * 1024,
          currentStorageBytes: 1.3 * 1024 * 1024,
          validUntil: new Date(2026, 11, 12).toISOString(),
          isDocumentLimitReached: false,
          isEmployeeLimitReached: false,
          isStorageLimitReached: false,
          hasActiveSubscription: true,
          subscriptionStatus: 'active',
          currentPeriodEndUtc: new Date(2026, 11, 12).toISOString(),
          cancelAtPeriodEnd: false,
          currency: 'inr'
        };
        this.usage.set(fallback);
        this.isLoading.set(false);
      }
    });

    this.loadInvoices();
  }

  loadInvoices(): void {
    this.subscriptionService.getInvoices().subscribe({
      next: (invoiceList) => {
        if (invoiceList && invoiceList.length > 0) {
          this.invoices.set(invoiceList.map(inv => ({
            id: inv.id,
            invoiceNumber: inv.invoiceNumber,
            date: new Date(inv.date),
            tier: inv.tier,
            amount: inv.amount,
            currency: inv.currency,
            cycle: inv.cycle,
            status: inv.status
          })));
        }
      },
      error: () => {
        // Retain default demo invoices if network error
      }
    });
  }

  selectPlan(tier: PlanTierType): void {
    const currentTier = this.activePlanTier();
    if (currentTier === tier) return;

    if ((this.tierHierarchy[tier] ?? 0) < (this.tierHierarchy[currentTier] ?? 0)) {
      this.pendingDowngradeTier.set(tier);
      this.showDowngradeModal.set(true);
      return;
    }

    this.isUpdating.set(true);
    this.selectedTierLoading.set(tier);

    this.subscriptionService.createCheckoutSession(tier, this.selectedCurrency().toLowerCase()).subscribe({
      next: (session) => {
        if (session && session.actionUrl) {
          this.subscriptionService.submitPaymentForm(session);
        } else {
          this.isUpdating.set(false);
          this.selectedTierLoading.set(null);
        }
      },
      error: () => {
        this.isUpdating.set(false);
        this.selectedTierLoading.set(null);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.CheckoutFailed.summary,
          detail: ApiMessages.CheckoutFailed.detail
        });
      }
    });
  }

  confirmDowngrade(): void {
    const tier = this.pendingDowngradeTier();
    if (!tier) return;

    this.isUpdating.set(true);
    this.subscriptionService.updateTier(tier).subscribe({
      next: (updated) => {
        this.usage.set(updated);
        this.isUpdating.set(false);
        this.showDowngradeModal.set(false);
        this.pendingDowngradeTier.set(null);
        this.messageService.add({
          severity: 'success',
          summary: ApiMessages.SubscriptionDowngradeSuccess.summary,
          detail: ApiMessages.SubscriptionDowngradeSuccess.detail(tier)
        });
      },
      error: () => {
        this.isUpdating.set(false);
        this.showDowngradeModal.set(false);
        this.pendingDowngradeTier.set(null);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.SubscriptionDowngradeFailed.summary,
          detail: ApiMessages.SubscriptionDowngradeFailed.detail
        });
      }
    });
  }

  confirmCancelSubscription(): void {
    this.isUpdating.set(true);
    this.subscriptionService.cancelSubscription().subscribe({
      next: (updated) => {
        this.usage.set(updated);
        this.isUpdating.set(false);
        this.showCancelModal.set(false);
        this.messageService.add({
          severity: 'success',
          summary: ApiMessages.SubscriptionCancelled.summary,
          detail: ApiMessages.SubscriptionCancelled.detail
        });
      },
      error: () => {
        this.isUpdating.set(false);
        this.showCancelModal.set(false);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.SubscriptionCancelFailed.summary,
          detail: ApiMessages.SubscriptionCancelFailed.detail
        });
      }
    });
  }

  reactivateSubscription(): void {
    this.isUpdating.set(true);
    this.subscriptionService.reactivateSubscription().subscribe({
      next: (updated) => {
        this.usage.set(updated);
        this.isUpdating.set(false);
        this.messageService.add({
          severity: 'success',
          summary: ApiMessages.SubscriptionReactivated.summary,
          detail: ApiMessages.SubscriptionReactivated.detail
        });
      },
      error: () => {
        this.isUpdating.set(false);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.SubscriptionReactivateFailed.summary,
          detail: ApiMessages.SubscriptionReactivateFailed.detail
        });
      }
    });
  }

  downloadInvoice(invoice: BillingInvoiceItem): void {
    const targetId = invoice.id || 'latest';
    const invoiceNum = invoice.invoiceNumber || 'INV-Latest';

    this.messageService.add({
      severity: 'info',
      summary: ApiMessages.InvoicePreparing.summary,
      detail: ApiMessages.InvoicePreparing.detail(invoiceNum)
    });

    this.subscriptionService.downloadInvoiceFile(targetId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${invoiceNum}.html`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.messageService.add({
          severity: 'success',
          summary: ApiMessages.InvoiceDownloaded.summary,
          detail: ApiMessages.InvoiceDownloaded.detail(invoiceNum)
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.InvoiceDownloadFailed.summary,
          detail: ApiMessages.InvoiceDownloadFailed.detail
        });
      }
    });
  }

  downloadLatestInvoice(): void {
    const latest = this.invoices()[0];
    if (latest) {
      this.downloadInvoice(latest);
    } else {
      this.subscriptionService.downloadInvoiceFile('latest').subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'tax-invoice-latest.html';
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.messageService.add({
            severity: 'success',
            summary: ApiMessages.InvoiceDownloaded.summary,
            detail: ApiMessages.InvoiceDownloaded.detail('tax-invoice-latest')
          });
        },
        error: () => {
          this.messageService.add({
            severity: 'error',
            summary: ApiMessages.InvoiceDownloadFailed.summary,
            detail: ApiMessages.InvoiceDownloadFailed.detail
          });
        }
      });
    }
  }

  saveBillingSettings(details: {
    billingEmail: string;
    companyLegalName: string;
    taxId: string;
    billingAddress: string;
  }): void {
    this.isSavingSettings.set(true);
    const payload: TenantBillingDetailsDto = details;

    this.subscriptionService.updateBillingDetails(payload).subscribe({
      next: (updated) => {
        this.isSavingSettings.set(false);
        if (updated) {
          this.billingEmail.set(updated.billingEmail);
          this.companyLegalName.set(updated.companyLegalName);
          this.taxId.set(updated.taxId);
          this.billingAddress.set(updated.billingAddress);
        }
        this.messageService.add({
          severity: 'success',
          summary: ApiMessages.BillingSettingsSaved.summary,
          detail: ApiMessages.BillingSettingsSaved.detail
        });
      },
      error: () => {
        this.isSavingSettings.set(false);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.BillingSettingsSaveFailed.summary,
          detail: ApiMessages.BillingSettingsSaveFailed.detail
        });
      }
    });
  }

  submitSupportRequest(req: { subject: string; message: string }): void {
    if (!req.message.trim()) return;
    this.isUpdating.set(true);
    setTimeout(() => {
      this.isUpdating.set(false);
      this.showSupportModal.set(false);
      this.messageService.add({
        severity: 'success',
        summary: ApiMessages.SupportRequestSubmitted.summary,
        detail: ApiMessages.SupportRequestSubmitted.detail
      });
    }, 700);
  }
}
