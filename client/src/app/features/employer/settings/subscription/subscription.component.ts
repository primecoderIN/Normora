import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageService } from 'primeng/api';
import { SubscriptionService, SubscriptionUsageDto, PlanTierType } from '@core/services/subscription.service';
import { UserService } from '@core/services/user.service';
import { ApiMessages } from '@core/constants/api-messages';

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

@Component({
  selector: 'app-subscription',
  standalone: true,
  imports: [CommonModule, ButtonModule, SkeletonModule, DatePipe],
  templateUrl: './subscription.component.html'
})
export class SubscriptionComponent implements OnInit {
  private subscriptionService = inject(SubscriptionService);
  private messageService = inject(MessageService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  public userService = inject(UserService);

  usage = signal<SubscriptionUsageDto | null>(null);
  isLoading = signal(true);
  isUpdating = signal(false);
  isOpeningPortal = signal(false);
  selectedTierLoading = signal<PlanTierType | null>(null);
  errorMessage = signal<string | null>(null);

  planDefinitions: PlanTierDefinition[] = [
    {
      tier: 'Free',
      name: 'Starter / Free',
      price: '$0',
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
      price: '$49',
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
      price: '$249',
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

  ngOnInit(): void {
    this.handleRouteParams();
    this.loadUsage();
  }

  private handleRouteParams(): void {
    const status = this.route.snapshot.queryParamMap.get('status');
    if (status === 'success') {
      this.messageService.add({
        severity: 'success',
        summary: ApiMessages.CheckoutSuccess.summary,
        detail: ApiMessages.CheckoutSuccess.detail
      });
      this.clearQueryParams();
    } else if (status === 'cancelled') {
      this.messageService.add({
        severity: 'warn',
        summary: ApiMessages.CheckoutCancelled.summary,
        detail: ApiMessages.CheckoutCancelled.detail
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
        this.usage.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set(ApiMessages.SubscriptionQuotaFailed.detail);
        this.isLoading.set(false);
      }
    });
  }

  selectPlan(tier: PlanTierType): void {
    if (this.usage()?.planTier === tier) return;

    if (tier === 'Free') {
      this.isUpdating.set(true);
      this.selectedTierLoading.set(tier);
      this.subscriptionService.updateTier(tier).subscribe({
        next: (updated) => {
          this.usage.set(updated);
          this.isUpdating.set(false);
          this.selectedTierLoading.set(null);
          this.messageService.add({
            severity: 'success',
            summary: ApiMessages.SubscriptionUpdated.summary,
            detail: `Your workspace has been switched to the ${tier} plan.`
          });
        },
        error: () => {
          this.isUpdating.set(false);
          this.selectedTierLoading.set(null);
          this.messageService.add({
            severity: 'error',
            summary: ApiMessages.SubscriptionUpdateFailed.summary,
            detail: ApiMessages.SubscriptionUpdateFailed.detail
          });
        }
      });
      return;
    }

    // For paid tiers (Pro, Enterprise), initiate Stripe Hosted Checkout Session
    this.isUpdating.set(true);
    this.selectedTierLoading.set(tier);

    this.subscriptionService.createCheckoutSession(tier).subscribe({
      next: (session) => {
        if (session.checkoutUrl) {
          window.location.href = session.checkoutUrl;
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
