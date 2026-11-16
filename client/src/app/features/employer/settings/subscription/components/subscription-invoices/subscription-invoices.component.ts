import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { BillingInvoiceItem } from '../../models/subscription.models';

@Component({
  selector: 'app-subscription-invoices',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './subscription-invoices.component.html'
})
export class SubscriptionInvoicesComponent {
  invoices = input<BillingInvoiceItem[]>([]);
  title = input<string>('Billing & Transaction History');
  description = input<string>('Review your past invoices, receipts, and payment transactions.');
  isCardView = input<boolean>(false);

  onDownloadInvoice = output<BillingInvoiceItem>();
  onBackToOverview = output<void>();
}
