import { CurrencyConfig, PlanTierDefinition } from '../models/subscription.models';

export function createPlanTierDefinitions(curr: CurrencyConfig, isAnnual: boolean): PlanTierDefinition[] {
  return [
    {
      tier: 'Free',
      name: 'Starter / Free',
      price: `${curr.symbol}0`,
      period: isAnnual ? '/ year' : '/ month',
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
      price: isAnnual ? curr.proPriceAnnual : curr.proPriceMonthly,
      period: isAnnual ? '/ month, billed annually' : '/ month',
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
      price: isAnnual ? curr.enterprisePriceAnnual : curr.enterprisePriceMonthly,
      period: isAnnual ? '/ month, billed annually' : '/ month',
      description: 'Maximum scale, custom quotas, and enterprise-grade compliance.',
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
}
