import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { SubscriptionService, SubscriptionUsageDto, PlanTierType } from '@core/services/subscription.service';
import { UserService } from '@core/services/user.service';
import { ApiMessages } from '@core/constants/api-messages';
import { DsToastService } from '@design-system';

interface PlanTierDefinition {
  tier: PlanTierType;
  name: string;
  price: string;
  description: string;
  maxDocs: string;
  maxMembers: string;
  maxStorage: string;
  features: string[];
  isPopular?: boolean;
}

export interface CurrencyConfig {
  code: string;
  symbol: string;
  label: string;
  flag: string;
  proPrice: string;
  enterprisePrice: string;
}

export const SUPPORTED_CURRENCIES: CurrencyConfig[] = [
  { code: 'USD', symbol: '$', label: 'USD ($)', flag: '🇺🇸', proPrice: '$49', enterprisePrice: '$249' },
  { code: 'EUR', symbol: '€', label: 'EUR (€)', flag: '🇪🇺', proPrice: '€45', enterprisePrice: '€229' },
  { code: 'GBP', symbol: '£', label: 'GBP (£)', flag: '🇬🇧', proPrice: '£39', enterprisePrice: '£199' },
  { code: 'INR', symbol: '₹', label: 'INR (₹)', flag: '🇮🇳', proPrice: '₹3,999', enterprisePrice: '₹19,999' },
  { code: 'CAD', symbol: 'C$', label: 'CAD (C$)', flag: '🇨🇦', proPrice: 'C$65', enterprisePrice: 'C$329' },
  { code: 'AUD', symbol: 'A$', label: 'AUD (A$)', flag: '🇦🇺', proPrice: 'A$75', enterprisePrice: 'A$379' },
  { code: 'JPY', symbol: '¥', label: 'JPY (¥)', flag: '🇯🇵', proPrice: '¥7,500', enterprisePrice: '¥38,000' },
  { code: 'SGD', symbol: 'S$', label: 'SGD (S$)', flag: '🇸🇬', proPrice: 'S$65', enterprisePrice: 'S$329' },
];

