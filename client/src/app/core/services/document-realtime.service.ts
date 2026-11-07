import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '@env/environment';

export interface DocumentStatusChanged {
  documentId: string;
  tenantId: string;
  fileName: string;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Failed';
}

/**
 * Service managing the real-time SignalR connection to DocumentHub.
 * Receives live document lifecycle updates (processing, ready, failed, deleted)
 * without requiring client-side polling.
 */
@Injectable({ providedIn: 'root' })
export class DocumentRealtimeService implements OnDestroy {
  private connection?: HubConnection;

  /**
   * Establishes a persistent WebSocket connection to the document hub and joins the tenant group.
   *
   * @param tenantId The active tenant identifier
   * @param onStatusChanged Callback invoked when a document's status transitions
   * @param onDocumentDeleted Optional callback invoked when a document is deleted
   */
  async connect(
    tenantId: string, 
    onStatusChanged: (event: DocumentStatusChanged) => void,
    onDocumentDeleted?: (documentId: string) => void
  ): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    // Real-time WebSocket connection to ASP.NET Core SignalR hub:
    // withCredentials: true ensures the Duende BFF encrypted session cookie is sent with the initial HTTP upgrade request.
    // 'X-CSRF: 1' satisfies anti-forgery middleware constraints.
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/documents`, {
        headers: { 'X-CSRF': '1' },
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('DocumentStatusChanged', onStatusChanged);
    if (onDocumentDeleted) {
      this.connection.on('DocumentDeleted', onDocumentDeleted);
    }

    await this.connection.start();

    // Join the tenant-isolated broadcast group.
    // The server-side DocumentHub validates that the authenticated connection possesses membership in this tenant before authorizing group entry.
    await this.connection.invoke('JoinTenant', tenantId);
  }

  /**
   * Gracefully terminates the SignalR hub connection.
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }
}
