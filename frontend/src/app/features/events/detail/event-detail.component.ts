import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, forkJoin, Subject, takeUntil } from 'rxjs';
import { of } from 'rxjs';
import { SeatsHubService } from '../../../core/services/seats-hub.service';
import { EventsService } from '../data/events.service';
import {
  EventDetail,
  Seat,
  SeatMap,
  SeatStatus,
} from '../models/event.models';

/** SignalR event name → resulting seat status. */
const STATUS_MAP: Record<string, SeatStatus> = {
  held: 'Held',
  confirmed: 'Booked',
  payment_failed: 'Available',
  cancelled: 'Available',
  expired: 'Available',
};

@Component({
  selector: 'app-event-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss',
})
export class EventDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly eventsService = inject(EventsService);
  private readonly seatsHub = inject(SeatsHubService);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly event = signal<EventDetail | null>(null);
  readonly seatMap = signal<SeatMap | null>(null);

  private eventId = '';

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id') ?? '';

    forkJoin({
      event: this.eventsService.getEvent(this.eventId),
      seatMap: this.eventsService.getSeatMap(this.eventId),
    })
      .pipe(
        catchError(() => {
          this.loading.set(false);
          return of(null);
        }),
        takeUntil(this.destroy$),
      )
      .subscribe((data) => {
        if (data) {
          this.event.set(data.event);
          this.seatMap.set(data.seatMap);
          void this.seatsHub.joinEvent(this.eventId);
        }
        this.loading.set(false);
      });

    // Live seat updates — flip statuses as other users hold/book.
    this.seatsHub.seatsChanged$
      .pipe(takeUntil(this.destroy$))
      .subscribe((change) => {
        if (change.eventId !== this.eventId) return;
        const status = STATUS_MAP[change.status];
        if (!status) return;

        const changedIds = new Set(change.seats.map((s) => s.seatId));
        const map = this.seatMap();
        if (!map) return;

        this.seatMap.set({
          ...map,
          seats: map.seats.map((seat) =>
            changedIds.has(seat.id) ? { ...seat, status } : seat,
          ),
        });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    void this.seatsHub.leaveEvent(this.eventId);
  }

  /** Seats grouped by row label (A, B, C…) for grid rendering. */
  rows(): { label: string; seats: Seat[] }[] {
    const map = this.seatMap();
    if (!map) return [];

    const grouped = new Map<string, Seat[]>();
    for (const seat of map.seats) {
      const list = grouped.get(seat.rowLabel) ?? [];
      list.push(seat);
      grouped.set(seat.rowLabel, list);
    }

    return [...grouped.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([label, seats]) => ({
        label,
        seats: seats.sort((x, y) => x.seatNumber - y.seatNumber),
      }));
  }

  seatColor(seat: Seat): string {
    if (seat.status === 'Booked') return '#9e9e9e';
    if (seat.status === 'Held') return '#ffb300';
    return seat.colorHex || '#3f51b5';
  }

  seatTooltip(seat: Seat): string {
    return `${seat.seatCode} · ${seat.categoryName} · $${seat.price.toFixed(2)} · ${seat.status}`;
  }

  soldOut(): boolean {
    return (this.event()?.availableSeatsCount ?? 1) === 0;
  }
}
