import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-subscription-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-settings.component.html'
})
export class SubscriptionSettingsComponent {
  billingEmail = input<string>('');
  companyLegalName = input<string>('');
  taxId = input<string>('');
  billingAddress = input<string>('');
  isSavingSettings = input<boolean>(false);

  onSaveBillingSettings = output<{
    billingEmail: string;
    companyLegalName: string;
    taxId: string;
    billingAddress: string;
  }>();
  onOpenCancelModal = output<void>();
  onBackToOverview = output<void>();

  // Local editable form fields
  emailField = signal('');
  nameField = signal('');
  taxIdField = signal('');
  addressField = signal('');

  constructor() {
    effect(() => {
      this.emailField.set(this.billingEmail());
    });
    effect(() => {
      this.nameField.set(this.companyLegalName());
    });
    effect(() => {
      this.taxIdField.set(this.taxId());
    });
    effect(() => {
      this.addressField.set(this.billingAddress());
    });
  }

  save(): void {
    this.onSaveBillingSettings.emit({
      billingEmail: this.emailField(),
      companyLegalName: this.nameField(),
      taxId: this.taxIdField(),
      billingAddress: this.addressField()
    });
  }
}
