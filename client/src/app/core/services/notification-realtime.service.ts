import { Injectable, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { firstValueFrom, Subject } from 'rxjs';
import { environment } from '@env/environment';

export interface InvitationReceivedEvent {
  tenantName: string;
  timestamp: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService {
  private oidcSecurityService = inject(OidcSecurityService);
  private connection?: HubConnection;

  // We use a Subject so any component can easily subscribe to notifications
  private invitationReceivedSubject = new Subject<InvitationReceivedEvent>();
  public invitationReceived$ = this.invitationReceivedSubject.asObservable();

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => firstValueFrom(this.oidcSecurityService.getAccessToken())
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ReceiveInvitation', (event: InvitationReceivedEvent) => {
      this.invitationReceivedSubject.next(event);
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
