import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Subject } from 'rxjs';

import { environment } from '../../environments/environment';
import { StockChanged } from './api.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class StockHubService {
  private readonly auth = inject(AuthService);
  private connection: HubConnection | null = null;
  private readonly changes = new Subject<StockChanged>();

  /** Every stock change pushed by the server. */
  readonly stockChanged$ = this.changes.asObservable();

  readonly state = signal<'offline' | 'connecting' | 'live'>('offline');

  async connect(): Promise<void> {
    if (this.connection) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl, { accessTokenFactory: () => this.auth.token() ?? '' })
      .withAutomaticReconnect()
      .build();

    connection.on('stockChanged', (message: StockChanged) => this.changes.next(message));
    connection.onreconnecting(() => this.state.set('connecting'));
    connection.onreconnected(() => this.state.set('live'));
    connection.onclose(() => this.state.set('offline'));

    this.connection = connection;
    this.state.set('connecting');

    try {
      await connection.start();
      this.state.set('live');
    } catch {
      this.connection = null;
      this.state.set('offline');
    }
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.state.set('offline');
    await connection?.stop();
  }
}