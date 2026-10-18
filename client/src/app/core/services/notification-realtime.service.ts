import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '@env/environment';
import { Subject } from 'rxjs';

export interface NotificationEvent {
  id: string;
  type: string;
  message: string;
  tenantName: string;
}

/**
 * Service managing real-time notifications via SignalR (e.g. employee invitations).
 */
@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService {
  private connection?: HubConnection;

  private invitationReceivedSource = new Subject<NotificationEvent>();
  /** Stream of incoming user invitations pushed from the server */
  invitationReceived$ = this.invitationReceivedSource.asObservable();

  /**
   * Initializes the SignalR notification hub connection with automatic reconnection.
   */
  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        headers: { 'X-CSRF': '1' },
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveInvitation', (event: NotificationEvent) => {
      this.invitationReceivedSource.next(event);
    });

    try {
      await this.connection.start();
    } catch (err) {
      console.error('Failed to connect to notification hub', err);
    }
  }

  /**
   * Disconnects from the notification hub and cleans up connection resources.
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
  }
}
