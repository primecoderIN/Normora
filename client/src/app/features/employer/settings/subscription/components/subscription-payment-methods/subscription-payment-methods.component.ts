import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-subscription-payment-methods',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subscription-payment-methods.component.html'
})
export class SubscriptionPaymentMethodsComponent {
  isOpeningPortal = input<boolean>(false);

  onOpenCustomerPortal = output<void>();
  onOpenAddPaymentModal = output<void>();
  onBackToOverview = output<void>();
}
