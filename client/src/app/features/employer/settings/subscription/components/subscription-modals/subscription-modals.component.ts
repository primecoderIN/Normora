import { Component, input, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlanTierType, SubscriptionUsageDto } from '@core/services/subscription.service';

@Component({
  selector: 'app-subscription-modals',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DatePipe, DecimalPipe],
  templateUrl: './subscription-modals.component.html'
})
export class SubscriptionModalsComponent {
  showQuotaDetails = input<boolean>(false);
  showDowngradeModal = input<boolean>(false);
  showCancelModal = input<boolean>(false);
  showSupportModal = input<boolean>(false);

  usage = input<SubscriptionUsageDto | null>(null);
  currentPlanName = input<string>('');
  pendingDowngradeTier = input<PlanTierType | null>(null);
  renewalDateFormatted = input<string>('');
  isUpdating = input<boolean>(false);
  isSubmittingSupport = input<boolean>(false);

  onCloseQuotaDetails = output<void>();
  onCloseDowngradeModal = output<void>();
  onConfirmDowngrade = output<void>();
  onCloseCancelModal = output<void>();
  onConfirmCancel = output<void>();
  onCloseSupportModal = output<void>();
  onSubmitSupport = output<{ subject: string; message: string }>();

  supportSubject = signal('');
  supportMessage = signal('');
  cancellationReason = signal('cost');
  cancellationFeedback = signal('');

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

  submitSupport(): void {
    if (!this.supportMessage().trim()) return;
    this.onSubmitSupport.emit({
      subject: this.supportSubject(),
      message: this.supportMessage()
    });
    this.supportSubject.set('');
    this.supportMessage.set('');
  }
}
