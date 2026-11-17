import { Component, input, output, linkedSignal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-subscription-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-settings.component.html'
})
export class SubscriptionSettingsComponent {
  billingEmail = input<string>('');
  companyLegalName = input<string>('');
  taxId = input<string>('');
  billingAddress = input<string>('');
  isSavingSettings = input<boolean>(false);
  cancelAtPeriodEnd = input<boolean>(false);

  onSaveBillingSettings = output<{
    billingEmail: string;
    companyLegalName: string;
    taxId: string;
    billingAddress: string;
  }>();
  onOpenCancelModal = output<void>();
  onReactivateSubscription = output<void>();
  onBackToOverview = output<void>();

  // Local editable form fields cleanly linked to parent input signals
  emailField = linkedSignal(() => this.billingEmail());
  nameField = linkedSignal(() => this.companyLegalName());
  taxIdField = linkedSignal(() => this.taxId());
  addressField = linkedSignal(() => this.billingAddress());

  save(): void {
    this.onSaveBillingSettings.emit({
      billingEmail: this.emailField(),
      companyLegalName: this.nameField(),
      taxId: this.taxIdField(),
      billingAddress: this.addressField()
    });
  }
}
