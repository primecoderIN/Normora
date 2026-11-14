import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '@env/environment';

export interface WorkspaceStateSnapshot {
  tenantId: string;
  dataVersion: number;
  mutationSource: string;
  timestamp: string;
}

/**
 * Service managing real-time workspace state synchronization.
 * Listens to SignalR events and SSE state snapshots to maintain a reactive dataVersion Signal.
 * Components and services can watch dataVersion to automatically reload state without full-page refetches.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeStateService {
  private hubConnection: signalR.HubConnection | null = null;

  /** Reactive signal containing the current workspace data version. */
  public readonly dataVersion = signal<number>(1);
  public readonly lastMutationSource = signal<string>('initial');

  public startConnection(tenantId: string): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/documents`, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('WorkspaceStateUpdated', (snapshot: WorkspaceStateSnapshot) => {
      if (snapshot.dataVersion > this.dataVersion()) {
        this.dataVersion.set(snapshot.dataVersion);
        this.lastMutationSource.set(snapshot.mutationSource);
      }
    });

    this.hubConnection
      .start()
      .then(() => {
        return this.hubConnection?.invoke('JoinTenant', tenantId);
      })
      .catch((err) => {
        // Silently handle connection failure in environments where WebSockets are unavailable
        console.debug('RealtimeStateService connection skipped:', err);
      });
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      void this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  /** Called when an SSE stream delivers a state_snapshot event during agent dialogue. */
  public handleStateSnapshot(version: number): void {
    if (version > this.dataVersion()) {
      this.dataVersion.set(version);
      this.lastMutationSource.set('agent_stream');
    }
  }
}

