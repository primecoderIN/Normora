import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '@env/environment';
import { Subject } from 'rxjs';

export interface NotificationEvent {
  id: string;
  type: string;
  message: string;
  tenantName: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService {
  private connection?: HubConnection;

  private invitationReceivedSource = new Subject<NotificationEvent>();
  invitationReceived$ = this.invitationReceivedSource.asObservable();

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

    await this.connection.start();
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
  }
}
