import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '@env/environment';

export interface DocumentStatusChanged {
  documentId: string;
  tenantId: string;
  fileName: string;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Failed';
}

@Injectable({ providedIn: 'root' })
export class DocumentRealtimeService {
  private connection?: HubConnection;

  async connect(tenantId: string, onStatusChanged: (event: DocumentStatusChanged) => void): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    // The BFF auth cookie authenticates the connection; JoinTenant then asks the server to validate
    // this tenant subscription rather than trusting the client-provided tenant ID.
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/documents`, {
        headers: { 'X-CSRF': '1' }
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