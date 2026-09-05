import { Injectable, inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DocumentStatusChanged {
  documentId: string;
  tenantId: string;
  fileName: string;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Failed';
}

@Injectable({ providedIn: 'root' })
export class DocumentRealtimeService {
  private oidcSecurityService = inject(OidcSecurityService);
  private connection?: HubConnection;

  async connect(tenantId: string, onStatusChanged: (event: DocumentStatusChanged) => void): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    // The token authenticates the connection; JoinTenant then asks the server to validate
    // this tenant subscription rather than trusting the client-provided tenant ID.
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/documents`, {
        accessTokenFactory: () => firstValueFrom(this.oidcSecurityService.getAccessToken())
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('DocumentStatusChanged', onStatusChanged);
    await this.connection.start();
    await this.connection.invoke('JoinTenant', tenantId);
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
  }
}