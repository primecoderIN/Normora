import { Injectable, signal, computed } from '@angular/core';

export type DsToastSeverity = 'success' | 'info' | 'warn' | 'warning' | 'error';

export interface DsToastMessage {
  id?: string;
  severity?: DsToastSeverity;
  summary?: string;
  detail?: string;
  life?: number;
}

export interface DsActiveToast extends DsToastMessage {
  id: string;
  severity: DsToastSeverity;
  createdAt: number;
}

/**
 * Enterprise Toast Notification Service.
 * Replaces PrimeNG MessageService with a modern, signal-based notification manager.
 * Provides full backward compatibility for `add({ severity, summary, detail, life })`.
 */
@Injectable({
  providedIn: 'root',
})
export class DsToastService {
  private static nextId = 0;
  private readonly _toasts = signal<DsActiveToast[]>([]);

  readonly toasts = computed(() => this._toasts());

  add(message: DsToastMessage): void {
    const id = message.id || `toast-${++DsToastService.nextId}`;
    let severity: DsToastSeverity = message.severity || 'info';
    if (severity === 'warn') {
      severity = 'warning';
    }

    const toast: DsActiveToast = {
      ...message,
      id,
      severity,
      createdAt: Date.now(),
    };

    this._toasts.update((current) => [...current, toast]);

    const life = message.life ?? 4000;
    if (life > 0) {
      setTimeout(() => {
        this.remove(id);
      }, life);
    }
  }

  addAll(messages: DsToastMessage[]): void {
    messages.forEach((msg) => this.add(msg));
  }

  remove(id: string): void {
    this._toasts.update((current) => current.filter((t) => t.id !== id));
  }

  clear(): void {
    this._toasts.set([]);
  }

  success(summary: string, detail?: string, life?: number): void {
    this.add({ severity: 'success', summary, detail, life });
  }

  error(summary: string, detail?: string, life?: number): void {
    this.add({ severity: 'error', summary, detail, life });
  }

  info(summary: string, detail?: string, life?: number): void {
    this.add({ severity: 'info', summary, detail, life });
  }

  warning(summary: string, detail?: string, life?: number): void {
    this.add({ severity: 'warning', summary, detail, life });
  }
}
