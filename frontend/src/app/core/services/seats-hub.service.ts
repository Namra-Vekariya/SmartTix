import { Injectable, inject } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { API_BASE_URL } from '../tokens';
import { SeatsChangedEvent } from '../models/seat.models';

/**
 * SignalR client for /hubs/seats (anonymous hub — the seat map is public).
 * Call joinEvent(eventId) while a seat map is on screen; every seat change
 * for that event arrives on seatsChanged$.
 */
@Injectable({ providedIn: 'root' })
export class SeatsHubService {
  private readonly baseUrl = inject(API_BASE_URL);
  private connection: HubConnection | null = null;
  private readonly seatsChangedSubject = new Subject<SeatsChangedEvent>();

  readonly seatsChanged$: Observable<SeatsChangedEvent> =
    this.seatsChangedSubject.asObservable();

  async joinEvent(eventId: string): Promise<void> {
    await this.start();
    await this.connection?.invoke('JoinEvent', eventId);
  }

  async leaveEvent(eventId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveEvent', eventId);
    }
  }

  async stop(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
  }

  private async start(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/seats`)
      .withAutomaticReconnect()
      .build();

    this.connection.on('seatsChanged', (payload: SeatsChangedEvent) =>
      this.seatsChangedSubject.next(payload),
    );

    await this.connection.start();
  }
}
