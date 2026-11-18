import { Component, input, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlanTierType, SubscriptionUsageDto } from '@core/services/subscription.service';
import { BillingCycle, CurrencyConfig, PlanTierDefinition, SubscriptionTab } from '../../models/subscription.models';
import { SUPPORTED_CURRENCIES } from '../../helpers/subscription-currency.helper';

@Component({
  selector: 'app-subscription-overview',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DatePipe, DecimalPipe],
  templateUrl: './subscription-overview.component.html'
})
export class SubscriptionOverviewComponent {
  usage = input<SubscriptionUsageDto | null>(null);
  billingCycle = input<BillingCycle>('monthly');
  selectedCurrency = input<string>('INR');
  currencies = input<CurrencyConfig[]>(SUPPORTED_CURRENCIES);
  planDefinitions = input<PlanTierDefinition[]>([]);
  currentPlanName = input<string>('');
  currentPlanDescription = input<string>('');
  currentPlanPriceFormatted = input<string>('');
  renewalDateFormatted = input<string | null>(null);
  isUpdating = input<boolean>(false);
  selectedTierLoading = input<PlanTierType | null>(null);
  docPercent = input<number>(0);
  empPercent = input<number>(0);
  storagePercent = input<number>(0);
  isFreePlan = input<boolean>(true);

  onBillingCycleChange = output<BillingCycle>();
  onCurrencyChange = output<string>();
  onSelectPlan = output<PlanTierType>();
  onScrollToPlanTiers = output<void>();
  onSetTab = output<SubscriptionTab>();
  onOpenQuotaDetails = output<void>();
  onOpenCancelModal = output<void>();
  onReactivateSubscription = output<void>();
  onOpenSupportModal = output<void>();
  onDownloadLatestInvoice = output<void>();

  private readonly tierHierarchy: Record<PlanTierType, number> = {
    Free: 0,
    Pro: 1,
    Enterprise: 2
  };

  get activePlanTier(): PlanTierType {
    return this.usage()?.planTier ?? 'Free';
  }

  isTierCurrent(tier: PlanTierType): boolean {
    return this.activePlanTier === tier;
  }

  isTierDowngrade(tier: PlanTierType): boolean {
    return (this.tierHierarchy[tier] ?? 0) < (this.tierHierarchy[this.activePlanTier] ?? 0);
  }

  formatPercent(current: number, max: number): string {
    if (!max || max <= 0 || current <= 0) return '0%';
    const ratio = (current / max) * 100;
    if (ratio < 0.01) return '< 0.01%';
    if (ratio < 1) return ratio.toFixed(2) + '%';
    return Math.round(ratio) + '%';
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
    return 'bg-linear-to-r from-primary-600 to-indigo-600';
  }

  getBadgeClass(percent: number): string {
    if (percent >= 100) return 'text-rose-600 dark:text-rose-400 bg-rose-500/10 border-rose-500/20';
    if (percent >= 80) return 'text-amber-600 dark:text-amber-400 bg-amber-500/10 border-amber-500/20';
    return 'text-emerald-600 dark:text-emerald-400 bg-emerald-500/10 border-emerald-500/20';
  }
}
