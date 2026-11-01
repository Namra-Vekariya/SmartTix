import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, Subject, takeUntil } from 'rxjs';
import { forkJoin, finalize, of } from 'rxjs';
import { AuthService } from '../../auth/data/auth.service';
import { SeatsHubService } from '../../../core/services/seats-hub.service';
import { EventsService } from '../data/events.service';
import { BookingService } from '../../bookings/data/booking.service';
import { EventDetail, Seat, SeatMap } from '../models/event.models';

const MAX_SEATS = 6;

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
  private readonly router = inject(Router);
  private readonly eventsService = inject(EventsService);
  private readonly seatsHub = inject(SeatsHubService);
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly event = signal<EventDetail | null>(null);
  readonly seatMap = signal<SeatMap | null>(null);

  // Seat ids the user has clicked ,still local — hold happens on Proceed
  readonly selected = signal<Set<string>>(new Set());
  readonly holding = signal(false);

  readonly selectedCount = computed(() => this.selected().size);

  readonly selectedTotal = computed(() => {
    const map = this.seatMap();
    if (!map) return 0;
    const ids = this.selected();
    return map.seats.filter((seat) => ids.has(seat.id)).reduce((sum, seat) => sum + seat.price, 0);
  });

  readonly selectedSeatsLabel = computed(() => {
    const map = this.seatMap();
    if (!map) return '';
    const ids = this.selected();
    return map.seats
      .filter((seat) => ids.has(seat.id))
      .map((seat) => seat.seatCode)
      .join(', ');
  });

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
    // Backend sends the resulting SeatStatus in `status` ("Held"/"Booked"/...).
    this.seatsHub.seatsChanged$.pipe(takeUntil(this.destroy$)).subscribe((change) => {
      if (change.eventId !== this.eventId) return;

      const map = this.seatMap();
      if (!map) return;

      const newStatus =
        change.status === 'Available' || change.status === 'Held' || change.status === 'Booked'
          ? change.status
          : 'Blocked';

      const changedIds = new Set(change.seats.map((s) => s.seatId));

      // A seat left our control (someone else held/booked it) — if it was
      // selected here, drop it and tell the user immediately.
      if (newStatus !== 'Available') {
        const stolen = change.seats.filter((s) => this.selected().has(s.seatId));
        if (stolen.length > 0) {
          this.selected.update((current) => {
            const next = new Set(current);
            for (const s of stolen) next.delete(s.seatId);
            return next;
          });
          const codes = stolen.map((s) => s.seatCode).join(', ');
          this.snackBar.open(
            `Seat ${codes} ${newStatus === 'Held' ? 'was just held' : 'was just booked'} by another user — removed from your selection.`,
            'Close',
            { duration: 5000 },
          );
        }
      }

      this.seatMap.set({
        ...map,
        seats: map.seats.map((seat) =>
          changedIds.has(seat.id) ? { ...seat, status: newStatus } : seat,
        ),
      });
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    void this.seatsHub.leaveEvent(this.eventId);
  }

  toggleSeat(seat: Seat): void {
    if (seat.status !== 'Available') return;

    const current = this.selected();

    if (current.has(seat.id)) {
      const next = new Set(current);
      next.delete(seat.id);
      this.selected.set(next);
      return;
    }

    if (current.size >= MAX_SEATS) {
      this.snackBar.open(`You can book up to ${MAX_SEATS} seats at once.`, 'Close', {
        duration: 3000,
      });
      return;
    }

    this.selected.set(new Set(current).add(seat.id));
  }

  isSelected(seat: Seat): boolean {
    return this.selected().has(seat.id);
  }

  proceedToCheckout(): void {
    const seatIds = [...this.selected()];
    if (seatIds.length === 0) return;

    if (!this.auth.isAuthenticated()) {
      this.snackBar.open('Please sign in to complete your booking.', 'Close', {
        duration: 4000,
      });
      void this.router.navigate(['/login'], {
        queryParams: { returnUrl: this.router.url },
      });
      return;
    }

    this.holding.set(true);
    this.bookingService
      .holdSeats({ eventId: this.eventId, seatIds })
      .pipe(
        finalize(() => this.holding.set(false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (hold) => {
          void this.router.navigate(['/bookings/checkout', hold.holdId]);
        },
        error: () => {
          // Global interceptor already toasted (e.g. "seat was just taken").
          // The seat map may be stale — refresh it and prune the selection.
          this.refreshSeatMap();
        },
      });
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
    if (seat.status === 'Booked' || seat.status === 'Blocked') return '#9e9e9e';
    if (seat.status === 'Held') return '#ffb300';
    return seat.colorHex || '#3f51b5';
  }

  seatTooltip(seat: Seat): string {
    return `${seat.seatCode} · ${seat.categoryName} · $${seat.price.toFixed(2)} · ${seat.status}`;
  }

  soldOut(): boolean {
    return (this.event()?.availableSeatsCount ?? 1) === 0;
  }

  private refreshSeatMap(): void {
    this.eventsService
      .getSeatMap(this.eventId)
      .pipe(takeUntil(this.destroy$))
      .subscribe((map) => {
        this.seatMap.set(map);
        this.pruneSelection(map);
      });
  }

  private pruneSelection(map: SeatMap): void {
    const availableIds = new Set(
      map.seats.filter((s) => s.status === 'Available').map((s) => s.id),
    );
    this.selected.update((current) => new Set([...current].filter((id) => availableIds.has(id))));
  }
}