@Component({
  selector: 'app-subscription',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './subscription.component.html'
})
export class SubscriptionComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);
  private messageService = inject(DsToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  public userService = inject(UserService);

  usage = signal<SubscriptionUsageDto | null>(null);
  isLoading = signal(true);
  isUpdating = signal(false);
  isOpeningPortal = signal(false);
  selectedTierLoading = signal<PlanTierType | null>(null);
  errorMessage = signal<string | null>(null);

  readonly currencies = SUPPORTED_CURRENCIES;
  readonly selectedCurrency = signal<string>(this.detectLocalCurrency());
  readonly isDetectedCurrency = signal<boolean>(true);
  readonly isWorkspaceCurrency = signal<boolean>(false);

  readonly activeCurrencyConfig = computed<CurrencyConfig>(() => {
    return this.currencies.find(c => c.code === this.selectedCurrency()) || this.currencies[0];
  });

  readonly planDefinitions = computed<PlanTierDefinition[]>(() => {
    const curr = this.activeCurrencyConfig();
    return [
      {
        tier: 'Free',
        name: 'Starter / Free',
        price: `${curr.symbol}0`,
        description: 'Essential AI knowledge base for small teams getting started.',
        maxDocs: '10 Documents',
        maxMembers: '5 Team Members',
        maxStorage: '50 MB Storage',
        features: [
          'Gemini Grounded RAG Answering',
          'Source Citations & Chunk Previews',
          'Company-wide Knowledge Base',
          'Standard Ingestion Pipeline'
        ]
      },
      {
        tier: 'Pro',
        name: 'Pro Workspace',
        price: curr.proPrice,
        description: 'Expanded capacity with department scoping for growing organizations.',
        maxDocs: '100 Documents',
        maxMembers: '50 Team Members',
        maxStorage: '5 GB Storage',
        isPopular: true,
        features: [
          'Everything in Starter',
          'Department & User Group Scoping',
          'Document Version History & Outdated Alerts',
          'Markdown, PDF & Word Export',
          'Priority Hangfire Ingestion'
        ]
      },
      {
        tier: 'Enterprise',
        name: 'Enterprise',
        price: curr.enterprisePrice,
        description: 'Maximum scale, custom quotas, and enterprise-grade compliance governance.',
        maxDocs: '10,000 Documents',
        maxMembers: '5,000 Team Members',
        maxStorage: '500 GB Storage',
        features: [
          'Everything in Pro',
          'Full Compliance Audit Logging',
          'White-Label Branding & Custom Subdomains',
          'Dedicated pgvector Search Indexing',
          '99.9% Uptime SLA & Priority Support'
        ]
      }
    ];
  });

  setCurrency(code: string): void {
    this.selectedCurrency.set(code);
    this.isDetectedCurrency.set(false);
    this.isWorkspaceCurrency.set(false);
  }

  private detectLocalCurrency(): string {
    try {
      const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || '';
      const tzLower = timeZone.toLowerCase();
      const lang = (typeof navigator !== 'undefined' ? navigator.language : '').toLowerCase();

      if (tzLower.includes('calcutta') || tzLower.includes('kolkata') || lang.includes('en-in') || lang.includes('hi')) {
        return 'INR';
      }
      if (tzLower.includes('london') || lang.includes('en-gb')) {
        return 'GBP';
      }
      if (
        tzLower.includes('paris') ||
        tzLower.includes('berlin') ||
        tzLower.includes('rome') ||
        tzLower.includes('madrid') ||
        tzLower.includes('amsterdam') ||
        tzLower.includes('dublin') ||
        tzLower.includes('brussels') ||
        tzLower.includes('vienna') ||
        tzLower.includes('lisbon') ||
        tzLower.includes('helsinki') ||
        tzLower.includes('athens') ||
        lang.includes('de') ||
        lang.includes('fr') ||
        lang.includes('it') ||
        lang.includes('es') ||
        lang.includes('nl')
      ) {
        return 'EUR';
      }
      if (tzLower.includes('toronto') || tzLower.includes('vancouver') || tzLower.includes('montreal') || lang.includes('en-ca') || lang.includes('fr-ca')) {
        return 'CAD';
      }
      if (tzLower.includes('sydney') || tzLower.includes('melbourne') || tzLower.includes('brisbane') || tzLower.includes('perth') || lang.includes('en-au')) {
        return 'AUD';
      }
      if (tzLower.includes('tokyo') || lang.includes('ja')) {
        return 'JPY';
      }
      if (tzLower.includes('singapore') || lang.includes('en-sg')) {
        return 'SGD';
      }
    } catch {
      // fallback
    }
    return 'USD';
  }

  docPercent = computed(() => {
    const u = this.usage();
    if (!u || u.maxDocuments === 0) return 0;
    return Math.min(100, Math.round((u.currentDocuments / u.maxDocuments) * 100));
  });

  empPercent = computed(() => {
    const u = this.usage();
    if (!u || u.maxEmployees === 0) return 0;
    return Math.min(100, Math.round((u.currentEmployees / u.maxEmployees) * 100));
  });

  storagePercent = computed(() => {
    const u = this.usage();
    if (!u || u.maxStorageBytes === 0) return 0;
    return Math.min(100, Math.round((u.currentStorageBytes / u.maxStorageBytes) * 100));
  });

  // Strict tier hierarchy ranking: Free (0) < Pro (1) < Enterprise (2)
  private readonly tierHierarchy: Record<PlanTierType, number> = {
    Free: 0,
    Pro: 1,
    Enterprise: 2
  };

  /**
   * Returns numeric hierarchy index for a given plan tier.
   */
  getTierLevel(tier: PlanTierType): number {
    return this.tierHierarchy[tier] ?? 0;
  }

  /**
   * Evaluates if the tier matches the workspace's active subscription tier.
   */
  isTierCurrent(tier: PlanTierType): boolean {
    return (this.usage()?.planTier ?? 'Free') === tier;
  }

  /**
   * Evaluates if the tier is lower than the active subscription (locked/disabled).
   */
  isTierDowngrade(tier: PlanTierType): boolean {
    const currentTier = this.usage()?.planTier ?? 'Free';
    return this.getTierLevel(tier) < this.getTierLevel(currentTier);
  }

  /**
   * Evaluates if the tier represents a viable upgrade pathway.
   */
  isTierUpgrade(tier: PlanTierType): boolean {
    const currentTier = this.usage()?.planTier ?? 'Free';
    return this.getTierLevel(tier) > this.getTierLevel(currentTier);
  }

  ngOnInit(): void {
    this.handleRouteParams();
    this.loadUsage();
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

  /**
   * Loads resource usage and quota metrics for the active workspace.
   */
  loadUsage(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.subscriptionService.getUsage().subscribe({
      next: (data) => {
        this.usage.set(data);
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
        this.errorMessage.set(ApiMessages.SubscriptionQuotaFailed.detail);
        this.isLoading.set(false);
      }
    });
  }

  selectPlan(tier: PlanTierType): void {
    // Business rule: Only upgrade pathways can be selected. Lower/previous tiers are locked.
    if (!this.isTierUpgrade(tier)) {
      return;
    }

    // For paid tiers (Pro, Enterprise), initiate PayU Hosted Checkout Session
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

  openCustomerPortal(): void {
    this.isOpeningPortal.set(true);
    this.subscriptionService.createCustomerPortalSession().subscribe({
      next: (session) => {
        if (session.portalUrl) {
          window.location.href = session.portalUrl;
        } else {
          this.isOpeningPortal.set(false);
        }
      },
      error: () => {
        this.isOpeningPortal.set(false);
        this.messageService.add({
          severity: 'error',
          summary: ApiMessages.PortalFailed.summary,
          detail: ApiMessages.PortalFailed.detail
        });
      }
    });
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  getBarColorClass(percent: number): string {
    if (percent >= 100) return 'bg-rose-500';
    if (percent >= 80) return 'bg-amber-500';
    return 'bg-primary-600';
  }

  getBadgeClass(percent: number): string {
    if (percent >= 100) return 'text-rose-600 dark:text-rose-400 bg-rose-500/10 border-rose-500/20';
    if (percent >= 80) return 'text-amber-600 dark:text-amber-400 bg-amber-500/10 border-amber-500/20';
    return 'text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 border-emerald-500/20';
  }
}
