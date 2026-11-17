import { PlanTierType } from '@core/services/subscription.service';

export type SubscriptionTab = 'overview' | 'history' | 'invoices' | 'settings';
export type BillingCycle = 'monthly' | 'annual';

export interface PlanTierDefinition {
  tier: PlanTierType;
  name: string;
  price: string;
  period: string;
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
  proPriceMonthly: string;
  proPriceAnnual: string;
  enterprisePriceMonthly: string;
  enterprisePriceAnnual: string;
  proNumericMonthly: number;
  enterpriseNumericMonthly: number;
}

export interface BillingInvoiceItem {
  id: string;
  invoiceNumber: string;
  date: Date;
  tier: string;
  amount: string;
  currency: string;
  cycle: string;
  status: 'Paid' | 'Pending' | 'Failed';
  pdfUrl?: string;
}
