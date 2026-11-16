import { CurrencyConfig } from '../models/subscription.models';

export const SUPPORTED_CURRENCIES: CurrencyConfig[] = [
  {
    code: 'INR',
    symbol: '₹',
    label: 'INR (₹)',
    flag: '🇮🇳',
    proPriceMonthly: '₹3,999',
    proPriceAnnual: '₹3,199',
    enterprisePriceMonthly: '₹19,999',
    enterprisePriceAnnual: '₹15,999',
    proNumericMonthly: 3999,
    enterpriseNumericMonthly: 19999
  },
  {
    code: 'USD',
    symbol: '$',
    label: 'USD ($)',
    flag: '🇺🇸',
    proPriceMonthly: '$49',
    proPriceAnnual: '$39',
    enterprisePriceMonthly: '$249',
    enterprisePriceAnnual: '$199',
    proNumericMonthly: 49,
    enterpriseNumericMonthly: 249
  },
  {
    code: 'EUR',
    symbol: '€',
    label: 'EUR (€)',
    flag: '🇪🇺',
    proPriceMonthly: '€45',
    proPriceAnnual: '€36',
    enterprisePriceMonthly: '€229',
    enterprisePriceAnnual: '€183',
    proNumericMonthly: 45,
    enterpriseNumericMonthly: 229
  },
  {
    code: 'GBP',
    symbol: '£',
    label: 'GBP (£)',
    flag: '🇬🇧',
    proPriceMonthly: '£39',
    proPriceAnnual: '£31',
    enterprisePriceMonthly: '£199',
    enterprisePriceAnnual: '£159',
    proNumericMonthly: 39,
    enterpriseNumericMonthly: 199
  },
  {
    code: 'CAD',
    symbol: 'C$',
    label: 'CAD (C$)',
    flag: '🇨🇦',
    proPriceMonthly: 'C$65',
    proPriceAnnual: 'C$52',
    enterprisePriceMonthly: 'C$329',
    enterprisePriceAnnual: 'C$263',
    proNumericMonthly: 65,
    enterpriseNumericMonthly: 329
  },
  {
    code: 'AUD',
    symbol: 'A$',
    label: 'AUD (A$)',
    flag: '🇦🇺',
    proPriceMonthly: 'A$75',
    proPriceAnnual: 'A$60',
    enterprisePriceMonthly: 'A$379',
    enterprisePriceAnnual: 'A$303',
    proNumericMonthly: 75,
    enterpriseNumericMonthly: 379
  },
  {
    code: 'JPY',
    symbol: '¥',
    label: 'JPY (¥)',
    flag: '🇯🇵',
    proPriceMonthly: '¥7,500',
    proPriceAnnual: '¥6,000',
    enterprisePriceMonthly: '¥38,000',
    enterprisePriceAnnual: '¥30,400',
    proNumericMonthly: 7500,
    enterpriseNumericMonthly: 38000
  },
  {
    code: 'SGD',
    symbol: 'S$',
    label: 'SGD (S$)',
    flag: '🇸🇬',
    proPriceMonthly: 'S$65',
    proPriceAnnual: 'S$52',
    enterprisePriceMonthly: 'S$329',
    enterprisePriceAnnual: 'S$263',
    proNumericMonthly: 65,
    enterpriseNumericMonthly: 329
  }
];

export function detectLocalCurrency(): string {
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
  return 'INR';
}
